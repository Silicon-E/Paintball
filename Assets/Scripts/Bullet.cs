using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Specialized;
using UnityEditor;

public class Bullet : MonoBehaviour {
	public LineRenderer line;

	//TODO: do these need to be static?
	static float mark2 = 0.1f;//Time to fire second raycast
	static float markFinal = 0.2f;//Time to fire max length raycast
	static float range1 = 4f;//Range of first raycast
	static float range2 = 8f;//Range of second raycast
	float rangeFinal = 100f;//Max range
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

		line.material.color = Manager.teamColors[team];

		start = transform.position;//Start at spawn location
		speed = range1 / mark2;//Meters per second
	}
	
	// Update is called once per frame
	void FixedUpdate ()
	{
		count += Time.deltaTime;
		//Debug.Log(count);
		if(count*speed>rangeFinal){
			Destroy(gameObject); return; }
		RaycastHit hit;
		if(prevCount==0f)
		{
			if(Physics.Raycast(origin, direction, out hit, range1))
			{
				RayHit(hit);
			}else if(Physics.Raycast(origin, direction, out hit, rangeFinal))//If no raycast hit at first, set end to max-range hit
				end = hit.point;
			else//If still no raycast hit, set end to max range
				end = origin + direction*rangeFinal;
		}else if(prevCount<mark2 && count>=mark2)
		{
			if(Physics.Raycast(origin, direction, out hit, range2))
			{
				RayHit(hit);
			}
		}else if(prevCount<markFinal && count>=markFinal)
		{
			if(Physics.Raycast(origin, direction, out hit, rangeFinal))
			{
				RayHit(hit);
				//Speed up to hit target, modify start and range to keep look consistent (since position is calculated independently of prev position)
				speed = ((end-start).magnitude -range2) / (markFinal-mark2);
				start -= (end-start).normalized * speed*mark2;
				rangeFinal += speed*mark2;
			}
		}
		prevCount = count;

		Vector3 showDir = (end-start).normalized;
		Vector3 posFar = start + showDir*count*speed;
		Vector3 posNear = start + showDir*Mathf.Max(count*speed -2f,  0f);//Bullet length: 2
		line.SetPositions(new Vector3[] {posFar, posNear});
		line.enabled = true;
		//Debug.DrawRay(origin, direction * rangeFinal, Color.red);
		//Debug.DrawRay(end, Vector3.up, Color.cyan);
	}

	private void RayHit(RaycastHit hit)
	{
		end = hit.point;
		rangeFinal = hit.distance;

		FPControl fp = hit.collider.gameObject.GetComponent<FPControl>();
		if(fp!=null)
		{
			if(fp.Damage(10, -direction))//TODO: replace with deliberated damage system
			{
				//TODO: returns true if damage was lethal
			}
		}
	}

	public static float DistToDelay(float dist)//Return delay of bullet at a given range
	{
		if(dist<=range1) return 0f;
		else if(dist<=range2) return mark2;
		else return markFinal;
	}
}
