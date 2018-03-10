using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
//using NUnit.Framework.Constraints;

public class MyLocalDiscovery : NetworkDiscovery {

	bool isStartedClient = false;

	/*public override void Initialize()
	{
		base.Initialize();
		int s = base.broadcastKey;
	}*/

	public override void OnReceivedBroadcast(string fromAddress, string data)
	{
		//base.OnReceivedBroadcast(fromAddress, data); Does not change behavior; Docs suggest that it contains no vanilla implementation
		//Debug.Log(fromAddress + data);

		if(!isStartedClient)
		{
			NetworkManager.singleton.networkAddress = fromAddress;
			NetworkManager.singleton.StartClient();

			isStartedClient = true;
		}
	}

	/*void OnStartClient() Not called
	{
		isStartedClient = true;
	}*/
}
