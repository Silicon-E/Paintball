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
using System.Runtime.InteropServices;

public class AIControl : Controller {       //NOTE: noise AND noiseMulti are de-implemented in favor of randomly mulitplying target-leading

	public FPControl fp;
	public Collider frustum;
	static float degPerSec = 180;
	public static float moveRadius = 5;// Max distance from squad pos at which AI will stop going
	static float chaseRadius = 10;//Distance from squad pos that AI will chase enemy
	//static float distPerDeg = 4f/45f;//NoiseMulti bonus per degrees of mouse movement
	public NavMeshAgent agent;
	public GameObject target = null;
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

	private Vector3 Flatten(Vector3 inVec)
	{
		Vector3 outVec = new Vector3(inVec.x, inVec.y, inVec.z);
		outVec.Scale(new Vector3(1,0,1));
		return outVec;
	}

	List<Priority> movePris = new List<Priority>();
	List<Priority> lookPris = new List<Priority>();
	delegate bool Act(ref input inp);
	bool lookAtTarget(ref input inp)
	{
		if(target==null) return false;

		bool isFlanking = Vector3.Dot(fp.gameObject.transform.forward, target.transform.forward) > 0f;

		Quaternion preRot = fp.camPivot.transform.rotation;

		Vector3 velOffset = targetPhysics.velocity * Bullet.DistToDelay(Vector3.Distance(target.transform.position, fp.camPivot.transform.position));
		if(isFlanking)
			/*noiseMulti = 0f*/;
		else
			velOffset *= (Mathf.Abs(noise.x)+0)*4f;//Essentially a random float from 0 to 4 that is the same for each shot
		Vector3 aimPos = target.transform.position + velOffset;

		Quaternion desiredRot = Quaternion.LookRotation((aimPos/*+ noise*noiseMulti*/) -fp.camPivot.transform.position);
		if(Quaternion.Angle(fp.camPivot.transform.rotation, desiredRot) < degPerSec*Time.deltaTime)
		{
			fp.camPivot.transform.rotation = desiredRot;
			Fire(ref inp);
		}else //if  not looking close enough
			fp.camPivot.transform.rotation = Quaternion.RotateTowards(fp.camPivot.transform.rotation, desiredRot, degPerSec*Time.deltaTime/**noiseMulti*/);

		float sep = Vector3.Distance(aimPos, fp.camPivot.transform.position + fp.camPivot.transform.forward*Vector3.Distance(target.transform.position, fp.camPivot.transform.position));
		noiseMulti = Mathf.Min(1, sep*0.5f);
		//noiseMulti += Quaternion.Angle(preRot, fp.camPivot.transform.rotation) *distPerDeg;
		//Mathf.Clamp(noiseMulti, 0f, 1f);

		//if(sep<0.2f)
		//	Fire(ref inp);
		return true;
	}
	bool lookToDamage(ref input inp)
	{
		if(damageDir==Vector3.zero || target!=null)
			return false;

		if(Vector3.Dot(fp.camPivot.transform.forward, damageDir)>0.95)//If looking close enough to damageDir
		{
			damageDir = Vector3.zero;
			return false;
		}

		fp.camPivot.transform.rotation = Quaternion.RotateTowards(fp.camPivot.transform.rotation, Quaternion.LookRotation(damageDir), degPerSec*Time.deltaTime/**noiseMulti*/);
		return true;
	}
	bool lookIdle(ref input inp)//Look in moving direction
	{
		System.Random rand = new System.Random((int)((Time.time +gameObject.GetInstanceID())*0.5f));//Same seed from each 2-second interval

		if(rand.Next(1,fp.squad.members.Count+1)==1)//each interval has a 1/4 chance of looking TODO: make it 1/[peeps_in_squad]
			fp.camPivot.transform.rotation = Quaternion.RotateTowards(fp.camPivot.transform.rotation, Quaternion.LookRotation(new Vector3((float)(rand.NextDouble())*2f-1f, ((float)(rand.NextDouble())*2f-1f)*0.5f, (float)(rand.NextDouble())*2f-1f)), degPerSec*Time.deltaTime*noiseMulti);
		else
			fp.camPivot.transform.rotation = Quaternion.RotateTowards(fp.camPivot.transform.rotation, Quaternion.LookRotation(Vector3.Max(new Vector3(0,0,0.0001f), fp.physics.velocity)), degPerSec*Time.deltaTime*noiseMulti);
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
		//Debug.Log(fp.team+": "+radius);
		radius = Mathf.Max(radius, 1.25f);
		if(Vector3.Distance(Flatten(fp.player.transform.position)/*+Vector3.down*/, fp.squad.destination/*fp.squad.transform.position*/) < radius)
			return false;
		else if(radius == chaseRadius) //If chasing, but got to max distance
			chasePos = nullVec; //Stop chasing
		
		agent.destination = fp.squad.destination; //fp.squad.transform.position;
		Vector3 moveVec = Quaternion.Inverse(Quaternion.LookRotation(new Vector3(fp.camPivot.transform.forward.x,0f,fp.camPivot.transform.forward.z))) * agent.desiredVelocity;
		inp.move = new Vector2(moveVec.x, moveVec.z);
		return true;
	}
	bool moveChase(ref input inp)
	{
		//TODO: if mode is not "kill things" AND chase pos is too far from squad pos
			//return false;

		if(chasePos==null || chasePos==nullVec || target!=null) return false;//If targeting or not chasing

		if(Vector3.Distance(Flatten(fp.transform.position), Flatten(chasePos)) <= 0.5f)//If within 0.5m of chasePos
		{
			chasePos = nullVec;
			return false;
		}

		agent.destination = chasePos;
		inp.move = ToLocalMovement(agent.desiredVelocity);

		if(! agent.CalculatePath(chasePos, new NavMeshPath())) // If can't get to position
			chasePos = nullVec;                   // Stop chasing

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
			Vector3 moveVec = Quaternion.Inverse(Quaternion.LookRotation(new Vector3(fp.camPivot.transform.forward.x,0f,fp.camPivot.transform.forward.z))) * desiredMove;
			inp.move = new Vector2(moveVec.x, moveVec.z);
		}else//move to ideal range, clamped within 45 degrees of agent.desiredVelocity
		{
			Vector3 desiredMove = Vector3.RotateTowards(agent.desiredVelocity.normalized, targetDir, 1f, 0f);//1 radian possible deviance
			Vector3 moveVec = Quaternion.Inverse(Quaternion.LookRotation(new Vector3(fp.camPivot.transform.forward.x,0f,fp.camPivot.transform.forward.z))) * desiredMove;
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
		/*if(!isLocalPlayer)
		{
			Destroy(this);//destroy this component
			return;
		}*/

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
		if(isServer  &&  target!=null)
		{
			if(checkCooldown <= 0f)
			{
				if(!CheckLOS())
					SetTarget(null);
			}
			checkCooldown -= Time.deltaTime;
		}
		//Debug.Log(target);
	}
	private bool CheckLOS()//will NPE if target is not set; if NPE occurrs, set target before calling.
	{
		RaycastHit hit;
		if(Physics.Raycast(fp.camPivot.transform.position, (target.transform.position-fp.camPivot.transform.position), out hit, 128, Manager.losMasks[fp.team]))
		{
			if(hit.collider.gameObject == target)
				return true;
		}
		return false;
	}

	public override input GetInput()
	{//Debug.DrawRay(fp.camPivot.transform.position, fp.camPivot.transform.forward*Vector3.Distance(target.transform.position, fp.camPivot.transform.position), Color.red);
		input inp = new input();
		agent.nextPosition = fp.transform.position;

		inp.mouse = Vector2.zero;// LookAt(target.transform.position-fp.camPivot.transform.position, 360);

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
		damageDir = dir.normalized;
	}

	private Vector2 ToLocalMovement(Vector3 worldVec)
	{
		Vector3 rotatedVec = Quaternion.Inverse(Quaternion.LookRotation(new Vector3(fp.camPivot.transform.forward.x,0f,fp.camPivot.transform.forward.z))) * worldVec;
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
		//Quaternion diff = Quaternion.LookRotation(fp.camPivot.transform.forward-dir,Vector3.up);//Quaternion.Inverse(fp.camPivot.transform.rotation) * Quaternion.LookRotation(dir, Vector3.up);
		//Debug.Log(diff.eulerAngles);
		Vector3 targetHoriz = target.transform.position; targetHoriz.Scale(new Vector3(1,0,1));
		Vector3 thisHoriz = fp.camPivot.transform.position; thisHoriz.Scale(new Vector3(1,0,1));
		Quaternion horiz = Quaternion.LookRotation(targetHoriz-thisHoriz, Vector3.up);
			Debug.Log(targetHoriz-thisHoriz);
		Vector3 relativeUp = new Vector3(0, target.transform.position.y-fp.camPivot.transform.position.y, (targetHoriz-thisHoriz).magnitude);
		Quaternion vert = Quaternion.LookRotation(relativeUp, Vector3.up);
			Debug.Log(relativeUp);
		Vector2 vec = new Vector2(horiz.eulerAngles.y, vert.eulerAngles.x);
		if(vec.magnitude > distance*Time.deltaTime)//If vec mag is greater than allowed distance this tick
			return vec.normalized*distance *Time.deltaTime;
		else//If vec mag is within allowed distance
			return vec;
	}*/
}
