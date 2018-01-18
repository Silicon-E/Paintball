using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour {

	public LineRenderer line;

	[HideInInspector] public Squad squad;
	[HideInInspector] public int index;
	[HideInInspector] public int signal = 0; //The signal id for which to wait at this waypoint (0 is none)
	[HideInInspector] public bool highlighted = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		/*line.SetPosition(0, transform.position);    HANDLED BY Squad.cs

		if(squad.waypoints[index+1] != null)
			line.SetPosition(1, squad.waypoints[index+1].transform.position);
		else
			line.SetPosition(1, transform.position);*/
	}
}
