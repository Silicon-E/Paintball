using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.Networking.Types;
using System.Runtime.ConstrainedExecution;
using System;
using UnityEngine.Events;
using System.IO;

public class AIControl : Controller {

	public FPControl fp;
	public Collider frustum;
	static float degPerSec = 180;
	static float moveRadius = 5;// Max distance from squad pos at which AI will stop going
	static float chaseRadius = 10;//Distance from squad pos that AI will chase enemy
	//static float distPerDeg = 4f/45f;//NoiseMulti bonus per degrees of mouse movement
	public NavMeshAgent agent;
	public GameObject target;
	private Vector3 chasePos;
	Vector3 noise;
	float noiseMulti = 1;

	public float idealRange;//Ideal range for this AI's weapon; varies by unit type

	private Rigidbody targetPhysics;

	private Vector3 damageDir;

	static Vector3 nullVec = new Vector3(19862252, 14442670, 37577711);
	static float checkDelay = 1f;//Interval to check line-of-sight to target
	private float checkCooldown;
	[HideInInspector]public bool shouldChase = true;

	List<Priority> movePris = new List<Priority>();
	List<Priority> lookPris = new List<Priority>();
	delegate bool Act(ref input inp);
	bool lookAtTarget(ref input inp)
	{
		if(target==null) return false;

		Quaternion preRot = fp.camera.transform.rotation;

		Vector3 velOffset = targetPhysics.velocity * Bullet.DistToDelay(Vector3.Distance(target.transform.position, fp.camera.transform.position));
		velOffset *= (noise.x+0)*1f;//Essentially a random float from 0 to 1 that is the same for each shot
		Vector3 aimPos = target.transform.position + velOffset;

		fp.camera.transform.rotation = Quaternion.RotateTowards(fp.camera.transform.rotation, Quaternion.LookRotation((aimPos+ noise*noiseMulti) -fp.camera.transform.position), degPerSec*Time.deltaTime*noiseMulti);

		float sep = Vector3.Distance(aimPos, fp.camera.transform.position + fp.camera.transform.forward*Vector3.Distance(target.transform.position, fp.camera.transform.position));
		noiseMulti = Mathf.Min(1, sep*0.5f);
		//noiseMulti += Quaternion.Angle(preRot, fp.camera.transform.rotation) *distPerDeg;
		//Mathf.Clamp(noiseMulti, 0f, 1f);
		if(sep<1f)
			Fire(ref inp);
		return true;
	}
	bool lookToDamage(ref input inp)
	{
		if(damageDir==Vector3.zero || target!=null)
			return false;

		if(Vector3.Dot(fp.camera.transform.forward, damageDir)>0.95)//If looking close enough to damageDir
		{
			damageDir = Vector3.zero;
			return false;
		}

		fp.camera.transform.rotation = Quaternion.RotateTowards(fp.camera.transform.rotation, Quaternion.LookRotation(damageDir), degPerSec*Time.deltaTime*noiseMulti);
		return true;
	}
	bool lookIdle(ref input inp)//Look in moving direction
	{
		System.Random rand = new System.Random((int)((Time.time +gameObject.GetInstanceID())*0.5f));//Same seed from each 2-second interval

		if(rand.Next(1,fp.squad.members.Count)==1)//each interval has a 1/4 chance of looking TODO: make it 1/[peeps_in_squad]
			fp.camera.transform.rotation = Quaternion.RotateTowards(fp.camera.transform.rotation, Quaternion.LookRotation(new Vector3((float)(rand.NextDouble())*2f-1f, ((float)(rand.NextDouble())*2f-1f)*0.5f, (float)(rand.NextDouble())*2f-1f)), degPerSec*Time.deltaTime*noiseMulti);
		else
			fp.camera.transform.rotation = Quaternion.RotateTowards(fp.camera.transform.rotation, Quaternion.LookRotation(fp.physics.velocity), degPerSec*Time.deltaTime*noiseMulti);
		return true;
	}

	bool moveAway(ref input inp)
	{
		//TODO if under heavy fire from an advancing and relatively close enemy
		return false;
	}
	bool moveToCover(ref input inp)
	{
		//TODO if under heavy fire from static enemy OR ordered to take cover
		return false;
	}
	bool moveToSquad(ref input inp)//NOTE: also used to make squads move by moving their squad pos
	{//no NPE protection; all units should belong to a squad.
		//if too far from squad pos, go towards
		float radius;
		if( (chasePos!=null && chasePos!=nullVec) || target!=null)//If targeting or chasing
			radius = chaseRadius;
		else
			radius = (float)new System.Random(gameObject.GetInstanceID()).NextDouble()*moveRadius;
		Debug.Log(fp.team+": "+radius);
		if(Vector3.Distance(fp.player.transform.position+Vector3.down, fp.squad.transform.position) < radius)
			return false;
		
		agent.destination = fp.squad.transform.position;
		Vector3 moveVec = Quaternion.Inverse(Quaternion.LookRotation(new Vector3(fp.camera.transform.forward.x,0f,fp.camera.transform.forward.z))) * agent.desiredVelocity;
		inp.move = new Vector2(moveVec.x, moveVec.z);
		return true;
	}
	bool moveChase(ref input inp)
	{
		//TODO: if mode is not "kill things" AND chase pos is too far from squad pos
			//return false;

		if(chasePos==null || chasePos==nullVec || target!=null) return false;//If targeting or not chasing

		if(Vector3.Distance(fp.transform.position, chasePos) <= 0.5f)//If within 0.5m of chasePos
		{
			chasePos = nullVec;
			return false;
		}

		agent.destination = chasePos;
		inp.move = ToLocalMovement(agent.desiredVelocity);
		return true;
	}
	bool moveIdealRange(ref input inp)
	{
		if(target==null  ||  Mathf.Abs(Vector3.Distance(target.transform.position, fp.transform.position)-idealRange)<1f)//If no target or within 1m of ideal range
			return false;
		
		Vector3 targetDir = (target.transform.position-fp.transform.position).normalized;
		targetDir *= Mathf.Sign(Vector3.Distance(target.transform.position, fp.transform.position)-idealRange);

		if(inp.move==Vector2.zero)//If no previous movePri has affected movement AND returned false (cough moveToSquad cough)
		{
			Vector3 desiredMove = targetDir;
			Vector3 moveVec = Quaternion.Inverse(Quaternion.LookRotation(new Vector3(fp.camera.transform.forward.x,0f,fp.camera.transform.forward.z))) * desiredMove;
			inp.move = new Vector2(moveVec.x, moveVec.z);
		}else//move to ideal range, clamped within 45 degrees of agent.desiredVelocity
		{
			Vector3 desiredMove = Vector3.RotateTowards(agent.desiredVelocity.normalized, targetDir, 1f, 0f);//1 radian possible deviance
			Vector3 moveVec = Quaternion.Inverse(Quaternion.LookRotation(new Vector3(fp.camera.transform.forward.x,0f,fp.camera.transform.forward.z))) * desiredMove;
			inp.move = new Vector2(moveVec.x, moveVec.z);
		}
		return true;
	}bool moveErratic(ref input inp)
	{
		if(target==null) return false;

		System.Random rand = new System.Random((int)(Time.time +gameObject.GetInstanceID() ));//Same seed from each 1-second interval
		inp.move.x = (rand.Next(0,2)-0.5f)*2f;
		return true;
	}

	public void Awake()
	{
		chasePos = nullVec;
		SetTarget(target);//To set targetPhysics if given pre-set target. Mostly for tetsing, but also a safety measure.

		agent.updatePosition = false;
		agent.updateRotation = false;

		lookPris.Add(new Priority(lookAtTarget));
		lookPris.Add(new Priority(lookToDamage));
		lookPris.Add(new Priority(lookIdle));
		movePris.Add(new Priority(moveAway));
		movePris.Add(new Priority(moveToCover));
		movePris.Add(new Priority(moveToSquad));
		movePris.Add(new Priority(moveChase));
		movePris.Add(new Priority(moveIdealRange));
		movePris.Add(new Priority(moveErratic));
	}

	void FixedUpdate()
	{
		if(target!=null)
		{
			if(checkCooldown <= 0f)
			{
				if(!CheckLOS())
					SetTarget(null);
			}
			checkCooldown -= Time.deltaTime;
		}
		Debug.Log(target);
	}
	private bool CheckLOS()//will NPE if target is not set; if NPE occurrs, set target before calling.
	{
		RaycastHit hit;
		if(Physics.Raycast(fp.camera.transform.position, (target.transform.position-fp.camera.transform.position), out hit, 128, Manager.losMasks[fp.team]))
		{
			if(hit.collider.gameObject == target)
				return true;
		}
		return false;
	}

	public override input GetInput()
	{//Debug.DrawRay(fp.camera.transform.position, fp.camera.transform.forward*Vector3.Distance(target.transform.position, fp.camera.transform.position), Color.red);
		input inp = new input();
		agent.nextPosition = fp.transform.position;

		inp.mouse = Vector2.zero;// LookAt(target.transform.position-fp.camera.transform.position, 360);

		foreach(Priority p in movePris)
		{
			if(p.act(ref inp))
				break;
		}
		foreach(Priority p in lookPris)
		{
			if(p.act(ref inp))
				break;
		}
		
		return inp;
	}

	void Fire(ref input inp)
	{
		if(fp.fireCooldown>0) return;

		inp.mouseL = true;
		noise = new Vector3(UnityEngine.Random.value*2-1, UnityEngine.Random.value*2-1, UnityEngine.Random.value*2-1);
	}

	void SetTarget(GameObject newTarget)
	{
		if(newTarget==null && target!=null)
		{
			if(shouldChase) chasePos = target.transform.position;
			else shouldChase = true;//shouldChase being false is a 1-time ticket to not chase. Used to keep AIs from chasing players they just killed.
			noiseMulti = 1f;
		}
		target = newTarget;
		targetPhysics = (target==null) ?null :target.GetComponent<Rigidbody>();
		checkCooldown = checkDelay;
	}

	void OnTriggerEnter(Collider other)
	{
		if(target==null)//If no current target
		{
			FPControl otherFP = other.gameObject.GetComponent<FPControl>();
			if(otherFP!=null && otherFP.team != fp.team)//If enterer is on other team
			{
				target = other.gameObject;//Set target and ONLY target so CheckLOS() can run
				if(CheckLOS())
					SetTarget(other.gameObject);
				else
					target = null;//Set to null once finished checking
			}
		}
	}
	void OnTriggerExit(Collider other)
	{
		if(other.gameObject == target)//If target exited frustum
		{
			SetTarget(null);
		}
	}

	public void TookDamage(Vector3 dir)
	{
		damageDir = dir;
	}

	private Vector2 ToLocalMovement(Vector3 worldVec)
	{
		Vector3 rotatedVec = Quaternion.Inverse(Quaternion.LookRotation(new Vector3(fp.camera.transform.forward.x,0f,fp.camera.transform.forward.z))) * worldVec;
		return new Vector2(rotatedVec.x, rotatedVec.z);
	}

	class Priority
	{//Note: in C#, 'const' is automatically 'static' as well.
		public Priority(Act a)
		{
			act = a;
		}
		public Act act;
	}

	/*private Vector2 LookAt(Vector3 dir, float distance)//Look direction, degrees per second
	{
		//Quaternion diff = Quaternion.LookRotation(fp.camera.transform.forward-dir,Vector3.up);//Quaternion.Inverse(fp.camera.transform.rotation) * Quaternion.LookRotation(dir, Vector3.up);
		//Debug.Log(diff.eulerAngles);
		Vector3 targetHoriz = target.transform.position; targetHoriz.Scale(new Vector3(1,0,1));
		Vector3 thisHoriz = fp.camera.transform.position; thisHoriz.Scale(new Vector3(1,0,1));
		Quaternion horiz = Quaternion.LookRotation(targetHoriz-thisHoriz, Vector3.up);
			Debug.Log(targetHoriz-thisHoriz);
		Vector3 relativeUp = new Vector3(0, target.transform.position.y-fp.camera.transform.position.y, (targetHoriz-thisHoriz).magnitude);
		Quaternion vert = Quaternion.LookRotation(relativeUp, Vector3.up);
			Debug.Log(relativeUp);
		Vector2 vec = new Vector2(horiz.eulerAngles.y, vert.eulerAngles.x);
		if(vec.magnitude > distance*Time.deltaTime)//If vec mag is greater than allowed distance this tick
			return vec.normalized*distance *Time.deltaTime;
		else//If vec mag is within allowed distance
			return vec;
	}*/
}
