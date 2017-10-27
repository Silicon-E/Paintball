using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Security.Permissions;

public class DmgIndicator : MonoBehaviour {

	public int maxTicks = 10;
	public Transform cam;
	public GameObject tickPrefab;
	private Vector3[] hitDirs;//Directions from which hits come
	private Image[] ticks;
	private float[] opacities;
	private int tickInd = 0;

	// Use this for initialization
	void Start () {
		hitDirs = new Vector3[maxTicks];
		ticks = new Image[maxTicks];
		opacities = new float[maxTicks];

		for(int i=0; i<ticks.GetLength(0); i++)
		{
			ticks[i] = GameObject.Instantiate(tickPrefab, transform).GetComponent<Image>();
			ticks[i].enabled = false;
		}
	}
	
	// Update is called once per frame
	void Update () {
		if(cam==null) return;//If cam object has been destroyed (probably player dying)

		//List<Vector2> dispDirs;
		for(int i=0; i<ticks.GetLength(0); i++)
		{
			Vector3 vec = Vector3.ProjectOnPlane(hitDirs[i], /*cam.up-*/cam.forward);
			vec = Quaternion.Inverse(Quaternion.LookRotation(/*cam.up-*/cam.forward)) *vec;
			Vector2 v2 = new Vector2(vec.x,vec.y);
			ticks[i].rectTransform.localRotation = Quaternion.AngleAxis(Vector2.Angle(Vector2.up,v2)*Mathf.Sign(-v2.x), Vector3.forward);

			Color newC = ticks[i].color; newC.a = opacities[i];
			ticks[i].color = newC;
			opacities[i] -= 1f*Time.deltaTime;
			//ticks[i].rectTransform.localRotation = Quaternion.AngleAxis(Vector2.Angle(Vector2.up,v2)*Mathf.Sign(v2.x) +(cam.forward.y>0 ?0 :180), Vector3.forward);
		}
	}

	public void Add(Vector3 dir)
	{
		hitDirs[tickInd] = dir;
		ticks[tickInd].enabled = true;
		opacities[tickInd] = 1f;
		tickInd++;
		if(tickInd==maxTicks) tickInd=0;
	}
}
