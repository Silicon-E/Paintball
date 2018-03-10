using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour {

	public Image pointsFrame;
	public Text winBanner;
	public Text winEcho;

	//[HideInInspector]
	[SyncVar(hook="WinUpdate")]
	public int winningTeam; //Set by final points on server
	public int contestedPoint = 0;

	void Start ()
	{
		if(isServer)
			winningTeam = -1;
	}

	void WinUpdate(int newWinTeam)
	{
		winningTeam = newWinTeam;

		if(winningTeam != -1)
		{
			winBanner.text = (winningTeam==0) ?"P1 WINS" :"P2 WINS";
			winEcho.text = (winningTeam==0) ?"P1 WINS" :"P2 WINS";
			winBanner.color = Manager.teamColors[winningTeam];
			winEcho.color = Manager.teamColors[winningTeam];
			winBanner.enabled = true;
			winEcho.enabled = true;
			StartCoroutine("WinAnim");
		}
	}
	IEnumerator WinAnim()
	{
		float t = 0f;
		while(t < 2f)
		{
			pointsFrame.rectTransform.anchoredPosition = new Vector3(0,   160*t -40,   0);
			winEcho.color = new Color(winEcho.color.r, winEcho.color.g, winEcho.color.b,
				1-(t*0.5f));
			winEcho.rectTransform.localScale = Vector3.one * (1+ t*0.5f);

			t += Time.deltaTime;
			yield return null;
		}

		t=0f;
		while(t < 5f)
		{
			t += Time.deltaTime;
			yield return null;
			GameObject.FindObjectOfType<JoinGameHUD>().BackWhileWaiting(); // Disconnects and exits to menu
		}
	}

	public override void OnStartClient()
	{
		if(!isServer)
			StartCoroutine("StartPlaying"); //GameObject.FindObjectOfType<JoinGameHUD>().StartPlaying();
	}
	IEnumerator StartPlaying()
	{
		while(GameObject.FindObjectOfType<JoinGameHUD>() == null)
			yield return null;
		GameObject.FindObjectOfType<JoinGameHUD>().StartPlaying();
	}

	/*void OnDisconnectedFromServer(NetworkDisconnection conn)
	{
		if(isServer)
		{
			//TODO: spawn persistent dialog saying "opponent disconnected"
			GameObject.FindObjectOfType<JoinGameHUD>().BackWhileWaiting(); // Disconnects and exits to menu
		}
	}*/
}
