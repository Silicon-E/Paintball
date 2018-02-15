using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuCamera : MonoBehaviour {

	public AnimationCurve pitchCurve;
	public float pitchAmp;
	public float yawSpeed;

	private float curveTime = 0f;
	private float maxCurveTime;

	// Use this for initialization
	void Start () {
		maxCurveTime = pitchCurve.keys[pitchCurve.length-1].time;
	}
	
	// Update is called once per frame
	void Update () {
		transform.rotation = Quaternion.Euler(pitchCurve.Evaluate(curveTime) *pitchAmp, Time.time*yawSpeed, 0);

		curveTime += Time.deltaTime;
		if(curveTime > maxCurveTime)
			curveTime = 0f;
	}
}
