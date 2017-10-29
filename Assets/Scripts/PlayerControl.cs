using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : Controller {

	public FPControl player;
	public float mouseMulti;
	public Canvas HUDCanvas;
	public Camera minimapCamera;

	[HideInInspector]public Transform ragdoll;
	[HideInInspector]public Vector3 lerpCamPos;
	static float lerpCamSpeed = 2f; 

	public override input GetInput()
	{
		input i = new input();

		Vector2 moveVec = Vector3.zero;
		if(Input.GetKey(KeyCode.W))
			moveVec.y++;
		if(Input.GetKey(KeyCode.S))
			moveVec.y--;
		if(Input.GetKey(KeyCode.D))
			moveVec.x++;
		if(Input.GetKey(KeyCode.A))
			moveVec.x--;
		moveVec.Normalize();
		i.move=moveVec;

		i.mouse = new Vector2(Input.GetAxis("Mouse X")*mouseMulti, Input.GetAxis("Mouse Y")*mouseMulti);
		i.jump = Input.GetKey(KeyCode.Space);
		i.crouch = Input.GetKey(KeyCode.LeftShift);
		i.mouseL = Input.GetMouseButton(0);//Previously MouseButtonDown; with firing cooldown, this now unneeded

		return i;
	}

	void Update()
	{
		if(ragdoll!=null)
		{
			Camera.main.transform.rotation = Quaternion.Lerp(Camera.main.transform.rotation, Quaternion.LookRotation(ragdoll.position-Camera.main.transform.position), Time.deltaTime*lerpCamSpeed);
			Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, ragdoll.position + lerpCamPos, Time.deltaTime*lerpCamSpeed);
		}

		if(player!=null)
		{
			Vector3 mmVec = player.transform.position;
			mmVec.y = minimapCamera.transform.position.y;
			minimapCamera.transform.position = mmVec;
		}
	}
}
