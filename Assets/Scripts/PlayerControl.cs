using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerControl : Controller {

	public FPControl player;
	public float mouseMulti;
	public Canvas HUDCanvas;
	public Camera minimapCamera;
	public RectTransform minimapMask;
	public RectTransform minimapImage;
	public RectTransform minimapCanvas;
	public GameObject commandStuff;
	public GameObject squadPrefab;

	[HideInInspector]public Transform ragdoll;
	[HideInInspector]public Vector3 lerpCamPos;
	static float lerpCamSpeed = 2f; 
	bool commandMode = true;//Whether in minimap-based command mode
	float commandLerp = 1;
	static float lerpPerSec = 8;

	private int squadInd = 0;
	private int numSquads = 0;
	private int maxSquads;
	private List<Squad> squads = new List<Squad>();
	private bool cursorEngaged = true;
	private bool paused = false;
	private Squad sqPlacing = null;

	public Button newSqButton;
	public Image pauseDarken;

	void Start()
	{
		maxSquads = Squad.CODES.Length;
		newSqButton.onClick.AddListener(NewSquad);
	}

	public override input GetInput()
	{
		if(!cursorEngaged)
			return new input();

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
		if(Input.GetKeyDown(KeyCode.Tab) && !paused)//Can't tab in/out while paused
			commandMode = !commandMode;
		if(Input.GetKeyDown(KeyCode.Escape))
			paused = !paused;
		
		if(commandMode || paused)
			cursorEngaged = false;
		else cursorEngaged = true;
		Cursor.visible = !cursorEngaged;
		Cursor.lockState = cursorEngaged ?CursorLockMode.Locked :CursorLockMode.None;    Debug.Log(paused+", "+ Cursor.lockState);

		if(paused)
		{
			pauseDarken.enabled = true;
			//TODO: pause menu logic
		}else
		{
			pauseDarken.enabled = false;
			if(sqPlacing!=null)
				sqPlacing.transform.position = mouseToWorld(/*new Vector2(Screen.width, Screen.height)*/);
		}

		if(commandLerp==1)
			commandStuff.SetActive(commandMode);
		if(commandMode)
		{
			if(Input.GetMouseButtonDown(0) && sqPlacing!=null)
			{
				sqPlacing = null;
			}
			if(sqPlacing==null)
			{
				Squad pointingAt = null;
				Debug.DrawRay(mouseToWorld(), Vector3.up, Color.black);
				Collider[] founds = Physics.OverlapSphere(mouseToWorld(), 0.1f);//, LayerMask.NameToLayer("Minimap"));
				foreach(Collider f in founds)
				{
					if(f.tag=="Squad")
					{
						pointingAt = f.GetComponent<Squad>();
						break;
					}
				}//Debug.Log("pointingAt: "+pointingAt);
				if(pointingAt!=null)
				{Debug.Log(Input.GetAxisRaw("Mouse ScrollWheel"));
					pointingAt.wantedMembers = Mathf.Clamp(pointingAt.wantedMembers + (int)Input.GetAxis("Mouse ScrollWheel"), 0, 10);
					pointingAt.UpdateMembers();
				}
			}
		}else
		{
			sqPlacing = null;
		}

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
	}

	void NewSquad()
	{
		if(numSquads >= maxSquads)
			return;

		Vector3 spawnPos = mouseToWorld(/*new Vector2(Screen.width, Screen.height)*/);

		GameObject newObj = GameObject.Instantiate(squadPrefab, spawnPos, squadPrefab.transform.rotation);
		newObj.layer = squadPrefab.layer;

		Squad newSq = newObj.GetComponent<Squad>();
		sqPlacing = newSq;

		sqPlacing.nameInd = squadInd;
		squadInd++;
		numSquads++;
		sqPlacing.UpdateName();
	}

	private Vector3 mouseToWorld(/*Vector2 screen*/)
	{
		/*Vector3 mousePos;
		mousePos.x = Input.mousePosition.x;
		mousePos.y = minimapCamera.pixelHeight - Input.mousePosition.y;
		mousePos.z = minimapCamera.nearClipPlane;
		Vector3 spawnPos = minimapCamera.ScreenToWorldPoint(mousePos);
		spawnPos.Scale(new Vector3(1,0,1));
		return spawnPos;*/

		Vector2 multi = new Vector2(Mathf.Min(1, Screen.width/(float)Screen.height),  Mathf.Min(1, Screen.height/(float)Screen.width));
		return minimapCamera.transform.position + new Vector3(minimapCamera.orthographicSize*(Input.mousePosition.x/(float)Screen.width)*multi.x*2 - multi.x*minimapCamera.orthographicSize,
			-minimapCamera.transform.position.y,
			minimapCamera.orthographicSize*(Input.mousePosition.y/(float)Screen.height)*multi.y*2 - multi.y*minimapCamera.orthographicSize);
		

		/*Vector3 multi = new Vector3(Mathf.Min(1, Screen.width/(float)Screen.height),
			0,
			Mathf.Min(1, Screen.height/(float)Screen.width));
		Vector3 pos = minimapCamera.ScreenToWorldPoint(Input.mousePosition);
		//pos.Scale(multi);
		return pos;*/
	}
}
