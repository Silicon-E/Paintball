using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_UNET

namespace UnityEngine.Networking
{
	[RequireComponent(typeof(NetworkManager))]
	//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public class NetworkManagerHUD : MonoBehaviour
	{
		[HideInInspector] public NetworkManager manager;

		private int mode;
		private const int HOST = 1;
		private const int CLIENT = 2;

		void Awake()
		{
			manager = GetComponent<NetworkManager>();
		}

		public void HostGame()
		{
			mode = HOST;
		}

		public void JoinGame()
		{
			mode = CLIENT;
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
};
#endif //ENABLE_UNET
