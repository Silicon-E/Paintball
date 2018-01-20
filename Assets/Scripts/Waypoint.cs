using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour {

	public LineRenderer line;
	public SpriteRenderer marker;

	[HideInInspector] public Squad squad;
	[HideInInspector] public int index;
	[HideInInspector] public int signal = 0; //The signal id for which to wait at this waypoint (0 is none)
	[HideInInspector] public bool highlighted = false;
	[HideInInspector] public bool placed = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		if(highlighted)
		{
			marker.color = Manager.miniEmphasis;
			highlighted = false;
		}else
			marker.color = Color.white;
	}
}
