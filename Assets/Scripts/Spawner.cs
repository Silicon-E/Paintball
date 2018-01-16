using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Spawner : NetworkBehaviour {








	//CONDEMNED SCRIPT; DO NOT USE









	public GameObject unitPrefab;
	public GameObject squadPrefab;

	private GameObject[] spawnPositions;
	private GameObject[] squadSpawns;
	private int team;
	private int sqSpawnInd = 0;

	// Use this for initialization
	void OnPlayerConnected(NetworkPlayer player) { //Start () {
		if(!isServer/*LocalPlayer*/)
			return;

		if(isServer)
			team = 0;
		else
			team = 1;
		GameObject.FindObjectOfType<PlayerControl>().team = team;

		spawnPositions = GameObject.FindGameObjectsWithTag("Spawn Unit "+team);
		squadSpawns = GameObject.FindGameObjectsWithTag("Spawn Squad "+team);

		Spawn(1, player);//TODO:remove
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void Spawn(int count, NetworkPlayer player)
	{
		Squad newSq = GameObject.Instantiate(squadPrefab, squadSpawns[sqSpawnInd].transform.position, squadPrefab.transform.rotation)
			.GetComponent<Squad>();
		sqSpawnInd++;
		if(sqSpawnInd == squadSpawns.Length)
			sqSpawnInd = 0;

		for(int i=0; i<count; i++)
		{
			int ind;

			if(spawnPositions.Length == 1)
				ind = 0;
			else
				ind = (int)Mathf.Repeat(i, spawnPositions.Length-1);
			
			GameObject obj = GameObject.Instantiate(unitPrefab, spawnPositions[ind].transform.position, spawnPositions[ind].transform.rotation);

			FPControl newFp = obj.GetComponent<FPControl>();
			newFp.Init(team);
			newFp.Assign(newSq);

			//NetworkServer.SpawnWithClientAuthority(obj,  player.ipAddress/*connectionToClient*/);


		}
	}
}
