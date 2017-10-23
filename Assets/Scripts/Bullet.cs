using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Specialized;
using UnityEditor;

public class Bullet : MonoBehaviour {
	public LineRenderer line;

	static float mark2 = 0.1f;//Time to fire second raycast
	static float markFinal = 0.2f;//Time to fire max length raycast
	static float range1 = 2f;//Range of first raycast
	static float range2 = 4f;//Range of second raycast
	static float rangeFinal = 100f;//Max range
	private float speed;

	Vector3 origin;//Camera where fired
	Vector3 direction;//Direction fired
	Vector3 start;//Location of weapon muzzle

	Vector3 end;//Location bullet is going towards
	float count = 0f;
	float prevCount = 0f;

	// Use this for initialization
	void Start () {
		
	}

	public void Init(Vector3 o, Vector3 d, int team)
	{
		origin = o;
		direction = d;
		GradientColorKey[] c = {
			new GradientColorKey(Manager.teamColors[team], 0f),
			new GradientColorKey(Manager.teamColors[team], 1f)};
		GradientAlphaKey[] a = {
			new GradientAlphaKey(1,0),
			new GradientAlphaKey(1,1)};
		Gradient g = new Gradient();
		g.SetKeys(c,a);
		line.colorGradient = g;

		start = transform.position;//Start at spawn location
		speed = range2 / mark2;//Meters per second
	}
	
	// Update is called once per frame
	void FixedUpdate ()
	{
		count += Time.deltaTime;
		if(count*speed>rangeFinal){
			Destroy(gameObject); return; }
		RaycastHit hit;
		if(prevCount==0f)
		{
			if(Physics.Raycast(origin, direction, out hit, range1))
			{
				end = hit.point;
				rangeFinal = hit.distance;
			}else //If no raycast hit at first, set end to max range
				end = origin + direction*rangeFinal;
		}else if(prevCount<mark2 && count>=mark2)
		{
			if(Physics.Raycast(origin, direction, out hit, range2))
			{
				end = hit.point;
				rangeFinal = hit.distance;
			}
		}else if(prevCount<markFinal && count>=markFinal)
		{
			if(Physics.Raycast(origin, direction, out hit, rangeFinal))
			{
				end = hit.point;
				rangeFinal = hit.distance;
			}
		}
		prevCount = count;

		Vector3 showDir = (end-start).normalized;
		Vector3 posFar = start + showDir*count*speed;
		Vector3 posNear = start + showDir*Mathf.Max(count*speed -2f,  0f);//Bullet length: 2
		line.SetPositions(new Vector3[] {posFar, posNear});
	}
}
