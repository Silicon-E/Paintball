using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : MonoBehaviour {

    // Use this for initialization

    //postition where to "shoot" raycast from
    public Camera cam;
    private GameObject hitObject;


	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            //Sends info to console saying that the fire button was clicked
            Debug.Log("Bang");

            //Create Raycast

            //Store hit info
            RaycastHit hit;


            //Check if Raycast hits anything 
            //Physics.Raycast(Camera postition[Vector 3], Camera direction[Vector 3], hit info, Layer in which to check for collisons[layer 2]);
            if (Physics.Raycast(cam.transform.position, cam.transform.TransformDirection(Vector3.forward), out hit, 100,  1 << LayerMask.NameToLayer("Targets"))){

                //sends message to Target.cs that target has been hit
                hitObject = hit.transform.gameObject;
                Destroy(hitObject);


                //Sends hit info to console saying if the raycast "Hit" anything thats not on layer 2, and what it hit
                Debug.Log("Hit: " + hit.collider);

                //Draws Raycast line, Green if it collided with anything on layer 2
                Debug.DrawRay(cam.transform.position, cam.transform.TransformDirection(Vector3.forward) * 100, Color.green);
            }
            else
            {
                //Draws raycast line, Red if it didn't collide with anything on layer 2
                Debug.DrawRay(cam.transform.position,cam.transform.TransformDirection(Vector3.forward) * 100 ,Color.red);
            }
        }
    }
}
