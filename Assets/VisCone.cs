using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisCone : MonoBehaviour {

	public FPControl control;

	void OnTriggerEnter(Collider other)
	{
		FPControl otherFP = other.gameObject.GetComponent<FPControl>();
		if(otherFP!=null && otherFP.team != control.team)//If enterer is on other team
		{
			control.unitsInVisCone.Add(otherFP);
		}
	}

	void OnTriggerExit(Collider other)
	{
		FPControl otherFP = other.gameObject.GetComponent<FPControl>();
		if(otherFP!=null && otherFP.team != control.team)//If enterer is on other team
		{
			control.unitsInVisCone.Remove(otherFP);
		}
	}
}
