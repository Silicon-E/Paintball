using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniCamImitator : MonoBehaviour {

	public Camera thisCam;

	public Camera miniCam;


	void Update ()
	{
		thisCam.orthographicSize = miniCam.orthographicSize;
	}
}
