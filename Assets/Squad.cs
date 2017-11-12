using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Squad : MonoBehaviour {

	public PlayerControl control;
	public LineRenderer line;

	//[HideInInspector]
	public List<Waypoint> waypoints = new List<Waypoint>();

	//[HideInInspector]
	public List<FPControl> members;

	static string[] CODES = {"alpha","bravo","charlie","delta","echo","foxtrot","india","kilo","november","oscar","quebec","romeo","sierra","tango","victor","xray","yankee","zulu"};
	static string[] ABBR = {"a","b","c","d","e","f","i","k","n","o","q","r","s","t","v","x","y","z"};

	void Start () {
		
	}

	void Update () {
		Waypoint prevW = null;
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
