using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthDot : MonoBehaviour
{
	public AnimationCurve sizeCurve;
	public float maxScale;
	public float minScale;

	private float timeAlive = 0f;
	private float lifetime;
	private RectTransform rectTransform;
	private float initScale;

	void Start ()
	{
		lifetime = sizeCurve.keys[sizeCurve.length-1].time;
		rectTransform = GetComponent<RectTransform>();
		initScale = Random.Range(minScale, maxScale);
	}

	void Update ()
	{
		timeAlive += Time.deltaTime;

		if(timeAlive > lifetime)
		{
			Destroy(gameObject);
			return;
		}

		rectTransform.localScale = Vector3.one * initScale * sizeCurve.Evaluate(timeAlive);
	}
}
