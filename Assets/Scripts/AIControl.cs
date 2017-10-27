using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using UnityEngine.Networking.Types;
using System.Runtime.ConstrainedExecution;
using System;

public class AIControl : Controller {

	public FPControl fp;
	static float degPerSec = 180;
	public NavMeshAgent agent;
	public GameObject target;
	Vector3 noise;
	float noiseMulti = 1;

	public float idealRange;//Ideal range for this AI's weapon; varies by unit type

	private Rigidbody targetPhysics;

	List<Priority> movePris = new List<Priority>();
	List<Priority> lookPris = new List<Priority>();
	delegate bool Act(ref input inp);
	bool lookAtTarget(ref input inp)
	{
		if(target==null) return false;
		Vector3 velOffset = targetPhysics.velocity * Bullet.DistToDelay(Vector3.Distance(target.transform.position, fp.camera.transform.position));
		Debug.Log(velOffset);
		Vector3 aimPos = target.transform.position + velOffset;
		fp.camera.transform.rotation = Quaternion.RotateTowards(fp.camera.transform.rotation, Quaternion.LookRotation((aimPos+ noise*noiseMulti) -fp.camera.transform.position), degPerSec*Time.deltaTime*noiseMulti);
		float sep = Vector3.Distance(aimPos, fp.camera.transform.position + fp.camera.transform.forward*Vector3.Distance(target.transform.position, fp.camera.transform.position));
		noiseMulti = Mathf.Min(1, sep*0.5f);
		if(sep<1f)
			inp.mouseL = true;
		return true;
	}
	bool lookIdle(ref input inp)//Look in moving direction
	{
		System.Random rand = new System.Random((int)(Time.time*0.5f));//Same seed from each 2-second interval

		if(rand.Next(1,8)==1)//each interval has a 1/8 chance of looking TODO: make it 1/[peeps_in_squad]
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
	{
		//TODO: if too far from squad pos, go towards
		Vector3 moveVec = Quaternion.Inverse(Quaternion.LookRotation(new Vector3(fp.camera.transform.forward.x,0f,fp.camera.transform.forward.z))) * agent.desiredVelocity;
		inp.move = new Vector2(moveVec.x, moveVec.z);
		return false;
	}
	bool moveIdealRange(ref input inp)
	{
		if(target==null)
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
	}

	public void Awake()
	{
		SetTarget(target);//To set targetPhysics if given pre-set target. Mostly for tetsing, but also a safety measure.

		agent.updatePosition = false;
		agent.updateRotation = false;

		noise = new Vector3(UnityEngine.Random.value*2-1, UnityEngine.Random.value*2-1, UnityEngine.Random.value*2-1);

		lookPris.Add(new Priority(lookAtTarget));
		lookPris.Add(new Priority(lookIdle));
		movePris.Add(new Priority(moveAway));
		movePris.Add(new Priority(moveToCover));
		movePris.Add(new Priority(moveToSquad));
		movePris.Add(new Priority(moveIdealRange));
	}

	public override input GetInput()
	{Debug.DrawRay(fp.camera.transform.position, fp.camera.transform.forward*Vector3.Distance(target.transform.position, fp.camera.transform.position), Color.red);
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

	void SetTarget(GameObject newTarget)
	{
		target = newTarget;
		targetPhysics = target.GetComponent<Rigidbody>();
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
