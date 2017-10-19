using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class FPControl : MonoBehaviour {

	public Controller control;
	public Collider collider;
	public Rigidbody physics;
	public float moveV;
	public float maxMove;
	public float jumpI;
	public float jumpPortion;//Portion of horix movement converted to vertical movement
	public Camera camera;

	public LayerMask onGroundMask;

	private bool cursorEngaged = true;
	public GameObject player = null;

	void Start ()
	{
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	public void Init(GameObject p, Rigidbody r, Collider c, int n)
	{
		player = p;
		physics = r;
		collider = c;
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

			Controller.input inp = control.GetInput();
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
			//player.transform.RotateAround(player.transform.position, Vector3.up, camera.transform.localRotation.eulerAngles.y);
			//camera.transform.RotateAround(camera.transform.position, Vector3.up, -camera.transform.localRotation.eulerAngles.y);

			Debug.DrawRay(camera.transform.position, camera.transform.forward, Color.blue);

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
			Controller.input inp = control.GetInput();
			Debug.Log(inp.mouse);
			//                 position                                          halfextents                           direction     rotation        maxDist  layerMask
			RaycastHit hit;
			Debug.DrawRay(player.transform.position, Vector3.down*(1.01f), Color.red);
			if(Physics.SphereCast(player.transform.position, 0.5f, Vector3.down, out hit, 1.01f-0.5f, onGroundMask))
			{
				Vector3 horizMove = new Vector3(physics.velocity.x, 0f, physics.velocity.z);//X & Z movement
				Vector3 moveVec = Quaternion.Euler(0,camera.transform.rotation.eulerAngles.y,0) * new Vector3(inp.move.x, 0f, inp.move.y);

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

				if(inp.jump)
				{
					physics.AddForce((Vector3.up*(jumpI + horizMove.magnitude*jumpPortion))*physics.mass, ForceMode.Impulse);
					Vector3 newV = new Vector3(physics.velocity.x*(1-jumpPortion), physics.velocity.y, physics.velocity.z*(1-jumpPortion));
					physics.velocity = newV;
				}
			}else
			{
				collider.material.staticFriction = 0f;
				collider.material.dynamicFriction = 0f;
			}
		}else //end of if engaged
		{
			collider.material.staticFriction = 0.5f;
			collider.material.dynamicFriction = 0.5f;
		}
	}
}
