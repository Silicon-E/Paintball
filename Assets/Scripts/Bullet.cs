using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Specialized;
using UnityEngine.Networking;
//using NUnit.Framework.Internal.Filters;


#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.UI;

public class Bullet : NetworkBehaviour {
	public LineRenderer line;
	private Controller control;
	//public LayerMask[] teamMasks;

	//TODO: do these need to be static?
	static float mark2 = 0.1f;//Time to fire second raycast
	static float markFinal = 0.2f;//Time to fire max length raycast
	static float range1 = 4f;//Range of first raycast
	static float range2 = 8f;//Range of second raycast
	float rangeFinal = 100f;//Max range

	private float speed;
	private bool isEffect;
	private int team;

	private Image hitIndicator;

	Vector3 origin;//Camera where fired
	Vector3 direction;//Direction fired
	Vector3 start;//Location of weapon muzzle

	Vector3 end;//Location bullet is going towards
	float count = 0f;
	float prevCount = 0f;
	LayerMask hitMask;

	// Use this for initialization
	void Start () {
		
	}

	public void Init(Controller c, Vector3 o, Vector3 d, int t, Image h, bool isEff)
	{
		control = c;

		origin = o;
		direction = d;

		line.material.color = Manager.teamColors[t];

		start = transform.position;//Start at spawn location
		speed = range1 / mark2;//Meters per second

		hitIndicator = h;

		isEffect = isEff;
		team = t;

		hitMask = Manager.losMasks[t];//teamMasks[team];
	}
	
	// Update is called once per frame
	void FixedUpdate ()
	{
		count += Time.deltaTime;
		//Debug.Log(count);
		if(count*speed>rangeFinal){
			Destroy(gameObject); return; }
		RaycastHit hit;
		if(prevCount==0f)
		{
			if(Physics.Raycast(origin, direction, out hit, range1, hitMask))
			{
				RayHit(hit);
			}else if(Physics.Raycast(origin, direction, out hit, rangeFinal, hitMask))//If no raycast hit at first, set end to max-range hit
				end = hit.point;
			else//If still no raycast hit, set end to max range
				end = origin + direction*rangeFinal;
		}else if(prevCount<mark2 && count>=mark2)
		{
			if(Physics.Raycast(origin, direction, out hit, range2, hitMask))
			{
				RayHit(hit);
			}
		}else if(prevCount<markFinal && count>=markFinal)
		{
			if(Physics.Raycast(origin, direction, out hit, rangeFinal, hitMask))
			{
				bool hitEnemy = RayHit(hit);
				//Speed up to hit target, modify start and range to keep look consistent (since position is calculated independently of prev position)
				if(hit.distance > speed*(markFinal+mark2) && hitEnemy)
				{
					speed = ((end-start).magnitude -range2) / mark2;
					start -= (end-start).normalized * (speed*markFinal - range2);
					rangeFinal = Vector3.Distance(start,end);// *= speed*mark2;
				}
			}
		}
		prevCount = count;

		Vector3 showDir = (end-start).normalized;
		Vector3 posFar = start + showDir*count*speed;
		Vector3 posNear = start + showDir*Mathf.Max(count*speed -2f,  0f);//Bullet length: 2
		line.SetPositions(new Vector3[] {posFar, posNear});
		line.enabled = true;
		//Debug.DrawRay(origin, direction * rangeFinal, Color.red);
		//Debug.DrawRay(end, Vector3.up, Color.cyan);
	}

	private bool RayHit(RaycastHit hit)//Returns true if hit an enemy
	{
		end = hit.point;
		rangeFinal = hit.distance;

		FPControl fp = hit.collider.gameObject.GetComponent<FPControl>();
		if(fp!=null)
		{
			if(hitIndicator!=null)
			{
				Color c = hitIndicator.color; c.a = 1f;
				hitIndicator.color = c;
			}
			if(!isEffect)
				DamageUnit(fp, 25, hit.point);
			return true;
		}else return false;
	}

	public static float DistToDelay(float dist)//Return delay of bullet at a given range
	{
		if(dist<=range1) return 0f;
		else if(dist<=range2) return mark2;
		else return markFinal;
	}

	public void DamageUnit(FPControl fp, int amount, Vector3 point)//Called to damage this controller, isFromServer defaults to be "sent" from other side
	{
		fp.Damage(amount, -direction, point);
		if(fp.playerControl != null)//Player may not have joined yet
			//fp.playerControl.CmdDamageAlert(fp.unitId/*netId*/, amount, -direction, point, fp.health, (isServer ?1 :0));
			Manager.bulletHits.Add(new Manager.HitInfo(fp.unitId, amount, -direction, point, fp.health/*, (isServer ?1 :0)*/));//This necessary to get around Unity's built-in refusal to let Bullet send Cmds
	}

	/*
	MOVED TO PlayerControl
	[Command] void CmdDamageAlert(NetworkInstanceId id, int amount, Vector3 dir, Vector3 point, int newHealth, int isFromServer)
	{Debug.Log("CmdDamageAlert");
		RpcDamageAlert(id, amount, dir, point, newHealth, isFromServer);
	}
	[ClientRpc] void RpcDamageAlert(NetworkInstanceId id, int amount, Vector3 dir, Vector3 point, int newHealth, int isFromServer)
	{Debug.Log("RpcDamageAlert");
		if(isFromServer != (isServer ?1 :0)) //Only run if sent from other side
			NetworkServer.FindLocalObject(netId).GetComponent<FPControl>().OnDamage(amount, dir, point, newHealth);
	}
	*/
}
