﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Networking;

public class PlayerControl : Controller {

	public FPControl player = null;
	public float mouseMulti;
	public GameObject squadPrefab;

	public Canvas HUDCanvas;
	public Image hitIndicator;
	public DmgIndicator dmgIndicator;
	public Camera minimapCamera;
	public RectTransform minimapMask;
	public RectTransform minimapImage;
	public RectTransform minimapCanvas;
	public GameObject commandStuff;
	public Button newSqButton;
	public Image pauseDarken;

	[HideInInspector]public int team = 0;
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

	void Awake()
	{
		

		//NetworkServer.SpawnWithClientAuthority(gameObject, connectionToClient);
	}

	void Start()
	{
		PlayerContValues vals = FindObjectOfType<PlayerContValues>();

		HUDCanvas = vals.HUDCanvas;
		hitIndicator = vals.hitIndicator;
		dmgIndicator = vals.dmgIndicator;
		minimapCamera = vals.minimapCamera;
		minimapMask = vals.minimapMask;
		minimapImage = vals.minimapImage;
		minimapCanvas = vals.minimapCanvas;
		commandStuff = vals.commandStuff;
		newSqButton = vals.newSqButton;
		pauseDarken = vals.pauseDarken;
		maxSquads = Squad.CODES.Length;
		newSqButton.onClick.AddListener(NewSquad);

		if(isServer)
			team = 0;
		else
			team = 1;
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
		if(!isLocalPlayer)
			return;


		if(Input.GetKeyDown(KeyCode.Tab) && !paused)//Can't tab in/out while paused
		if(player!=null || !commandMode) //If no player and in command mode, no tabbing out
				commandMode = !commandMode;
		if(Input.GetKeyDown(KeyCode.Escape))
			paused = !paused;
		
		if(commandMode || paused)
			cursorEngaged = false;
		else cursorEngaged = true;
		Cursor.visible = !cursorEngaged;
		Cursor.lockState = cursorEngaged ?CursorLockMode.Locked :CursorLockMode.None;//    Debug.Log(paused+", "+ Cursor.lockState);

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
			if(sqPlacing==null) //Can't do mouse interaction while placing a squad
			{
				FPControl pointingUnit = null;
				Squad pointingSquad = null;
				Debug.DrawRay(mouseToWorld(), Vector3.up, Color.black);
				Collider[] founds = Physics.OverlapSphere(mouseToWorld(), 0.1f);
				foreach(Collider f in founds)
				{
					if(f.tag=="Squad")
					{
						pointingSquad = f.GetComponent<Squad>();
						pointingSquad.highlighted = true;
					}else if(f.tag=="Unit Select"/*  &&  f.GetComponentInParent<FPControl>()!=null*/)
					{
						if(Input.GetMouseButtonDown(0) && f.GetComponentInParent<FPControl>().team==team)
						{
							pointingUnit = f.GetComponentInParent<FPControl>();
						}

						break;
					}
				}//Debug.Log("pointingSquad: "+pointingSquad);
				if(pointingSquad != null)
				{//Debug.Log(Input.GetAxisRaw("Mouse ScrollWheel"));
					pointingSquad.wantedMembers = Mathf.Clamp(pointingSquad.wantedMembers + (int)Input.GetAxis("Mouse ScrollWheel"), 1, 10);
					pointingSquad.UpdateMembers();
				}
				if(pointingUnit != null)
				{
					if(player!=null)
					{
						player.control = player.gameObject.GetComponent<AIControl>(); //Make previous controlled player an AI
						player.hitIndicator = null;
						player.dmgIndicator = null;
					}
					if(!isServer)//Server value is taken care of in the command
					{
						player = pointingUnit;
						player.control = this;
						player.hitIndicator = hitIndicator;
						player.dmgIndicator = dmgIndicator;
					}
					//if(NetworkServer.ReplacePlayerForConnection(connectionToClient, player.gameObject, 0))
					//	Debug.Log("switched");
					HUDCanvas.enabled = true; //can't switch to dead unit, so re-enable HUD, as it may have been disabled by death

					//NetworkServer.ReplacePlayerForConnection(connectionToClient, f.gameObject, playerControllerId);
					CmdReplacePlayer(pointingUnit.netId);
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

	[Command]
	void CmdReplacePlayer(NetworkInstanceId netId)
	{Debug.Log("Replace Player Command");

		if(player!=null)
			player.GetComponent<NetworkIdentity>().RemoveClientAuthority(connectionToClient);

		GameObject obj = NetworkServer.FindLocalObject(netId);
		player = obj.GetComponent<FPControl>();

		//NetworkServer.ReplacePlayerForConnection(connectionToClient, obj, playerControllerId);
		obj.GetComponent<NetworkIdentity>().AssignClientAuthority(connectionToClient);
		obj.GetComponent<FPControl>().control = this; //obj.GetComponent<AIControl>();

		//Set values in 'player' for the host
		player.control = this;
		player.hitIndicator = hitIndicator;
		player.dmgIndicator = dmgIndicator;

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
