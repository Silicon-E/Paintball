using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;

public class SquadCenter : MonoBehaviour {

	public Squad squad; //Parent squad

	private float waitTime = 0f;
	private float checkInterval = 1f;

	void Start()
	{
		if(squad.hasAuthority)
		{
			OverlapCheck();
		}
	}
	//IEnumerator WaitForAuthority

	void Update()
	{
		waitTime += Time.deltaTime;

		//if(squad.hasAuthority) THIS NEEDS TO HAPPEN ON THE SERVER FOR RESPAWNS
		//{
			if(waitTime > checkInterval)
			{
				OverlapCheck();
				waitTime = 0f;
			}
		//}
	}

	void OverlapCheck()
	{
		foreach(Collider c in Physics.OverlapSphere(transform.position, 0.00001f))
			OnTriggerEnter(c);
	}

	void OnTriggerEnter(Collider other)
	{
		if(/*squad.hasAuthority  && MUST HAPPEN ON SERVER*/  other.tag == "Territory") //If other is a territory collider
		{
			Point parentPoint = other.GetComponentInParent<Point>();
			if(parentPoint != null)
				squad.territoryId = parentPoint.pointId;
			else
				squad.territoryId = squad.team * 100; //If this is a free-floating territory trigger, it is assumed to be the spawn terriyorry and so always makes respawns possible.
		}
	}
}
