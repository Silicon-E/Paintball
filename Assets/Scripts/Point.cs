using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.ConstrainedExecution;
using UnityEngine.Networking.NetworkSystem;

public class Point : NetworkBehaviour {

	public float capTime = 10;
	public int startingOwner = 0; //0=none, 1=team 0, -1=team 1
	public Point nextPoint0 = null; //Adjacent point on team 0's side
	public Point nextPoint1 = null; //Adjacent point on team 1's side
	public GUIPoint guiPoint; //Contains this point's GUI elements
	public GameManager gameManager;

	List<FPControl> FPCs0 = new List<FPControl>();
	List<FPControl> FPCs1 = new List<FPControl>();
	int occupants0 = 0;
	int occupants1 = 0;
	const int STATUS_NONE = 0;//No occupants and no progress
	const int STATUS_WAIT_0 = 1;//Waiting for team 0 to enter
	const int STATUS_WAIT_1 = -1;//Waiting for team 1 to enter
	const int STATUS_VACANT_0 = 2;//Team 0 has progress but no units on the point
	const int STATUS_VACANT_1 = -2;//Team 1 has progress but no units on the point
	const int STATUS_BLOCK_0 = 3;//Team 1 is blocking team 0's progress
	const int STATUS_BLOCK_1 = -3;//Team 1 is blocking team 0's progress
	const int STATUS_CAP_0 = 4;//team 0 capturing
	const int STATUS_CAP_1 = -4;//team 1 capturing
	const int STATUS_HELD_0 = 5;//Waiting for team 1 to enter
	const int STATUS_HELD_1 = -5;//Waiting for team 1 to enter
	[SyncVar/*(hook="OnStatusChange")*/] int status = 0;
	/*void OnStatusChange(int newStatus)
	{
		Debug.Log("status: "+status);
		Debug.Log("newStatus: "+newStatus);
	}*/
	[SyncVar] float capProgress = 0f;//increases from 0 to 1

	// Use this for initialization
	void Start () {
		if(isServer)
		{
			switch(startingOwner)
			{
			case 1:
				status = STATUS_HELD_0; break;
			case -1:
				status = STATUS_HELD_1; break;
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		if(isServer)
		{
			occupants0=0;
			foreach(FPControl fp in FPCs0)
				if(!fp.isDead)
					occupants0++;
			occupants1=0;
			foreach(FPControl fp in FPCs1)
			{
				if(!fp.isDead)
					occupants1++;
			}

			float change = Time.deltaTime / capTime;    //UPDATE CAP_PROGRESS
			switch(Mathf.Abs(status)) //Abs(x), so handles both positive and negative cases
			{
			case STATUS_VACANT_0:
				if(capProgress < change)
					capProgress = 0;
				else
					capProgress -= change;
				break;
			case STATUS_CAP_0:
				capProgress += change;
				break;
			}

			UpdateStatus();// Debug.Log("0: "+occupants0+"\n1: "+occupants1);
		}

		//UPDATE GUI
		if(Mathf.Abs(status) >= STATUS_VACANT_0)// if vacant, blocked, capping, or held
		{
			if(status > 0) //If team 0 is capping or holding
			{
				guiPoint.fill.fillClockwise = true;
				guiPoint.fill.color = Manager.teamColors[0];
				guiPoint.dot.color = Manager.teamColors[0];
			}else //if team 1 is capping or holding
			{
				guiPoint.fill.fillClockwise = false;
				guiPoint.fill.color = Manager.teamColors[1];
				guiPoint.dot.color = Manager.teamColors[1];
			}
		}else //if none or waiting
		{
			guiPoint.fill.color = Color.clear;
		}

		if (Mathf.Abs(status) == STATUS_WAIT_0) //If waiting
		{
			guiPoint.arrow.rectTransform.localScale = new Vector3(Mathf.Sign(status), 1f, 1f);
			if(status > 0) //If waiting for team 0
				guiPoint.arrow.color = Manager.teamColors[0];
			else //If waiting for team 1
				guiPoint.arrow.color = Manager.teamColors[1];
		}else //If not waiting
			guiPoint.arrow.color = Color.clear;

		if(Mathf.Abs(status)==STATUS_CAP_0) //If capping
			guiPoint.dot.color = new Color(guiPoint.dot.color.r, guiPoint.dot.color.g, guiPoint.dot.color.b, 0.5f+0.5f*Mathf.Sin(Time.time * 4f));
		else if(Mathf.Abs(status)==STATUS_BLOCK_0) //If blocked
			{} //Color set previoulsy, no need to change
		else
			guiPoint.dot.color = Color.clear;

		if(Mathf.Abs(status)==STATUS_BLOCK_0) //If blocked 
			guiPoint.clash.color = Manager.teamColors[(status>0) ?1 :0];
		else
			guiPoint.clash.color = Color.clear;
		
		if(status==STATUS_HELD_0 || status==STATUS_HELD_1)
			guiPoint.slider.value = 1f;
		else
			guiPoint.slider.value = capProgress;
	}

	void OnTriggerEnter(Collider other)
	{
		if(isServer)
		{
			if(LayerMask.LayerToName(other.gameObject.layer) == Manager.teamLayers[0])
				FPCs0.Add(other.GetComponent<FPControl>());//occupants0++;
			else //Can only collide with units, so only 2 layers are possible
				FPCs1.Add(other.GetComponent<FPControl>());//occupants1++;
			UpdateStatus();
		}
	}
	void OnTriggerExit(Collider other)
	{
		if(isServer)
		{
			if(LayerMask.LayerToName(other.gameObject.layer) == Manager.teamLayers[0])
				FPCs0.Remove(other.GetComponent<FPControl>());//occupants0--;
			else //Can only collide with units, so only 2 layers are possible
				FPCs1.Remove(other.GetComponent<FPControl>());//occupants1--;
			UpdateStatus();
		}
	}

	void UpdateStatus()
	{
		if(gameManager.winningTeam != -1) //No points can be captured when a team has won
			return;

		if(status == STATUS_WAIT_0 //Stop waiting
				&& occupants0 > 0)
			status = STATUS_NONE;
		else if(status == STATUS_WAIT_1
				&& occupants1 > 0)
			status = STATUS_NONE;



		if((status==STATUS_NONE || status==STATUS_VACANT_0 || status==STATUS_BLOCK_0) //Begin capturing  (NONE,VACANT,BLOCK -> CAP)
				&& occupants0 > 0
				&& occupants1 == 0)
		{
			status = STATUS_CAP_0;
		}else if((status==STATUS_NONE || status==STATUS_VACANT_1 || status==STATUS_BLOCK_1)
				&& occupants1 > 0
				&& occupants0 == 0)
		{
			status = STATUS_CAP_1;
		}

		else if(status==STATUS_BLOCK_0 //BLOCK -> VACANT
				&& occupants0==0)
			status = STATUS_VACANT_0;
		else if(status==STATUS_BLOCK_1 //BLOCK -> VACANT
				&& occupants1==0)
			status = STATUS_VACANT_1;

		else if((status == STATUS_CAP_0 || status == STATUS_CAP_1) //Capture
				&& capProgress >= 1)
		{
			Point next;
			if(status == STATUS_CAP_0)
			{
				status = STATUS_HELD_0;
				next = nextPoint1;
			}else
			{
				status = STATUS_HELD_1;
				next = nextPoint0;
			}
			if(next==null) // WINNING
			{
				gameManager.winningTeam = (Mathf.Abs(status) > 0) ?0 :1;
			}else
			{
				if(status == STATUS_HELD_0)
					next.status = STATUS_WAIT_0;
				else
					next.status = STATUS_WAIT_1;
				next.capProgress = 0f;
			}
		}

		else if(status == STATUS_CAP_0 //Stop capturing (CAP -> VACANT)
				&& occupants0==0)
			status = STATUS_VACANT_0;
		else if(status == STATUS_CAP_1
				&& occupants1==0)
			status = STATUS_VACANT_1;

		else if(status == STATUS_CAP_0 //Block (CAP -> BLOCK)
				&& occupants1 > 0)
			status = STATUS_BLOCK_0;
		else if(status == STATUS_CAP_1 //Block (CAP -> BLOCK)
				&& occupants0 > 0)
			status = STATUS_BLOCK_1;

		else if(Mathf.Abs(status)==STATUS_VACANT_0 //Go from VACANT to NONE
				&& capProgress==0f)
			status = STATUS_NONE;
	}
}
