using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.CodeDom;

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
	[HideInInspector] public bool highlighted = false;

	//[HideInInspector]
	public List<Waypoint> waypoints = new List<Waypoint>();
	[HideInInspector] public int signal;//The signal this suad should wait for before continuing to the next waypoint (0 is none)

	//[HideInInspector]
	public List<FPControl> members;
	[HideInInspector] public int wantedMembers = 1;

	[HideInInspector] public static string[] CODES = {"alpha","bravo","charlie","delta","echo","foxtrot","india","kilo","november","oscar","quebec","romeo","sierra","tango","victor","xray","yankee","zulu"};
	static string[] ABBR = {"a","b","c","d","e","f","i","k","n","o","q","r","s","t","v","x","y","z"};

	public int nameInd;

	private bool shouldBeServer; //Which value of isServer will allow this squad to be manipulated & displayed

	void Start () {
		shouldBeServer = (team==0);
		wantedMembers = members.Count; //Account for preassigned mambers

		if(shouldBeServer != isServer)
		{
			GetComponent<Collider>().enabled = false;
			GetComponent<SpriteRenderer>().enabled = false;

			GetComponentInChildren<Canvas>().enabled = false;
			GetComponentInChildren<SpriteRenderer>().enabled = false;
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

	void Update () {
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
		for(int i=index; i<waypoints.Count; i++)
		{
			Destroy(waypoints[i].gameObject);
			waypoints.RemoveAt(i);
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
}
