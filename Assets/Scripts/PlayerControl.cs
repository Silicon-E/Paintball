using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEditor;

public class PlayerControl : Controller {

	public FPControl player = null;
	public float mouseMulti;
	public GameObject squadPrefab;
	public GameObject waypointPrefab;
	public GameObject healthDotPrefab;

	public Canvas HUDCanvas;
	public Image hitIndicator;
	public DmgIndicator dmgIndicator;
	public Slider healthSlider;
	public Image healthSliderBG;
	public Camera minimapCamera;
	public RectTransform minimapMask;
	public RectTransform minimapImage;
	public RectTransform minimapCanvas;
	public GameObject commandStuff;
	public Button newSqButton;
	public Canvas pauseCanvas;
	public GameManager gameManager;

	[HideInInspector]public int team = 0;
	[HideInInspector]public bool hasStarted = false;
	[HideInInspector]public Transform ragdoll;
	[HideInInspector]public Vector3 lerpCamPos;
	static float lerpCamSpeed = 2f; 
	bool commandMode = true;//Whether in minimap-based command mode
	float commandLerp = 1;
	static float lerpPerSec = 8;

	private int squadInd = 3; //NUMBER OF SQUADS THAT BEGIN ON THE FIELD
	private int newSquadId = 0; //Use negative numbers for pre-spawned squads
	private int numSquads = 0;
	private int maxSquads;
	private List<Squad> squads = new List<Squad>();
	private bool cursorEngaged = true;
	private bool paused = false;
	private Squad sqPlacing = null;
	private Waypoint wpPlacing = null;
	private bool isPlacing = false;
	private KeyCode[] numpadCodes = {KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4,
		KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9};
	private KeyCode[] alphaNumCodes = {KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
		KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9};
	private float healthDotWait = 0f;

	void Awake()
	{
		

		//NetworkServer.SpawnWithClientAuthority(gameObject, connectionToClient);
	}

	void Start()
	{
		PlayerContValues vals = FindObjectOfType<PlayerContValues>();
		if(isLocalPlayer)
			vals.localPlayerControl = this;

		gameManager = vals.gameManager;
		HUDCanvas = vals.HUDCanvas;
		hitIndicator = vals.hitIndicator;
		dmgIndicator = vals.dmgIndicator;
		healthSlider = vals.healthSlider;
		healthSliderBG = vals.healthSliderBG;
		minimapCamera = vals.minimapCamera;
		minimapMask = vals.minimapMask;
		minimapImage = vals.minimapImage;
		minimapCanvas = vals.minimapCanvas;
		commandStuff = vals.commandStuff;
		newSqButton = vals.newSqButton;
		pauseCanvas = vals.pauseCanvas;
		maxSquads = Squad.CODES.Length;
		newSqButton.onClick.AddListener(NewSquad);

		if(isLocalPlayer) //If on own side
			team = isServer ?0 :1;
		else //If on other side
			team = isServer ?1 :0;

		hasStarted = true;


		healthSlider.fillRect.GetComponent<Image>().color = Manager.teamColors[team];
		healthSliderBG.color = Color.Lerp(Manager.teamColors[(team==0) ?1 :0], Color.black, 0.5f);
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
		if(Input.GetKeyDown(KeyCode.Escape)  &&  !paused)
			paused = true;
		
		if(commandMode || paused)
			cursorEngaged = false;
		else cursorEngaged = true;
		Cursor.visible = !cursorEngaged;
		Cursor.lockState = cursorEngaged ?CursorLockMode.Locked :CursorLockMode.None;//    Debug.Log(paused+", "+ Cursor.lockState);

		if(paused)
		{
			pauseCanvas.enabled = true;

			//TODO: pause menu logic
		}else
		{
			pauseCanvas.enabled = false;
			if(isPlacing)
			{
				if(sqPlacing!=null)
					sqPlacing.transform.position = mouseToWorld(/*new Vector2(Screen.width, Screen.height)*/);
				else if(wpPlacing!=null)
					wpPlacing.transform.position = mouseToWorld();
			}
		}

		if(commandLerp==1)
			commandStuff.SetActive(commandMode);
		if(commandMode)
		{
			if(Input.GetMouseButtonDown(0) && isPlacing)
			{
				if(wpPlacing != null)
					wpPlacing.placed = true;
				isPlacing = false;
				sqPlacing = null;
				wpPlacing = null;
			}else if(Input.GetMouseButtonDown(1) && isPlacing) // Right-click to cancel
			{
				if(sqPlacing != null)
				{
					DeleteSquad(sqPlacing);
					//Destroy(sqPlacing.gameObject);
					//numSquads--;
				}
				if(wpPlacing != null)
				{
					Destroy(wpPlacing.gameObject);
					wpPlacing.squad.waypoints.Remove(wpPlacing);

					wpPlacing = null;
				}
			}else if(!isPlacing) //Can't do mouse interaction while placing a squad or waypoint
			{
				FPControl pointingUnit = null;
				Squad pointingSquad = null;
				Waypoint pointingWayp = null;
				Debug.DrawRay(mouseToWorld(), Vector3.up, Color.black);
				Collider[] founds = Physics.OverlapSphere(mouseToWorld(), 0.1f);
				foreach(Collider f in founds)
				{
					if(f.tag=="Squad")
					{
						pointingSquad = f.GetComponent<Squad>();
					}else if(f.tag=="Waypoint")
					{
						pointingWayp = f.GetComponent<Waypoint>();
					}else if(f.tag=="Unit Select"/*  &&  f.GetComponentInParent<FPControl>()!=null*/)
					{
						if(f.GetComponentInParent<FPControl>().team==team)
						{
							pointingUnit = f.GetComponentInParent<FPControl>();
						}

						break;
					}
				}

				if(pointingUnit != null)
				{
					pointingUnit.highlighted = true;

					if(Input.GetMouseButtonDown(0))
					{
						if(player!=null)
						{
							player.control = player.gameObject.GetComponent<AIControl>(); //Make previous controlled player an AI
							player.hitIndicator = null;
							player.dmgIndicator = null;
							player.squad.isCommanded = false;
						}
						//if(!isServer)//Server value is taken care of in the command
						//{
						player = pointingUnit;
						player.control = this;
						player.hitIndicator = hitIndicator;
						player.dmgIndicator = dmgIndicator;
						player.gunMesh.enabled = false;
						player.charMesh.enabled = false;
						player.squad.isCommanded = true;
						//}
						//if(NetworkServer.ReplacePlayerForConnection(connectionToClient, player.gameObject, 0))
						//	Debug.Log("switched");
						HUDCanvas.enabled = true; //can't switch to dead unit, so re-enable HUD, as it may have been disabled by death

						//NetworkServer.ReplacePlayerForConnection(connectionToClient, f.gameObject, playerControllerId);
						// DOES NOT WORK; CANNOT RUN ON CLIENT GetComponent<NetworkIdentity>().AssignClientAuthority(connectionToClient);//TODO: does this solve the client not being able to send?
						CmdReplacePlayer(pointingUnit.netId);
					}else if(Input.GetMouseButtonDown(1)) //Right-click; remove player authority
					{
						if(pointingUnit == player)
						{
							if(player!=null)
							{
								player.control = player.gameObject.GetComponent<AIControl>(); //Make previous controlled player an AI
								player.hitIndicator = null;
								player.dmgIndicator = null;
								player.gunMesh.enabled = true;
								player.charMesh.enabled = true;
								player.squad.isCommanded = false;
							}
							player = null;

							CmdRemovePlayer();
						}
					}
				}
				else if(pointingSquad != null)
				{
					pointingSquad.highlighted = true;

					if(Input.GetAxis("Mouse ScrollWheel") != 0f)
					{
						pointingSquad.wantedMembers = Mathf.Clamp(pointingSquad.wantedMembers + (int)Input.GetAxis("Mouse ScrollWheel"), 0/*1*/, 10);
						pointingSquad.UpdateMembers();
					}
					if(Input.GetMouseButtonDown(0) && pointingSquad.waypoints.Count==0) //If left-click and no waypoints
					{
						NewWaypoint(pointingSquad);
					}
					for(int i=0; i<numpadCodes.Length; i++)
					{
						if(Input.GetKeyDown(numpadCodes[i]) || Input.GetKeyDown(alphaNumCodes[i]))
						{
							pointingSquad.signal = i;
							break;
						}
					}
					if(Input.GetKeyDown(KeyCode.Delete)) //Remove Squad
					{
						DeleteSquad(pointingSquad);
					}
				}
				else if(pointingWayp != null)
				{
					pointingWayp.highlighted = true;

					if(Input.GetMouseButtonDown(0) && pointingWayp.squad.waypoints.Count-1==pointingWayp.index) //If left-click and is final waypoint
					{
						NewWaypoint(pointingWayp.squad);
					}
					if(Input.GetMouseButtonDown(1)) //If right-click, destroy
						pointingWayp.squad.RemoveWaypoint(pointingWayp.index);
					
					for(int i=0; i<numpadCodes.Length; i++)
					{
						if(Input.GetKeyDown(numpadCodes[i]) || Input.GetKeyDown(alphaNumCodes[i]))
						{
							pointingWayp.signal = i;
							break;
						}
					}
				}
			}
		}else
		{
			if(wpPlacing != null)
				wpPlacing.placed = true;
			isPlacing = false;
			sqPlacing = null;
			wpPlacing = null;
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
		if(player != null)
		{
			if(player.health < healthSlider.value)
			{
				healthDotWait += Time.deltaTime;
				if(healthDotWait > 0.05f)
				{
					GameObject dotObj = Instantiate(healthDotPrefab, healthSlider.transform);
					dotObj.GetComponent<Image>().color = Manager.teamColors[team];
					dotObj.GetComponent<RectTransform>().anchoredPosition = new Vector3((healthSlider.value*3.20f)-20, Random.Range(-40, 40), 0);
					healthDotWait = 0f;
				}
			}

			healthSlider.value = Mathf.Lerp(healthSlider.value, player.health, Time.deltaTime*3f);

			/*float interpPerSec = 100f;
			if(Mathf.Abs(player.health-healthSlider.value)  <  interpPerSec*Time.deltaTime)
			{
				healthSlider.value = player.health;
			}else
			{
				healthSlider.value += interpPerSec*Time.deltaTime * Mathf.Sign(player.health - healthSlider.value);
			}*/
		}

		if(commandMode)
		{
			ragdoll = null;
			commandLerp += Time.deltaTime*lerpPerSec;
			commandLerp = Mathf.Clamp(commandLerp, 0, 1);

			if(player==null || player.isDead)
			{
				Vector2 moveVec = Vector2.zero;
				if(Input.GetKey(KeyCode.W))
					moveVec.y++;
				if(Input.GetKey(KeyCode.S))
					moveVec.y--;
				if(Input.GetKey(KeyCode.D))
					moveVec.x++;
				if(Input.GetKey(KeyCode.A))
					moveVec.x--;
				moveVec.Normalize();
				minimapCamera.transform.position += 20f* Time.deltaTime*new Vector3(moveVec.x, 0f, moveVec.y);//TODO: maybe replace '5f' with inspector parameter
			}
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


		foreach(Manager.HitInfo hit in Manager.bulletHits)//This necessary to get around Unity's built-in refusal to let Bullet send Cmds
		{
			if(isServer)
				RpcDamageAlert(hit.unitId, hit.amount, hit.dir, hit.point, hit.newHealth);
			else // if is client
				CmdDamageAlert(hit.unitId, hit.amount, hit.dir, hit.point, hit.newHealth);
		}
		Manager.bulletHits.Clear();
	}

	public void UnPause()
	{
		if(paused)
			paused = false;
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
		//CLIENT ONLY obj.GetComponent<FPControl>().control = this; //obj.GetComponent<AIControl>();

		//Set values in 'player' for the host
		//CLIENT ONLY player.control = this;
		//CLIENT ONLY player.hitIndicator = hitIndicator;
		//CLIENT ONLY player.dmgIndicator = dmgIndicator;
	}
	[Command]
	void CmdRemovePlayer()
	{
		if(player!=null)
			player.GetComponent<NetworkIdentity>().RemoveClientAuthority(connectionToClient);
	}
		

	void NewSquad()
	{
		if(!isLocalPlayer) //Called for both instances, but only the local player should run this
			return;

		if(numSquads >= maxSquads
				|| !hasStarted)
			return;

		Vector3 spawnPos = mouseToWorld(/*new Vector2(Screen.width, Screen.height)*/);

		CmdSpawnSquad(spawnPos, isServer);
	}
	[Command] void CmdSpawnSquad(Vector3 spawnPos, bool isFromServer)
	{
		GameObject newObj = GameObject.Instantiate(squadPrefab, spawnPos, squadPrefab.transform.rotation);
		//NetworkServer.Spawn(newObj);
		newObj.layer = squadPrefab.layer;

		Squad newSq = newObj.GetComponent<Squad>();

		newSq.team = team;
		newSq.id = newSquadId;
		newSquadId++;

		//NetworkServer.Spawn(newObj);
		NetworkServer.SpawnWithClientAuthority(newObj, this.connectionToClient);

		/*if(isFromServer)
		{
			newSq.nameInd = squadInd;
			squadInd++;
			numSquads++;
			newSq.UpdateName();

			sqPlacing = newSq;
			isPlacing = true;
		}*///else
		//	RpcSpawnSquadCallback(newSq.id/*newObj*/);
	}
	[ClientRpc] void RpcSpawnSquadCallback(int id/*GameObject newObj*/)     //THIS IS NO LONGER CALLED (INTENTIONAL)
	{
		if(isServer)
			return;
		//newObj.layer = squadPrefab.layer;
		//Squad newSq = newObj.GetComponent<Squad>();
		Squad newSq = null;
		foreach(Squad s in GameObject.FindObjectsOfType<Squad>())
		{
			if(s.id==id && s.team==team)
			{
				newSq = s;
				break;
			}
		}
		newSq.gameObject.layer = squadPrefab.layer;

		newSq.nameInd = squadInd;
		squadInd++;
		numSquads++;
		newSq.UpdateName();

		sqPlacing = newSq;
		isPlacing = true;
	}
	public void NewSquadSpawned(Squad newSq)
	{
		newSq.gameObject.layer = squadPrefab.layer;

		newSq.nameInd = squadInd;
		squadInd++;
		numSquads++;
		newSq.UpdateName();

		squads.Add(newSq);
		sqPlacing = newSq;
		isPlacing = true;
	}
	protected void DeleteSquad(Squad delSq)
	{
		CmdDestroySquad(delSq.id, delSq.team);
	}
	[Command] protected void CmdDestroySquad(int sqId, int sqTeam)
	{
		Squad delSq = null;
		foreach(Squad s in GameObject.FindObjectsOfType<Squad>())
		{
			if(s.id==sqId && s.team==sqTeam)
			{
				delSq = s;
				break;
			}
		}

		if(delSq.members.Count == 0) //Only destroy if no members
		{
			NetworkServer.Destroy(delSq.gameObject);
			RpcDestroySquadSuccess(sqTeam);
		}
	}
	[ClientRpc] protected void RpcDestroySquadSuccess(int sqTeam)
	{
		if(team == sqTeam)
		{
			sqPlacing = null;  //While placing, can only destroy squads by cancelling placement, so this is fine.
			isPlacing = false;

			squadInd--;
			numSquads--;
		}
	}

	void NewWaypoint(Squad sq) //Waypoints are not networked, so no messages are necessary.
	{
		GameObject newObj = Instantiate(waypointPrefab);
		Waypoint newWayp = newObj.GetComponent<Waypoint>();
		wpPlacing = newWayp;

		newWayp.squad = sq;
		newWayp.index = sq.waypoints.Count;

		sq.waypoints.Add(newWayp);

		isPlacing = true;
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

	[Command] void CmdDamageAlert(int id, int amount, Vector3 dir, Vector3 point, int newHealth)
	{Debug.Log("CmdDamageAlert");
		DamageAlert(id, amount, dir, point, newHealth);
	}
	[ClientRpc] void RpcDamageAlert(int id, int amount, Vector3 dir, Vector3 point, int newHealth)
	{Debug.Log("RpcDamageAlert");
		if(!isServer)
			DamageAlert(id, amount, dir, point, newHealth);
	}
	void DamageAlert(int id, int amount, Vector3 dir, Vector3 point, int newHealth)
	{Debug.Log("DamageAlert");

		FPControl target = null;
		foreach(FPControl fp in FindObjectsOfType<FPControl>())
		{
			if(fp.unitId == id)
			{
				target = fp;
				break;
			}
		}
		if(target != null)
			target.OnDamage(amount, dir, point, newHealth);
	}
}
