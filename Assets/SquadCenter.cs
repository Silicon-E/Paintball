using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquadCenter : MonoBehaviour {

	public Squad squad; //Parent squad

	void OntriggerEnter(Collider other)
	{
		if(other.tag == "Territory") //If other is a territory collider
			squad.territoryId = other.GetComponentInParent<Point>().pointId;
	}
}
