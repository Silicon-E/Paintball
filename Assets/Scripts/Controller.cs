using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Controller : MonoBehaviour {

	public struct input
	{
		public Vector2 move;
		public Vector2 mouse;
		public bool jump;
		public bool crouch;
		public bool mouseL;
	}

	public abstract input GetInput();// {return new input();}
}
