using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.CodeDom;
using System.Collections.Specialized;

public class Squad : NetworkBehaviour {

	public int team;
	[HideInInspector] public PlayerControl control; //No PlayerControls should exist in the hierarchy, so no need to be visible in inspector
	//public LineRenderer line;
	public SpriteRenderer marker;
	public Text label;
	public Sprite[] dots;
	public RectTransform dotsContainer;
	public Image dotsWhite;
	public Image dotsRed;
	public int nameInd;

	[HideInInspector] public bool highlighted = false;
	[HideInInspector] [SyncVar] public Vector3 destination;

	//[HideInInspector]
	public List<Waypoint> waypoints = new List<Waypoint>();
	[HideInInspector] public int signal;//The signal this suad should wait for before continuing to the next waypoint (0 is none)

	//[HideInInspector]
	public List<FPControl> members;
	[HideInInspector] public int wantedMembers = 1;

	[HideInInspector] public static string[] CODES = {"alpha","bravo","charlie","delta","echo","foxtrot","india","kilo","november","oscar","quebec","romeo","sierra","tango","victor","xray","yankee","zulu"};
	static string[] ABBR = {"a","b","c","d","e","f","i","k","n","o","q","r","s","t","v","x","y","z"};

	private bool shouldBeServer; //Which value of isServer will allow this squad to be manipulated & displayed

	void Start () {
		shouldBeServer = (team==0);
		wantedMembers = members.Count; //Account for preassigned mambers

		if(isServer)
			StartCoroutine("GetAuthority");

		if(shouldBeServer != isServer)
		{
			GetComponent<Collider>().enabled = false;
			GetComponent<SpriteRenderer>().enabled = false;

			GetComponentInChildren<Canvas>().enabled = false;
			foreach(SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
				sr.enabled = false;
			//GetComponentInChildren<LineRenderer>().enabled = false;
		}

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
					break;
				}
			}
			yield return null;
		}
	}

	public void UpdateName()
	{
		label.text = ABBR[nameInd].ToUpper();
	}
	void UpdateDots()
	{
		dotsWhite.sprite = dots[wantedMembers];
		dotsRed.sprite = dots[Mathf.Max(0, wantedMembers - members.Count)];
		float memOverWant = members.Count/(float)wantedMembers;
		if(float.IsNaN(memOverWant) || float.IsInfinity(memOverWant))
			memOverWant = 0;
		dotsWhite.rectTransform.localScale = new Vector3(memOverWant, 1, 1);
		dotsRed.rectTransform.localScale = new Vector3(1f-memOverWant, 1, 1);
		dotsContainer.localScale = new Vector3(wantedMembers/10f, 1, 1);
	}
	public void UpdateMembers()
	{
		if(members.Count > wantedMembers)//Has excess members
		{
			foreach(Squad s in GameObject.FindObjectsOfType<Squad>())
			{
				while(s.members.Count < s.wantedMembers)//While found squad needs more members
				{
					members[0].Reassign(s);
					if(members.Count <= wantedMembers)//If member count now in equilibrium
						goto findSqsDone;
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
				while(s.members.Count > s.wantedMembers)//While found has excess members
				{
					s.members[0].Reassign(this);
					if(members.Count >= wantedMembers)//If member count now in equilibrium
						goto findSqsDone;
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

	void Update () { Debug.Log(team+": "+destination);
		if(shouldBeServer != isServer)
			return;
		Debug.Log(team+": correct side");
		SetDestination();

		if(signal==0 && waypoints.Count>0 && waypoints[0].placed) //If can move to next point
		{
			bool shouldNext = true;
			Vector3 avgPos = Vector3.zero;
			foreach(FPControl fp in members)
			{
				avgPos += fp.transform.position;
				if(Vector3.Distance(destination, fp.transform.position) > AIControl.moveRadius)
				{
					shouldNext = false; //If outside moveRadius of destination, do not go to next waypoint.
				}
			}
			avgPos /= members.Count;
			avgPos.Scale(new Vector3(1,0,1)); // Y is always 0

			if(shouldNext)
			{
				transform.position = Vector3.Lerp(transform.position, waypoints[0].transform.position, Time.deltaTime *2f);

				if(Vector3.Distance(transform.position, waypoints[0].transform.position) < 1f)
					NextWaypoint();
			}else
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
		while(waypoints.Count < index)
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
		if(signal == 0)
		{
			if(waypoints.Count == 0 || !waypoints[0].placed)
				destination = this.transform.position;
			else
				destination = waypoints[0].transform.position;
		}else
			destination = this.transform.position;

		if(!isServer) //TODO: is necessary?
			CmdSetDestination(destination);
	}
	[Command] void CmdSetDestination(Vector3 dest)
	{Debug.Log("CmdSetDestination: "+dest);
		destination = dest;
	}
}
