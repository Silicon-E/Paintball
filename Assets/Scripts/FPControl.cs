using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.ComponentModel.Design.Serialization;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using UnityEngine.AI;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class FPControl : NetworkBehaviour {

	public int unitId;

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
	public GameObject camPivot;
	public GameObject gunPivot;
	public float camHeight;
	public LayerMask onGroundMask;
	public GameObject ragdollPrefab;
	public GameObject miniXPrefab;
	public GameObject bulletPrefab;
	public float fireDelay;
	public AnimationCurve regenCurve;
	public DmgIndicator dmgIndicator;
	public Image hitIndicator;
	public SpriteRenderer miniHighlight;
	public SpriteRenderer miniSprite;
	public Transform worldMuzzle;
	public SkinnedMeshRenderer charMesh;
	public GameObject charArmature;
	public Material[] teamMaterials;
	public Material[] gunMaterials;
	public MeshRenderer gunMesh;
	public NavMeshAgent agent;
	public Animator animator;

	//[HideInInspector]
	/*[SyncVar]*/ public int team;
	//[HideInInspector]
	public Squad squad = null;

	[HideInInspector]public float fireCooldown = 0f;
	//private bool cursorEngaged = true;
	private float crouchFactor = 1f;
	private bool onGround = false;
	[SyncVar, HideInInspector] public int health = 100;
	[HideInInspector][SyncVar] public bool isDead = false;
	[HideInInspector] public PlayerControl playerControl = null;
	[HideInInspector] public bool highlighted = false;

	[SyncVar] private float rotYaw = 0;
	[SyncVar] private float rotPitch = 0;
	private GameManager gameManager;
	[HideInInspector] [SyncVar] public float timeSinceDamaged = 0f;
	[SyncVar] private float regenAccumulation = 0f;
	private Transform vmMuzzle;
	private Vector3 worldGunLocalPos;

	private Vector3 Flatten(Vector3 inVec)
	{
		Vector3 outVec = new Vector3(inVec.x, inVec.y, inVec.z);
		outVec.Scale(new Vector3(1,0,1));
		return outVec;
	}

	void Start ()
	{
		gameManager = GameObject.FindObjectOfType<GameManager>();
		vmMuzzle = Camera.main.GetComponent<CameraVals>().vmMuzzle;// GetComponentInChildren<Mesh>().GetComponentInChildren<Transform>();
		worldGunLocalPos = gunMesh.transform.localPosition;
		//if(!hasAuthority)
			//Init(isServer ?1 :0);//Init with other team

		//if(!isServer)
			Init(team);
	}

	public void Init(int t/*GameObject p, Rigidbody r, Collider c, int n*/)
	{
		team = t;
		gameObject.layer = LayerMask.NameToLayer(Manager.teamLayers[team]);

		charMesh.material = teamMaterials[team]; //Manager.teamMaterials[team]; I'M NOT GOING TO PUT EVERYTHING IN THE RESOURCES FOLDER FOR THIS
		gunMesh.material = gunMaterials[team];

		foreach(SpriteRenderer s in GetComponentsInChildren<SpriteRenderer>())
			s.color = Manager.teamColors[team];

		player = gameObject;// p;
		physics = GetComponent<Rigidbody>();// r;
		collider = GetComponent<Collider>();// c;

		StartCoroutine("FindPlayerControl", t);
	}
	IEnumerator FindPlayerControl(int t)
	{
		while(playerControl==null)
		{//Debug.Log(gameObject+": playerControl==null");
			foreach(PlayerControl p in FindObjectsOfType<PlayerControl>())
			{
				if(/*p.isLocalPlayer && */p.team==t && p.hasStarted)
				{
					playerControl = p;
					break;
				}
			}
			yield return null;
		}
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

	public bool Damage(int amount, Vector3 dir, Vector3 point) // AUTHORITATIVE;   Called to damage this controller
	{
		if(isDead)
			return false;
		
		health-=amount;
		timeSinceDamaged = 0f;

		if(control is AIControl)
			((AIControl)control).shouldChase = false;

		OnDamage(amount, dir, point);

		if(health<=0)
		{
			if(isServer)
				RpcOnDie(dir, point);
			else
				CmdOnDie(dir, point);

			Die(dir, point);
			return true;
		}else
		{
			if(isServer  &&  control is AIControl)
				((AIControl)control).TookDamage(dir);
			return false;
		}
	}
	public void OnDamage(int amount, Vector3 dir, Vector3 point) // NON-AUTHORITATIVE
	{
		if(dmgIndicator!=null)
			dmgIndicator.Add(dir);

		if(isServer)
			squad.timeSinceMoveOrDamage = 0f; //this var only used on server
	}

	[Command] void CmdOnDie(Vector3 dir, Vector3 point) // NON-AUTHORITATIVE
	{ OnDie(dir, point); }
	[ClientRpc] void RpcOnDie(Vector3 dir, Vector3 point) // NON-AUTHORITATIVE
	{ if(!isServer)
		OnDie(dir, point); }
	public void Die(Vector3 dir, Vector3 point) // AUTHORITATIVE
	{
		if(control is PlayerControl)
		{
			((PlayerControl)control).ragdoll = charArmature.GetComponent<Ragdoll>().root.transform;
			squad.isCommanded = false;
			charMesh.enabled = true;
		}
		if(control is PlayerControl)
		{
			((PlayerControl)control).lerpCamPos = Camera.main.transform.forward * -3f + Vector3.up;
			((PlayerControl)control).HUDCanvas.enabled = false;
		}

		if(hasAuthority)
			isDead = true;
		
		OnDie(dir, point);
	}
	public void OnDie(Vector3 dir, Vector3 point) // NON-AUTHORITATIVE
	{
		collider.enabled = false;
		physics.isKinematic = true;

		miniSelect.enabled = false;
		miniSprite.enabled = false;

		GameObject ragdoll = Ragdoll(true, dir);
		for(float r=0.1f; r<=0.5f; r+= 0.1f)//Check increasingly large spheres up to r=0.5
		{
			foreach(Collider c in Physics.OverlapSphere(point, r))
			{
				if(c.gameObject.layer == LayerMask.NameToLayer("Ragdoll"))
				{
					c.gameObject.GetComponent<Rigidbody>().AddForceAtPosition(-dir*Manager.ragdollImpulse, point, ForceMode.Impulse);
					break;
				}
			}
		}

		GameObject miniX = GameObject.Instantiate(miniXPrefab, player.transform.position, miniXPrefab.transform.rotation);
		miniX.GetComponent<MiniX>().Init(Manager.teamColors[team]);
	}

	protected GameObject Ragdoll(bool isRagdoll, Vector3 dir = default(Vector3))
	{
		if(isRagdoll)
		{
			gunMesh.GetComponent<Rigidbody>().isKinematic = false;
			gunMesh.GetComponent<Rigidbody>().AddForce(-dir*Manager.ragdollImpulse *0.25f, ForceMode.Impulse);
			gunMesh.GetComponent<Collider>().enabled = true;
			gunMesh.transform.parent = null;
			gunMesh.enabled = true;

			foreach(Collider c in charArmature.GetComponentsInChildren<Collider>())
			{
				c.enabled = true;
				c.GetComponent<Rigidbody>().isKinematic = false;
				c.GetComponent<Rigidbody>().velocity = physics.velocity;
			}
			animator.enabled = false;

			GameObject ragdoll = charArmature;//GameObject.Instantiate(ragdollPrefab, player.transform.position, player.transform.rotation);


			return ragdoll;
		}else
		{
			gunMesh.GetComponent<Rigidbody>().isKinematic = true;
			gunMesh.GetComponent<Collider>().enabled = false;
			gunMesh.transform.parent = gunPivot.transform;
			gunMesh.transform.localPosition = worldGunLocalPos;

			foreach(Collider c in charArmature.GetComponentsInChildren<Collider>())
			{
				c.enabled = false;
				c.GetComponent<Rigidbody>().isKinematic = true;
			}
			animator.enabled = true;

			if(control is PlayerControl)
			{
				charMesh.enabled = false;
				gunMesh.enabled = false;
			}else
			{
				charMesh.enabled = true;
				gunMesh.enabled = true;
			}

			return new GameObject();
		}
	}

	public void Respawn() // AUTHORITATIVE; Attempt to respawn
	{
		NavMeshHit hit;
		Vector3 tryPos = squad.transform.position   +   AIControl.moveRadius * new Vector3(UnityEngine.Random.Range(-1f,1f), UnityEngine.Random.Range(-1f,1f), UnityEngine.Random.Range(-1f,1f));
		if(NavMesh.SamplePosition(tryPos, out hit, 10f, NavMesh.AllAreas))
		{
			// Actually respawn

			collider.enabled = true;
			physics.isKinematic = false;

			isDead = false;
			health = 1;
			timeSinceDamaged = 5f; // Takes 5 seconds to start regen instead of 10

			if(isServer)
				RpcOnRespawn();
			else
				CmdOnRespawn();
			
			OnRespawn();

			transform.position = hit.position;
			agent.Warp(hit.position);
			if(isServer)
				RpcWarpAgent(hit.position);
			else
				CmdWarpAgent(hit.position);
		}
	}
	public void OnRespawn() // NON-AUTHORITATIVE
	{
		miniSelect.enabled = true;
		miniSprite.enabled = true;
		if(control is PlayerControl)
			squad.isCommanded = true;
		Ragdoll(false);
	}
	[Command] public void CmdRespawn() // AUTHORITATIVE
	{ Respawn(); }
	[ClientRpc] public void RpcRespawn() // AUTHORITATIVE
	{ if(!isServer)
			Respawn(); }
	[Command] void CmdOnRespawn() // NON-AUTHORITATIVE
	{ OnRespawn(); }
	[ClientRpc] void RpcOnRespawn() // NON-AUTHORITATIVE
	{ if(!isServer)
		OnRespawn(); }

	[Command] public void CmdWarpAgent(Vector3 newPos) // AUTHORITATIVE
	{ agent.Warp(newPos); }
	[ClientRpc] public void RpcWarpAgent(Vector3 newPos) // AUTHORITATIVE
	{ if(!isServer)
		agent.Warp(newPos); }

	void Update ()
	{//Debug.Log(health);
		animator.SetFloat("Speed X", (Quaternion.Euler(0, -rotYaw, 0) * physics.velocity).x);
		animator.SetFloat("Speed Z", (Quaternion.Euler(0, -rotYaw, 0) * physics.velocity).z);
		//animator.SetFloat("Lean X", (Quaternion.Euler(0, -rotYaw, 0) * physics.velocity).x / moveV);
		//animator.SetFloat("Lean Z", (Quaternion.Euler(0, -rotYaw, 0) * physics.velocity).z / moveV);
		animator.SetFloat("Pitch", rotPitch / 90f);

		animator.SetFloat("Speed", Flatten(physics.velocity).magnitude);
		//if(Flatten(physics.velocity).magnitude == 0)
		//	animator.SetFloat("AnimSpeed", 1);
		//else
		//	animator.SetFloat("AnimSpeed", Flatten(physics.velocity).magnitude);
		if(!(control is PlayerControl) || isDead) //If not under player control or dead
			miniHighlight.enabled = false;
		else
			miniHighlight.enabled = true;

		if(player==null || !hasAuthority)
		{
			return;
		}


		timeSinceDamaged += Time.deltaTime;
		regenAccumulation += regenCurve.Evaluate(timeSinceDamaged) * Time.deltaTime;
		health += (int)regenAccumulation;
		health = Mathf.Clamp(health, 0, 100);
		regenAccumulation -= (int)regenAccumulation;

		if(highlighted)
		{
			miniSprite.color = Color.Lerp(Manager.teamColors[team], Color.white, 0.5f);
			highlighted = false;
		}else
			miniSprite.color = Manager.teamColors[team];

		if(!(control is PlayerControl) || isDead) //If not under player control or dead
			;//miniHighlight.enabled = false; DOES NOT RUN WITHOUT AUTHORITY
		else // If under player control and alive
		{
			//miniHighlight.enabled = true;
			squad.transform.position = Vector3.Lerp(squad.transform.position, Flatten(transform.position), Time.deltaTime *5f);
		}

		if(!isDead  &&  (hasAuthority  ||  isServer))//Must not be dead. Must either have Client authority or be on the Server
		{
			Controller.input inp = control.GetInput();

			if(control is PlayerControl)
			{
				rotYaw += inp.mouse.x;
				rotPitch -= inp.mouse.y;
				rotPitch = Mathf.Clamp(rotPitch, -90, 90);
				camPivot.transform.rotation = Quaternion.identity;
				camPivot.transform.Rotate(rotPitch, rotYaw, 0);

				/*camPivot.transform.Rotate(new Vector3(0, inp.mouse.x, 0), Space.World);//Rotate Horizontal
				float preY = camPivot.transform.localEulerAngles.y;
				camPivot.transform.Rotate(new Vector3(-inp.mouse.y, 0, 0));//Rotate Vertical
				float rawX = camPivot.transform.localEulerAngles.x;
				if(camPivot.transform.localEulerAngles.z>90)
				{
					camPivot.transform.Rotate(new Vector3(0, 180, 0), Space.World);
					camPivot.transform.Rotate(new Vector3(0, 0, 180));
					if(camPivot.transform.localEulerAngles.x < 180)
						camPivot.transform.Rotate(90-camPivot.transform.localEulerAngles.x, 0, 0);
					else
						camPivot.transform.Rotate(270-camPivot.transform.localEulerAngles.x, 0, 0);
				}*/

				Camera.main.transform.position = camPivot.transform.position;
				Camera.main.transform.rotation = camPivot.transform.rotation;
			}
			camPivot.transform.parent = null;
			transform.Rotate(new Vector3(0,camPivot.transform.rotation.eulerAngles.y - player.transform.rotation.eulerAngles.y,0));
			camPivot.transform.parent = player.transform;

			gunPivot.transform.rotation = camPivot.transform.rotation;

			//player.transform.RotateAround(player.transform.position, Vector3.up, camPivot.transform.localRotation.eulerAngles.y);
			//camPivot.transform.RotateAround(camPivot.transform.position, Vector3.up, -camPivot.transform.localRotation.eulerAngles.y);
			Debug.DrawRay(camPivot.transform.position, camPivot.transform.forward, Color.blue);

			fireCooldown -= Time.deltaTime;
			if(hitIndicator!=null)
			{
				Color c = hitIndicator.color; c.a -= 2f*Time.deltaTime;
				hitIndicator.color = c;
			}
			if(inp.mouseL && fireCooldown<=0f     && gameManager.winningTeam==-1)//Cannot shoot if a team has won
			{
				fireCooldown = fireDelay;
				GameObject newBullet = Instantiate(bulletPrefab, (control is PlayerControl) ?vmMuzzle.position :worldMuzzle.position, Quaternion.identity);//TODO: instantiate at muzzle
				newBullet.GetComponent<Bullet>().Init(control, camPivot.transform.position, camPivot.transform.forward, team, hitIndicator, false);//not only *an effect
				if(isServer)
				{
					RpcBulletEffect(control.gameObject, camPivot.transform.position, camPivot.transform.forward, team/*, hitIndicator.gameObject*/);
					AIReactToShot();
				}else
					CmdBulletEffect(control.gameObject, camPivot.transform.position, camPivot.transform.forward, team/*, hitIndicator.gameObject*/);
				// MOVED TO CmdNewBullet:
				//GameObject newBullet = Instantiate(bulletPrefab, player.transform.position, Quaternion.identity);//TD: instantiate at muzzle
				//newBullet.GetComponent<Bullet>().Init(control, camPivot.transform.position, camPivot.transform.forward, team, hitIndicator);
				//NetworkServer.SpawnWithClientAuthority(newBullet, playerControl.gameObject);

				/*Debug.Log("Bang");
				RaycastHit hit;
				//Check if Raycast hits anything
				if (Physics.Raycast(camPivot.transform.position, camPivot.transform.TransformDirection(Vector3.forward), out hit, 100,  1 << LayerMask.NameToLayer("Targets")))
				{
					//sends message to Target.cs that target has been hit
					GameObject hitObject = hit.transform.gameObject;
					Destroy(hitObject);

					//Sends hit info to console saying if the raycast "Hit" anything thats not on layer 2, and what it hit
					Debug.Log("Hit: " + hit.collider);

					//Draws Raycast line, Green if it collided with anything on layer 2
					Debug.DrawRay(camPivot.transform.position, camPivot.transform.TransformDirection(Vector3.forward) * 100, Color.green);
				}
				else
				{
					//Draws raycast line, Red if it didn't collide with anything on layer 2
					Debug.DrawRay(camPivot.transform.position,camPivot.transform.TransformDirection(Vector3.forward) * 100 ,Color.red);
				}*/
			}

		//}else
		//	Cursor.lockState = CursorLockMode.None;
			camPivot.transform.position = player.transform.position+new Vector3(0,camHeight,0);
		}
	}
	[Command] void CmdBulletEffect(GameObject cont, Vector3 pos, Vector3 dir, int t/*, GameObject hitInd*/)
	{
		BulletEffect(cont, pos, dir, t);
		AIReactToShot();
	}
	[ClientRpc] void RpcBulletEffect(GameObject cont, Vector3 pos, Vector3 dir, int t/*, GameObject hitInd*/)
	{
		if(!isServer)
			BulletEffect(cont, pos, dir, t);
	}
	void BulletEffect(GameObject cont, Vector3 pos, Vector3 dir, int t)
	{
		GameObject newBullet = Instantiate(bulletPrefab, worldMuzzle.position, Quaternion.identity);//TODO: instantiate at muzzle
		newBullet.GetComponent<Bullet>().Init(cont.GetComponent<Controller>(), pos, dir, t, null/*hitInd.GetComponent<Image>()*/, true);
		//UNESSECARY NetworkServer.SpawnWithClientAuthority(newBullet, playerControl.gameObject);
	}

	protected void AIReactToShot()
	{
		foreach(Collider other in Physics.OverlapSphere(transform.position, 20f))
		{
			if(other.tag=="Unit")
			{
				AIControl ai = other.GetComponent<AIControl>();
				if(ai.target == null)
					ai.TookDamage(this.transform.position - other.transform.position);
			}
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
			Vector3 moveVec = Quaternion.Euler(0,camPivot.transform.rotation.eulerAngles.y,0) * new Vector3(inp.move.x, 0f, inp.move.y).normalized;

			if(control is PlayerControl)
			{
				agent.speed = moveV;
				agent.destination = transform.position;
			}else if(Vector3.Distance(agent.nextPosition, transform.position) > 1f)
			{
				//agent.nextPosition = transform.position;
				agent.speed = 0;
				moveVec = (Flatten(agent.nextPosition) - Flatten(transform.position)).normalized;
			}else
				agent.speed = moveV;
			
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

			if(inp.jump && crouchFactor>0.75f)//If crouchFactor greater than halfway down
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
