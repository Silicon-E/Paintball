using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniX : MonoBehaviour {

	public SpriteRenderer sprite;

	public void Init(Color col)
	{
		sprite.color = col;
	}

	void Update () {
		Color col = sprite.color;
		col.a -= Time.deltaTime;
		if(col.a<=0f) Destroy(gameObject);
		sprite.color = col;
	}
}
