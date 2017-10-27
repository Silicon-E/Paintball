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
		public input(bool v)
		{
			move=Vector2.zero;
			mouse=Vector2.zero;
			jump=false;
			crouch=false;
			mouseL=false;
		}
	}

	public abstract input GetInput();// {return new input();}

	//FPControl fp;
}
