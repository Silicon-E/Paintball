using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Squad : MonoBehaviour {

	public PlayerControl control;
	public LineRenderer line;
	public Text label;
	public Sprite[] dots;
	public RectTransform dotsContainer;
	public Image dotsWhite;
	public Image dotsRed;

	//[HideInInspector]
	public List<Waypoint> waypoints = new List<Waypoint>();

	//[HideInInspector]
	public List<FPControl> members;
	[HideInInspector] public int wantedMembers = 1;

	[HideInInspector] public static string[] CODES = {"alpha","bravo","charlie","delta","echo","foxtrot","india","kilo","november","oscar","quebec","romeo","sierra","tango","victor","xray","yankee","zulu"};
	static string[] ABBR = {"a","b","c","d","e","f","i","k","n","o","q","r","s","t","v","x","y","z"};

	public int nameInd;

	void Start () {
		
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
		Waypoint prevW = null;Debug.Log("mem:"+members.Count+" want:"+wantedMembers);
		foreach(Waypoint w in waypoints)
		{
			if(prevW==null)
			{
				line.SetPositions(new Vector3[]{transform.position+Vector3.down, w.transform.position+Vector3.down});
				line.enabled = true;
			}else
			{
				prevW.line.SetPositions(new Vector3[]{prevW.transform.position+Vector3.down, transform.position+Vector3.down});
				prevW.line.enabled = true;
			}
			prevW = w;
		}
	}
}
