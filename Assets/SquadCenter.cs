using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquadCenter : MonoBehaviour {

	public Squad squad; //Parent squad

	void OntriggerEnter(Collider other)
	{
		if(other.tag == "Territory") //If other is a territory collider
		{
			Point parentPoint = other.GetComponentInParent<Point>();
			if(parentPoint != null)
				squad.territoryId = parentPoint.pointId;
			else
				squad.territoryId = squad.team * 100; //If this is a free-floating territory trigger, it is assumed to be the spawn terriyorry and so always makes respawns possible.
		}
	}
}
