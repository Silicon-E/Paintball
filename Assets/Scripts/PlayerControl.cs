using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : Controller {

	public FPControl player;
	public float mouseMulti;
	public Canvas HUDCanvas;
	public Camera minimapCamera;
	public RectTransform minimapMask;
	public RectTransform minimapImage;
	public RectTransform minimapCanvas;

	[HideInInspector]public Transform ragdoll;
	[HideInInspector]public Vector3 lerpCamPos;
	static float lerpCamSpeed = 2f; 
	bool commandMode = true;//Whether in minimap-based command mode
	float commandLerp = 1;
	static float lerpPerSec = 8;

	private int squadInd = 0;
	private int numSquads = 0;

	public override input GetInput()
	{
		input i = new input();
		if(commandMode) return i;

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

		if(player!=null && !commandMode)
		{
			Vector3 mmVec = player.transform.position;
			mmVec.y = minimapCamera.transform.position.y;
			minimapCamera.transform.position = mmVec;
		}

		if(commandMode)
		{
			ragdoll = null;
			commandLerp += Time.deltaTime*lerpPerSec;
			commandLerp = Mathf.Clamp(commandLerp, 0, 1);


		}else
		{
			commandLerp -= Time.deltaTime*lerpPerSec;
			commandLerp = Mathf.Clamp(commandLerp, 0, 1);
		}
		minimapMask.localScale = Vector3.Lerp(Vector3.one, new Vector3(Screen.width/200f, Screen.height/200f, 0), commandLerp);
		Vector3 newImgScale = minimapImage.localScale;
		if(minimapMask.localScale.x > minimapMask.localScale.y)
			newImgScale.y = minimapMask.localScale.x / minimapMask.localScale.y;
		else
			newImgScale.x = minimapMask.localScale.y / minimapMask.localScale.x;
		minimapImage.localScale = Mathf.Max(minimapMask.localScale.x,minimapMask.localScale.y) * new Vector3(1/minimapMask.localScale.x, 1/minimapMask.localScale.y, 1);//newImgScale;
		minimapCanvas.sizeDelta = 200* new Vector2(minimapMask.localScale.x, minimapMask.localScale.y);

		//minimapMask.localScale = Vector3.one * ( commandLerp*(Mathf.Max(Screen.width, Screen.height)-200)/200 +1);//   /HUDCanvas.referencePixelsPerUnit
		minimapCamera.orthographicSize = 15*Mathf.Max(minimapMask.localScale.x, minimapMask.localScale.y);

		if(Input.GetKeyDown(KeyCode.Tab))
			commandMode = !commandMode;
	}
}
