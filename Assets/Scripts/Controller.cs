using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour {

	public struct input
	{
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool crouch;
		public bool mouseL;
	}

	public virtual input GetInput() {}
}
