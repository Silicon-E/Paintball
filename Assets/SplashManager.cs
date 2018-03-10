using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashManager : MonoBehaviour {

	public float limit;
	private float count = 0f;
	
	// Update is called once per frame
	void Update ()
	{
		count += Time.deltaTime;

		if(count > limit)
			SceneManager.LoadScene("Scenes/Main Menu");
	}
}
