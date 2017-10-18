using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : Controller {

	public float mouseMulti;

	// Use this for initialization
	void Start () {
		
	}

	public input getInput()
	{
		input i = new input();

		Vector2 moveVec = Vector3.zero;
		if(Input.GetKey(KeyCode.W))
			moveVec.y++;
		if(Input.GetKey(KeyCode.S))
			moveVec.y++;
		if(Input.GetKey(KeyCode.D))
			moveVec.y++;
		if(Input.GetKey(KeyCode.A))
			moveVec.y++;

		moveVec.Normalize();

		input.look = new Vector2(Input.GetAxis("Mouse X")*mouseMulti, Input.GetAxis("Mouse Y")*mouseMulti);
		input.jump = Input.GetKey(KeyCode.Space);
		input.crouch = Input.GetKey(KeyCode.LeftShift);
		input.mouseL = Input.GetMouseButtonDown(0);
	}
}
