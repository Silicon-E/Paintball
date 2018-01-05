﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.ComponentModel.Design.Serialization;
using System.Runtime.InteropServices;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class FPControl : NetworkBehaviour {

	public Controller control;
	public Collider collider;
	public Collider miniSelect;
	public Rigidbody physics;
	public GameObject player = null;
	public float height;
	public float moveV;
	public float maxMove;
	public float jumpI;
	public float jumpPortion;//Portion of horix movement converted to vertical movement
	public float crouchSpeed;
	public GameObject camera;
	public float camHeight;
	public LayerMask onGroundMask;
	public GameObject ragdollPrefab;
	public GameObject miniXPrefab;
	public GameObject bulletPrefab;
	public float fireDelay;
	public DmgIndicator dmgIndicator;
	public Image hitIndicator;
	public SpriteRenderer miniHighlight;

	//[HideInInspector]
	/*[SyncVar]*/ public int team;
	//[HideInInspector]
	public Squad squad = null;

	[HideInInspector]public float fireCooldown = 0f;
	//private bool cursorEngaged = true;
	private float crouchFactor = 1f;
	private bool onGround = false;
	[SyncVar] private int health = 100;//TODO: replace with deliberated helath system
	private bool isDead = false;
	void OnStartLocalPlayer() {Debug.Log("local player");}//TODO: remove
	void Start ()
	{
		//if(!hasAuthority)
			//Init(isServer ?1 :0);//Init with other team

		//if(!isServer)
			Init(team);
	}

	public void Init(int t/*GameObject p, Rigidbody r, Collider c, int n*/)
	{
		team = t;
		gameObject.layer = LayerMask.NameToLayer(Manager.teamLayers[team]);

		GetComponent<MeshRenderer>().material = Manager.teamMaterials[team];
		foreach(SpriteRenderer s in GetComponentsInChildren<SpriteRenderer>())
			s.color = Manager.teamColors[team];

		player = gameObject;// p;
		physics = GetComponent<Rigidbody>();// r;
		collider = GetComponent<Collider>();// c;
	}

	public void Assign(Squad newSq)
	{
		if(squad!=null)
		{
			Debug.LogError("Can't assign FPControl; already assigned");
			return;
		}
		squad = newSq;
		squad.members.Add(this);
	}
	public void Reassign(Squad newSq)
	{
		Unassign();
		Assign(newSq);
	}
	private void Unassign()//Should only be called by Reassign()
	{
		squad.members.Remove(this);
		squad = null;
	}

	public bool Damage(int amount, Vector3 dir, int isFromServer = -1)//Called to damage this controller, isFromServer defaults to be "sent" from other side
	{Debug.Log("Damage(), isFromServer: "+isFromServer);
		if(isFromServer == -1) isFromServer = (!isServer) ?1 :0;//Translate
		if(isFromServer == (isServer ?1 :0)) return false;//Do not run based on own message
		Debug.Log("didn't abort");
		health-=amount;//TODO: replace with deliberated damage system
		if(dmgIndicator!=null) dmgIndicator.Add(dir);
		if(health<=0)
		{
			GameObject ragdoll = GameObject.Instantiate(ragdollPrefab, player.transform.position, player.transform.rotation);
			GameObject miniX = GameObject.Instantiate(miniXPrefab, player.transform.position, miniXPrefab.transform.rotation);
			miniX.GetComponent<MiniX>().Init(Manager.teamColors[team]);
			if(control is PlayerControl)
			{
				((PlayerControl)control).ragdoll = ragdoll.GetComponent<Ragdoll>().root.transform;
				((PlayerControl)control).lerpCamPos = Camera.main.transform.forward * -3f + Vector3.up;
				((PlayerControl)control).HUDCanvas.enabled = false;
			}
			foreach(Rigidbody r in ragdoll.GetComponentsInChildren<Rigidbody>())
				r.velocity = physics.velocity;
			//TODO: Make this object ragdoll instead of spawning prefab

			collider.enabled = false;
			physics.isKinematic = true;
			miniSelect.enabled = false;

			isDead = true;

			return true;
		}else
		{
			if(control is AIControl) ((AIControl)control).TookDamage(dir);
			return false;
		}
		CmdDamageAlert(amount, dir, (isServer ?1 :0));
	}
	[Command] void CmdDamageAlert(int amount, Vector3 dir, int isFromServer)
	{Debug.Log("CmdDamageAlert");
		RpcDamageAlert(amount, dir, isFromServer);
	}
	[ClientRpc] void RpcDamageAlert(int amount, Vector3 dir, int isFromServer)
	{Debug.Log("RpcDamageAlert");
		Damage(amount, dir, isFromServer);
	}

	void Update ()
	{//Debug.Log(health);
		



		if(player==null || !hasAuthority)
		{
			return;
		}



		if(!(control is PlayerControl) || isDead) //If not under player control or dead
			miniHighlight.enabled = false;
		else
			miniHighlight.enabled = true;

		if(!isDead  &&  (hasAuthority  ||  isServer))//Must not be dead. Must either have Client authority or be on the Server
		{
			Controller.input inp = control.GetInput();

			if(control is PlayerControl)
			{
				camera.transform.Rotate(new Vector3(0, inp.mouse.x, 0), Space.World);//Rotate Horizontal
				float preY = camera.transform.localEulerAngles.y;
				camera.transform.Rotate(new Vector3(-inp.mouse.y, 0, 0));//Rotate Vertical
				float rawX = camera.transform.localEulerAngles.x;
				if(camera.transform.localEulerAngles.z>90)
				{
					camera.transform.Rotate(new Vector3(0, 180, 0), Space.World);
					camera.transform.Rotate(new Vector3(0, 0, 180));
					if(camera.transform.localEulerAngles.x < 180)
						camera.transform.Rotate(90-camera.transform.localEulerAngles.x, 0, 0);
					else
						camera.transform.Rotate(270-camera.transform.localEulerAngles.x, 0, 0);
				}

				Camera.main.transform.position = camera.transform.position;
				Camera.main.transform.rotation = camera.transform.rotation;
			}
			camera.transform.parent = null;
			transform.Rotate(new Vector3(0,camera.transform.rotation.eulerAngles.y - player.transform.rotation.eulerAngles.y,0));
			camera.transform.parent = player.transform;

			//player.transform.RotateAround(player.transform.position, Vector3.up, camera.transform.localRotation.eulerAngles.y);
			//camera.transform.RotateAround(camera.transform.position, Vector3.up, -camera.transform.localRotation.eulerAngles.y);
			Debug.DrawRay(camera.transform.position, camera.transform.forward, Color.blue);

			fireCooldown -= Time.deltaTime;
			if(hitIndicator!=null)
			{
				Color c = hitIndicator.color; c.a -= 2f*Time.deltaTime;
				hitIndicator.color = c;
			}
			if(inp.mouseL && fireCooldown<=0f)
			{
				fireCooldown = fireDelay;
				GameObject newBullet = Instantiate(bulletPrefab, player.transform.position, Quaternion.identity);//TODO: instantiate at muzzle
				newBullet.GetComponent<Bullet>().Init(control, camera.transform.position, camera.transform.forward, team, hitIndicator);

				/*Debug.Log("Bang");
				RaycastHit hit;
				//Check if Raycast hits anything
				if (Physics.Raycast(camera.transform.position, camera.transform.TransformDirection(Vector3.forward), out hit, 100,  1 << LayerMask.NameToLayer("Targets")))
				{
					//sends message to Target.cs that target has been hit
					GameObject hitObject = hit.transform.gameObject;
					Destroy(hitObject);

					//Sends hit info to console saying if the raycast "Hit" anything thats not on layer 2, and what it hit
					Debug.Log("Hit: " + hit.collider);

					//Draws Raycast line, Green if it collided with anything on layer 2
					Debug.DrawRay(camera.transform.position, camera.transform.TransformDirection(Vector3.forward) * 100, Color.green);
				}
				else
				{
					//Draws raycast line, Red if it didn't collide with anything on layer 2
					Debug.DrawRay(camera.transform.position,camera.transform.TransformDirection(Vector3.forward) * 100 ,Color.red);
				}*/
			}

		//}else
		//	Cursor.lockState = CursorLockMode.None;
			camera.transform.position = player.transform.position+new Vector3(0,camHeight,0);
		}
	}


	//END UPDATE, BEGIN FIXEDUPDATE ---------------------------------------------------------------------------------------------------------------------


	void FixedUpdate ()
	{
		if(player==null || !hasAuthority)
		{
			return;
		}

		Controller.input inp;
		//if(cursorEngaged || !(control is PlayerControl))
		//{
			inp = control.GetInput();


			//                 position                                          halfextents                           direction     rotation        maxDist  layerMask

		/*}else //end of if engaged
		{
			inp = new Controller.input();//Neutral controls
			inp.move = Vector2.zero;
			inp.mouse = Vector2.zero;
			inp.jump = false;
			inp.crouch = false;
			inp.mouseL = false;

			//collider.material.staticFriction = 0.5f; Taken care of below
			//collider.material.dynamicFriction = 0.5f;
		}*/

		//Whether engaged or not
		RaycastHit hit;
		Debug.DrawRay(player.transform.position+new Vector3(0,(1-crouchFactor)*height*0.5f,0), Vector3.down*(height*crouchFactor*0.5f +0.01f), Color.red);
		if(Physics.SphereCast(player.transform.position+new Vector3(0,(1-crouchFactor)*height*0.5f,0), 0.5f, Vector3.down, out hit, height*crouchFactor*0.5f +0.01f-0.5f, onGroundMask))
		{
			onGround = true;

			Vector3 horizMove = new Vector3(physics.velocity.x, 0f, physics.velocity.z);//X & Z movement
			Vector3 moveVec = Quaternion.Euler(0,camera.transform.rotation.eulerAngles.y,0) * new Vector3(inp.move.x, 0f, inp.move.y).normalized;
			moveVec*=crouchFactor;//The more crouched (max at time of writing: 0.5f), the slower

			if(moveVec != Vector3.zero)
			{//Debug.Log("goin");
				if(horizMove.magnitude<=maxMove)
				{
					collider.material.staticFriction = 0f;
					collider.material.dynamicFriction = 0f;
					physics.velocity = moveVec*moveV;
				}else
				{
					collider.material.staticFriction = 0.5f;
					collider.material.dynamicFriction = 0.5f;
					if(Vector3.Dot(horizMove, moveVec)<1f)
						physics.AddForce(moveVec*moveV, ForceMode.Acceleration);//Applies velocity input as acceleration (a cheap fix)
				}

				/*if(horizMove.magnitude<=maxMove || Vector3.Dot(horizMove, moveVec)<1f)
					{
						physics.AddForce(moveVec*moveV, ForceMode.Acceleration);
					}*/
			}else
			{//Debug.Log("stoppin");
				//physics.velocity = Vector3.zero;
				collider.material.staticFriction = 10f;
				collider.material.dynamicFriction = 10f;
			}

			if(inp.jump && crouchFactor>0.75f)//If crouchFactor grater than halfway down
			{
				physics.AddForce((Vector3.up*(jumpI + horizMove.magnitude*jumpPortion))*physics.mass, ForceMode.Impulse);
				Vector3 newV = new Vector3(physics.velocity.x*(1-jumpPortion), physics.velocity.y, physics.velocity.z*(1-jumpPortion));
				physics.velocity = newV;
			}
		}else
		{
			onGround = false;

			collider.material.staticFriction = 0f;
			collider.material.dynamicFriction = 0f;
		}

		crouchFactor += (inp.crouch ?-1 :1)*crouchSpeed*Time.deltaTime;
		crouchFactor = Mathf.Clamp(crouchFactor, 0.5f, 1f);
		float dDist = ((CapsuleCollider)collider).center.y - ((CapsuleCollider)collider).height*0.5f;
		((CapsuleCollider)collider).height = height*crouchFactor;
		((CapsuleCollider)collider).center = new Vector3(0, (1-crouchFactor)*height*0.5f, 0);
		dDist -= ((CapsuleCollider)collider).center.y - ((CapsuleCollider)collider).height*0.5f;
		if(onGround) player.transform.position += new Vector3(0,dDist,0);//new Vector3(0,crouchSpeed*Time.deltaTime*height*0.5f *(inp.crouch ?-1 :1),0);
	}
}
