using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class FPControl : NetworkBehaviour {

	public Collider collider;
	public Rigidbody physics;
	public float moveA;
	public float maxMove;
	public float jumpI;
	public Camera camera;

	public LayerMask onGroundMask;

	private bool cursorEngaged = true;
	public GameObject player = null;
	public PlayerModel model = null;

	private LayerMask pickupMask;
	private Rigidbody pickupObj;
	private Pickup pickupScript;

	void Start ()
	{
		pickupMask = LayerMask.GetMask(new string[] {"Pickup"});
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	public void Init(GameObject p, PlayerModel m, Rigidbody r, Collider c, int n)
	{
		player = p;
		model = m;
		physics = r;
		collider = c;
		camera.gameObject.GetComponent<RedShader>().pNum = n;
		model.Hide();
	}

	void Update ()
	{
		if(Input.GetKeyUp(KeyCode.Escape))
		{
			cursorEngaged = false;
		}
		else if(Input.GetMouseButtonUp(0))
		{
			cursorEngaged = true;
		}
		Cursor.visible = !cursorEngaged;
		Cursor.lockState = cursorEngaged ?CursorLockMode.Locked :CursorLockMode.None;



		if(player==null)
		{
			return;
		}



		if(cursorEngaged)
		{
			Cursor.lockState = CursorLockMode.Locked;

			//Debug.Log("lEuler: "+camera.transform.localEulerAngles.x+", "+camera.transform.localEulerAngles.y+", "+camera.transform.localEulerAngles.z);
			camera.transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X")*mouseMulti, 0), Space.World);//Rotate Horizontal
			float preY = camera.transform.localEulerAngles.y;
			camera.transform.Rotate(new Vector3(-Input.GetAxis("Mouse Y")*mouseMulti, 0, 0));//Rotate Vertical
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
			model.pointer.transform.rotation = camera.transform.rotation;
			//player.transform.RotateAround(player.transform.position, Vector3.up, camera.transform.localRotation.eulerAngles.y);
			//camera.transform.RotateAround(camera.transform.position, Vector3.up, -camera.transform.localRotation.eulerAngles.y);

			Debug.DrawRay(camera.transform.position, camera.transform.forward);

		}else
			Cursor.lockState = CursorLockMode.None;
		camera.transform.position = player.transform.position;
	}

	void FixedUpdate ()
	{
		if(player==null)
		{
			return;
		}


		if(cursorEngaged)
		{
			//                 position                                          halfextents                           direction     rotation        maxDist  layerMask
			if(Physics.BoxCast(player.transform.position/*+new Vector3(0f,-0.5f,0f)*/, new Vector3(0.249f,0.01f,0.249f), Vector3.down, Quaternion.identity, 0.51f, onGroundMask))
			{
				Transform tf = model.pointer.transform;
				tf.rotation = Quaternion.Euler(0, model.pointer.transform.rotation.eulerAngles.y, 0);
				Vector3 moveVec = Vector3.zero;
				if(Input.GetKey(KeyCode.W))
					moveVec += tf.forward;
				if(Input.GetKey(KeyCode.S))
					moveVec += tf.forward*-1f;
				if(Input.GetKey(KeyCode.D))
					moveVec += tf.right;
				if(Input.GetKey(KeyCode.A))
					moveVec += tf.right*-1f;

				moveVec.Normalize();

				Vector3 movingVec = new Vector3(physics.velocity.x, 0f, physics.velocity.z);
				if(moveVec != Vector3.zero)
				{//Debug.Log("goin");
					if(movingVec.magnitude<=maxMove)
					{
						collider.material.staticFriction = 0f;
						collider.material.dynamicFriction = 0f;
					}else
					{
						collider.material.staticFriction = 0.5f;
						collider.material.dynamicFriction = 0.5f;
					}

					if(movingVec.magnitude<=maxMove || Vector3.Dot(movingVec, moveVec)<1f)
					{
						physics.AddForce(moveVec*moveA, ForceMode.Acceleration);
					}
				}else
				{//Debug.Log("stoppin");
					collider.material.staticFriction = 5f;
					collider.material.dynamicFriction = 5f;
				}

				if(Input.GetKeyDown(KeyCode.Space))
					physics.AddForce(Vector3.up*jumpI*physics.mass, ForceMode.Impulse);
			}else
			{
				collider.material.staticFriction = 0f;
				collider.material.dynamicFriction = 0f;
			}
		}//end of if engaged

		if(pickupObj!=null)
		{
			pickupObj.velocity = physics.velocity + (camera.transform.position + camera.transform.forward - pickupObj.position)*20; //   /Time.fixedDeltaTime;
			if(!isServer) CmdUpdateCubePos(pickupObj.gameObject, pickupObj.transform.position);
			//{
			//	pickupScript.pos = pickupObj.transform.position;
			//	pickupScript.rot = pickupObj.transform.rotation;
			//	pickupScript.vel = pickupObj.velocity;
			//}
			if(Vector3.Distance(pickupObj.position, camera.transform.position)>1.25f || pickupScript.side!=(isServer ?1 :0))
			{
				ReleasePickup();
			}
		}
	}

	[Command]
	void CmdUpdateCubePos(GameObject g, Vector3 p)
	{
		NetworkIdentity objNetId = g.GetComponent<NetworkIdentity>();        
		if (!hasAuthority)
		{
			objNetId.AssignClientAuthority(connectionToClient);   
		}
		g.transform.position = p;
	}

	void ReleasePickup()
	{
		pickupObj.velocity = physics.velocity;
		Collider c = pickupObj.GetComponent<Collider>();
		c.material.staticFriction = 1f;
		c.material.dynamicFriction = 1f;
		pickupObj.mass = 1f;

		pickupObj = null;
		pickupScript.side = -1;
		//pickupScript.pos = null;
		//pickupScript.rot = null;
		//pickupScript.vel = null;
		//pickupScript.localPlayerAuthority = false;
	}
}
