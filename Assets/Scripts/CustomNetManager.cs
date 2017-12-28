using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CustomNetManager : NetworkManager {
	
	public GameObject unitPrefab;
	public GameObject squadPrefab;

	private int connNum = 0;
	private short controlInd = 0;
	private GameObject[] spawnPositions;
	private GameObject[] squadSpawns;
	private Dictionary<NetworkConnection, int> connTeams = new Dictionary<NetworkConnection, int>();

	public override void OnServerConnect(NetworkConnection conn) //NOTE: only called on the server
	{
		NetworkServer.SetClientReady(conn); //as per default behaivor <-- NOT

		connTeams.Add(conn, connNum);

		//spawnPositions = GameObject.FindGameObjectsWithTag("Spawn Unit "+connTeams[conn]);
		//squadSpawns = GameObject.FindGameObjectsWithTag("Spawn Squad "+connTeams[conn]);

		if(connNum==0)
		{
			connNum = 1;
		}


		//GameObject obj = GameObject.Instantiate(unitPrefab, spawnPositions[0].transform.position, spawnPositions[0].transform.rotation);

		//FPControl newFp = obj.GetComponent<FPControl>();
		//newFp.Init(team);
		////newFp.Assign(newSq);

		//NetworkServer.AddPlayerForConnection(conn, obj, 0);
		////NetworkServer.SpawnWithClientAuthority(obj, conn);
	}
	public override void OnServerDisconnect(NetworkConnection conn) //NOTE: only called on the server
	{
		if(connTeams[conn]==0) //if server disconnects
			connNum = 0;
		else //if client disconnects
			connNum = 1;
	}

	public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
	{
		spawnPositions = GameObject.FindGameObjectsWithTag("Spawn Unit "+connTeams[conn]);
		squadSpawns = GameObject.FindGameObjectsWithTag("Spawn Squad "+connTeams[conn]);

		GameObject obj = GameObject.Instantiate(unitPrefab, spawnPositions[0].transform.position, spawnPositions[0].transform.rotation);
		FPControl newFp = obj.GetComponent<FPControl>();
		newFp.Init(connTeams[conn]/*connNum*/);

		NetworkServer.AddPlayerForConnection(conn, obj, playerControllerId);
		//newFp.GetComponent<NetworkIdentity>().AssignClientAuthority(conn);
	}

	public override void OnClientConnect(NetworkConnection conn)
	{
		ClientScene.Ready(conn);

		/*if(spawnPositions != null)
		{
			spawnPositions = GameObject.FindGameObjectsWithTag("Spawn Unit "+connNum);
			squadSpawns = GameObject.FindGameObjectsWithTag("Spawn Squad "+connNum);
		}*/

		ClientScene.AddPlayer(0/*controlInd*/);
		//controlInd++;

		Squad newSq = GameObject.Instantiate(squadPrefab/*, squadSpawns[0].transform.position, squadPrefab.transform.rotation*/)
			.GetComponent<Squad>();
		/*foreach(FPControl fp in GameObject.FindObjectsOfType<FPControl>())   MOVED TO Squad.Start()
		{
			if(fp.isLocalPlayer)
			{
				fp.Assign(newSq);
				newSq.transform.position = new Vector3(fp.transform.position.x, 0, fp.transform.position.z);
			}
		}*/
	}
}
