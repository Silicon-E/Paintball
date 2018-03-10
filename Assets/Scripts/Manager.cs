using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Manager
{
	public struct HitInfo
	{
		public int unitId;
		public int amount;
		public Vector3 dir;
		public Vector3 point;
		public int newHealth;
		//public int isFromServer;
		public HitInfo(int u, int a, Vector3 d, Vector3 p, int n/*, int i*/)
		{
			unitId = u;
			amount = a;
			dir = d;
			point = p;
			newHealth = n;
			//isFromServer = i;
		}
	}
	static public List<HitInfo> bulletHits = new List<HitInfo>();//This necessary to get around Unity's built-in refusal to let Bullet send Cmds

	static public Material[] teamMaterials = {
		Resources.Load("Robot Red") as Material, //AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Team 0.mat"),
		Resources.Load("Robot Blue") as Material, //AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Team 1.mat")
	};

	static public Color[] teamColors = {
		new Color(1f, 0f, 0.5f),
		new Color(0f, 0.5f, 1f)
	};
	static public Color[] pointColors = {
		new Color(0.75f, 0.25f, 0.5f),
		new Color(0.25f, 0.5f, 0.75f)
	};
	static public Color[] squadColors = {
		new Color(1f, 0.5f, 0.5f),
		new Color(0f, 0.75f, 1f)
	};

	static public Color miniEmphasis = new Color(0.5f, 1f, 0.75f);

	public static String[] teamLayers = {"Team 0", "Team 1"};

	public static LayerMask[] losMasks = {LayerMask.GetMask(new string[]{"Terrain","Team 1"}), LayerMask.GetMask(new string[]{"Terrain","Team 0"})};

	public static LayerMask[] enemyMasks = {LayerMask.NameToLayer("Team 1"), LayerMask.NameToLayer("Team 0")};

	public static List<Squad> needMembers = new List<Squad>();

	public static float ragdollImpulse = 5f;
}
