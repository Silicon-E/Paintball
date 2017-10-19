//Code from Unify Wiki; I don't know how it works.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : Singleton<Manager> {
	protected Manager () {} // guarantee this will be always a singleton only - can't use the constructor!

	public string myGlobalVar = "whatever";
}
