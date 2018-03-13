using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Networking.NetworkSystem;

#if ENABLE_UNET

public class JoinGameHUD : MonoBehaviour
{

	public RectTransform rectTransform;

	[HideInInspector] public NetworkManager manager = null;

	private bool deployed = false;
	private float height = -1f;
	private int mode = 0;
	private const int HOST = 1;
	private const int CLIENT = 2;
	private PlayerContValues playerValues;

	void Start()
	{
		DontDestroyOnLoad(gameObject);
	}

	void OnEnable()
	{
		SceneManager.sceneLoaded += OnLevelLoaded;
	}

	void OnLevelLoaded(Scene scene, LoadSceneMode loadMode)
	{
		try{
		StopCoroutine("FindManager"); // In case it's still running from the previous scene
		}catch(System.Exception e) {}
		StartCoroutine("FindManager");

	}

	IEnumerator FindManager()
	{
		while(manager == null)
		{
			manager = GameObject.FindObjectOfType<NetworkManager>();
			yield return null;
		}
		GotManager();
	}

	void GotManager()
	{
		if(mode>0)
			manager.GetComponent<MyLocalDiscovery>().Initialize();

		switch(mode)
		{
		case HOST:
			manager.StartHost();
			manager.GetComponent<MyLocalDiscovery>().StartAsServer();
			break;
		case CLIENT:
			//manager.StartClient(); // Done later by MyLocalDiscovery
			bool TEMP = manager.GetComponent<MyLocalDiscovery>().StartAsClient();
			Debug.Log("");
			break;
		}

		if(mode > 0)
		{
			playerValues = GameObject.FindObjectOfType<PlayerContValues>();
			playerValues.joinHud = this;
			if(mode==HOST)
				StartCoroutine("WaitForClient");
		}
	}

	IEnumerator WaitForClient()
	{
		while(manager.numPlayers < 2)
		{//Debug.Log(manager.numPlayers);
			yield return null;
		}
		StartPlaying();
	}

	void Update()
	{
		if(rectTransform != null)
		{
			height = Mathf.Lerp(height, deployed ?0f :-1f, Time.deltaTime * 5f);
			rectTransform.anchoredPosition = new Vector2(0, Screen.height * height);
		}
	}

	public void PlayButton()
	{
		deployed = true;
	}
	public void BackButton()
	{
		deployed = false;
	}
	public void QuitButton()
	{
		Application.Quit();
	}

	public void HostGame()
	{
		mode = HOST;
		SceneManager.LoadScene("Workshop");
	}
	public void JoinGame()
	{
		mode = CLIENT;
		SceneManager.LoadScene("Workshop");
	}

	/*void OnPlayerConnected(NetworkPlayer player) // PART OF LEGACY API; NEVER CALLED
	{
		StartPlaying();
	}
	void OnConnectedToServer()
	{
		StartPlaying();
	}*/
	public void StartPlaying()
	{
		playerValues.waitingPane.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, float.MaxValue);
	}

	public void BackWhileWaiting()
	{
		if(mode == HOST)
			manager.StopHost();
		else if(mode == CLIENT)
			manager.StopClient();
		manager.GetComponent<MyLocalDiscovery>().StopBroadcast(); // Stops listening and broadcasting
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		SceneManager.LoadScene("Scenes/Main Menu");
		Destroy(gameObject);
	}	

	/*void Update()
	{
		if (!NetworkClient.active && !NetworkServer.active && manager.matchMaker == null)
		{
			if (Input.GetKeyDown(KeyCode.S))
			{
				manager.StartServer();
			}
			if (Input.GetKeyDown(KeyCode.H))
			{
				manager.StartHost();
			}
			if (Input.GetKeyDown(KeyCode.C))
			{
				manager.StartClient();
			}
		}
		if (NetworkServer.active && NetworkClient.active)
		{
			if (Input.GetKeyDown(KeyCode.X))
			{
				manager.StopHost();
			}
		}
	}*/
}
#endif //ENABLE_UNET
