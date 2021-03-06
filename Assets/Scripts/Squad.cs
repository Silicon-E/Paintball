﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.CodeDom;
using System.Collections.Specialized;
using UnityEngine.Events;
using UnityEngine.AI;

public class Squad : NetworkBehaviour {

	[SyncVar] public int team; //needs to be correct when it is started after sevrer-side instantiation
	[SyncVar] public int id; //Not unique between sides, so also check team
	[HideInInspector] public PlayerControl control; //No PlayerControls should exist in the hierarchy, so no need to be visible in inspector
	//public LineRenderer line;
	public SpriteRenderer marker;
	public Text label;
	public Sprite[] dots;
	public Sprite[] dotsHollow;
	public RectTransform dotsContainer;
	public Image dotsRed;
	public Image dotsRedDark;
	public Image dotsWhite;
	public Image dotsWhiteDark;
	public Image dotsRedHollow;
	public int nameInd;

	[HideInInspector] public bool highlighted = false;
	[HideInInspector] public bool isCommanded = false; //If this squad has a player-controlled member, follow that member
	[HideInInspector] [SyncVar/*(hook="DestHook")*/] public Vector3 destination;
	/*void DestHook(Vector3 newVec)
	{
		destination = newVec;
	}*/
	[Command] void CmdSyncDestination(Vector3 newDest)
	{
		destination = newDest;
	}
	[HideInInspector] public int territoryId; //the squadId of the point that controls the territory this squad is in

	//[HideInInspector]
	public List<Waypoint> waypoints = new List<Waypoint>();
	[HideInInspector] public int signal;//The signal this suad should wait for before continuing to the next waypoint (0 is none)

	//[HideInInspector]
	public List<FPControl> members = new List<FPControl>();
	//SyncListInt memberIds = new SyncListInt();
	[HideInInspector] public int wantedMembers = 1;
	private Vector3 prevPos;
	[HideInInspector] public float timeSinceMoveOrDamage = 0f;
	private GameManager gameManager;

	[HideInInspector] public static string[] CODES = {"alpha","bravo","charlie","delta","echo","foxtrot","india","kilo","november","oscar","quebec","romeo","sierra","tango","victor","xray","yankee","zulu"};
	static string[] ABBR = {"a","b","c","d","e","f","i","k","n","o","q","r","s","t","v","x","y","z"};

	private bool shouldBeServer; //Which value of isServer will allow this squad to be manipulated & displayed

	public override void OnStartServer()
	{
		gameManager = GameObject.FindObjectOfType<GameManager>();
		prevPos = transform.position;
		destination = transform.position;
	}
	public override void OnStartClient()
	{
		gameManager = GameObject.FindObjectOfType<GameManager>();
		if(id >= 0) //If spawned instead of pre-included
		{
			foreach(PlayerControl p in GameObject.FindObjectsOfType<PlayerControl>())
			{
				if(p.team==team && p.isLocalPlayer)
				{
					p.NewSquadSpawned(this);
					break;
				}
			}
		}
	//}
	//void Start () {
		shouldBeServer = (team==0);
		wantedMembers = members.Count; //Account for preassigned mambers

		if(isServer)
		{
			//foreach(FPControl fp in members)
			StartCoroutine("GetAuthority");
		}

		if(shouldBeServer != isServer)
		{
			GetComponent<Collider>().enabled = false;
			GetComponent<SpriteRenderer>().enabled = false;

			GetComponentInChildren<Canvas>().enabled = false;
			foreach(SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
				sr.enabled = false;
			//GetComponentInChildren<LineRenderer>().enabled = false;
		}

		UpdateDots();

		/*foreach(FPControl fp in GameObject.FindObjectsOfType<FPControl>())
		{
			if(fp.isLocalPlayer)
			{
				fp.Assign(this);
				this.transform.position = new Vector3(fp.transform.position.x, 0, fp.transform.position.z);
			}
		}*/
	}
	IEnumerator GetAuthority()
	{
		bool finding = true;
		while(finding)
		{
			foreach(PlayerControl p in FindObjectsOfType<PlayerControl>())
			{
				if(p.team==team && p.hasStarted)
				{
					GetComponent<NetworkIdentity>().AssignClientAuthority(p.connectionToClient);
					finding = false;
					break;
				}
			}
			yield return null;
		}
	}
	public override void OnStartAuthority()
	{
		Debug.Log("Got Authority: "+gameObject);
	}


	public void UpdateName()
	{
		label.text = ABBR[nameInd].ToUpper();
	}
	void UpdateDots()
	{
		float dotTotal = Mathf.Max(members.Count, wantedMembers);
		int widthRed = Mathf.Max(0, members.Count - wantedMembers);
		int widthRedDark = 0;
		int widthWhite = Mathf.Max(0, members.Count - widthRed);
		int widthWhiteDark = 0;
		int widthRedHollow = Mathf.Max(0, wantedMembers - members.Count);
		foreach(FPControl fp in members)
		{
			if(fp.isDead)
			{
				if(widthWhite > 0)
				{
					widthWhiteDark++;
					widthWhite--;
				}else
				{
					widthRedDark++;
					widthRed--;
				}
			}
		}
		dotsRed.sprite = dots[widthRed];
		dotsRedDark.sprite = dots[widthRedDark];
		dotsWhite.sprite = dots[widthWhite];
		dotsWhiteDark.sprite = dots[widthWhiteDark];
		dotsRedHollow.sprite = dotsHollow[widthRedHollow];
		//float memOverWant = members.Count/(float)wantedMembers;
		//if(float.IsNaN(memOverWant) || float.IsInfinity(memOverWant))
		//	memOverWant = 0;
		if(dotTotal == 0f) //Prevent divide by 0
			dotTotal = 1f;

		dotsRedDark.rectTransform.anchoredPosition = new Vector2((150/dotTotal)* (widthRed), 0);
		dotsWhite.rectTransform.anchoredPosition = new Vector2((150/dotTotal)* (widthRed + widthRedDark), 0);
		dotsWhiteDark.rectTransform.anchoredPosition = new Vector2((150/dotTotal)* (widthRed + widthRedDark + widthWhite), 0);
		dotsRedHollow.rectTransform.anchoredPosition = new Vector2((150/dotTotal)* (widthRed + widthRedDark + widthWhite + widthWhiteDark), 0);

		dotsRed.rectTransform.localScale = new Vector3(widthRed/dotTotal, 1, 1);
		dotsRedDark.rectTransform.localScale = new Vector3(widthRedDark/dotTotal, 1, 1);
		dotsWhite.rectTransform.localScale = new Vector3(widthWhite/dotTotal, 1, 1);
		dotsWhiteDark.rectTransform.localScale = new Vector3(widthWhiteDark/dotTotal, 1, 1);
		dotsRedHollow.rectTransform.localScale = new Vector3(widthRedHollow/dotTotal, 1, 1);
		dotsContainer.localScale = new Vector3(dotTotal/10f, 1, 1);
	}
	public void UpdateMembers()
	{
		if(!hasAuthority) return;
		if(members.Count > wantedMembers)//Has excess members
		{
			foreach(Squad s in GameObject.FindObjectsOfType<Squad>())
			{
				while(s.team==team
					&& s.members.Count < s.wantedMembers)//While found squad needs more members
				{
					if(!isServer)
						CmdReassign(members[0].unitId, s.id); //Myst be before reassignment on this side, or members[0] will not exist
					members[0].Reassign(s);
					if(members.Count <= wantedMembers)//If member count now in equilibrium
					{
						UpdateDots();
						s.UpdateDots();
						goto findSqsDone;
					}
				}
			}
			findSqsDone:;
			/*int toRemove = members.Count-wantedMembers;
			foreach(FPControl fp in members)
			{
				if(toRemove <= 0) break;
				if(fp.control.GetType() != typeof(PlayerControl)) //If is an AI
				{
					fp.Unassign();
					toRemove--;
				}
			}*/
		}else if(members.Count < wantedMembers)//Need more members
		{
			foreach(Squad s in GameObject.FindObjectsOfType<Squad>())
			{
				while(s.team==team
					&& s.members.Count > s.wantedMembers)//While found has excess members
				{
					s.members[0].Reassign(this);
					if(!isServer)
						CmdReassign(s.members[0].unitId, this.id);
					if(members.Count >= wantedMembers)//If member count now in equilibrium
					{
						UpdateDots();
						s.UpdateDots();
						goto findSqsDone;
					}
				}
			}
			findSqsDone:;
			/*foreach(FPControl fp in GameObject.FindObjectsOfType<FPControl>())
			{
				if(members.Count >= wantedMembers)
					break;
				if(fp.squad==null)
					fp.Assign(this);
			}*/
		}
		if(members.Count==wantedMembers)
			if(Manager.needMembers.Contains(this))
				Manager.needMembers.Remove(this);
		else if(! Manager.needMembers.Contains(this))
			Manager.needMembers.Add(this);
		
		UpdateDots();
	}
	[Command] void CmdReassign(int unit, int squad)
	{
		FPControl reFP = null;
		Squad reSquad = null;
		foreach(FPControl fp in GameObject.FindObjectsOfType<FPControl>())
			if(fp.unitId == unit/* && fp.team == team*/) // Units have team-independent IDs
			{
				reFP = fp;
				break;
			}
		foreach(Squad sq in GameObject.FindObjectsOfType<Squad>())
			if(sq.id==squad && sq.team==team)
			{
				reSquad = sq;
				break;
			}
		if(reFP!=null && reSquad!=null)
		{
			reFP.Reassign(reSquad);
		}else
			Debug.LogError("Reassign Components not found:\nFPControl: "+reFP+"\nSquad: "+reSquad);
	}

	void Update () {
		Debug.DrawRay(destination, Vector3.up, Color.green);
		if(isServer) //Figure out respawns
		{
			if(transform.position == prevPos
				&& (team==0 ?(territoryId > gameManager.contestedPoint) :(territoryId < gameManager.contestedPoint)) )
			{
				timeSinceMoveOrDamage += Time.deltaTime;
				if(timeSinceMoveOrDamage > 5f)
				{
					foreach(FPControl fp in members)
					{
						if(fp.isDead)
						{
							if(fp.hasAuthority)
								fp.Respawn();
							else
							{
								if(fp.isServer)
									fp.RpcRespawn();
								else
									fp.CmdRespawn();
							}
							break;
						}
					}
					timeSinceMoveOrDamage = 0f;
				}
			}else
				timeSinceMoveOrDamage = 0f;
			prevPos = transform.position;
		}

		if(shouldBeServer != isServer)//===============================================================================================================
			return;
		
		SetDestination();

		bool someAlive = false;
		foreach(FPControl fp in members)
		{
			if(!fp.isDead)
				someAlive = true;
		}
		if(!someAlive && waypoints.Count>0 && waypoints[0].placed) //TODO: allow squad dragging when all members are dead
		{
			transform.position = waypoints[0].transform.position;
			NextWaypoint();
		}else if(signal==0 && waypoints.Count>0 && waypoints[0].placed) //If can move to next point
		{
			bool shouldNext = true;
			Vector3 avgPos = Vector3.zero;
			float numAlive = 0;
			foreach(FPControl fp in members)
			{
				if(!fp.isDead)
				{
					numAlive++;
					avgPos += fp.transform.position;
					if(Vector3.Distance(destination, fp.transform.position) > AIControl.moveRadius)
					{
						shouldNext = false; //If outside moveRadius of destination, do not go to next waypoint.
					}
				}
			}
			avgPos /= numAlive; //members.Count;
			avgPos.Scale(new Vector3(1,0,1)); // Y is always 0

			if(shouldNext)
			{
				transform.position = Vector3.Lerp(transform.position, waypoints[0].transform.position, Time.deltaTime *2f);

				if(Vector3.Distance(transform.position, waypoints[0].transform.position) < 1f)
					NextWaypoint();
			}else if(!isCommanded)
				transform.position = Vector3.Lerp(transform.position, avgPos, Time.deltaTime *5f);

		}

		Waypoint prevW = null;
		foreach(Waypoint w in waypoints)
		{
			if(prevW==null) //If first pass
			{
				//line.SetPositions(new Vector3[]{transform.position+Vector3.down, w.transform.position+Vector3.down});
				//line.enabled = true;
				w.line.SetPositions(new Vector3[]{this.transform.position+Vector3.down, w.transform.position+Vector3.down});
			}else
			{
				/*prevW.*/w.line.SetPositions(new Vector3[]{prevW.transform.position+Vector3.down, w.transform.position+Vector3.down});
				//prevW.line.enabled = true;
				//if(w.index == waypoints.Count-1)
				//	w.line.enabled = false; THIS HAS A LINE NOW // Last waypoint has no line
			}
			prevW = w;
		}

		if(highlighted)
		{
			marker.color = Manager.miniEmphasis;
			highlighted = false;
		}else
			marker.color = Color.white;
			
	}

	public void RemoveWaypoint(int index) //Removes a waypoint and all subsequent waypoints
	{
		while(waypoints.Count > index)
		{
			Destroy(waypoints[index].gameObject);
			waypoints.RemoveAt(index);
		}
	}

	private void NextWaypoint()
	{
		foreach(Waypoint wp in waypoints)
			wp.index--;
		
		signal = waypoints[0].signal;

		Destroy(waypoints[0].gameObject);
		waypoints.RemoveAt(0);
	}

	public void SetDestination()
	{
		if(isCommanded)
			destination = this.transform.position;
		else if(signal == 0)
		{
			if(waypoints.Count == 0 || !waypoints[0].placed)
				destination = this.transform.position;
			else
				destination = waypoints[0].transform.position;
		}else
			destination = this.transform.position;

		for(float i=0; i<10; i++)
		{
			NavMeshHit hit;
			if(NavMesh.SamplePosition(new Vector3(destination.x, i, destination.z), out hit, 1f, NavMesh.AllAreas))
			{
				destination = hit.position;
				break;
			}
		}

		if(!isServer)
			CmdSyncDestination(destination);

		//if(!isServer) //TODO: is necessary?
		//	CmdSetDestination(destination);
	}
	/*[Command] void CmdSetDestination(Vector3 dest)
	{Debug.Log("CmdSetDestination: "+dest);
		destination = dest;
	}*/
}
