using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Manager
{
	static public Material[] teamMaterials = {
		Resources.Load("Team 0") as Material, //AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Team 0.mat"),
		Resources.Load("Team 1") as Material, //AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Team 1.mat")
	};

	static public Color[] teamColors = {
		new Color(1f, 0f, 0.5f),
		new Color(0f, 0.5f, 1f)
	};
	static public Color[] squadColors = {
		new Color(1f, 0.5f, 0.5f),
		new Color(0f, 0.75f, 1f)
	};

	static public Color miniEmphasis = new Color(0.5f, 1f, 0.75f);

	public static String[] teamLayers = {"Team 0", "Team 1"};

	public static LayerMask[] losMasks = {LayerMask.GetMask(new string[]{"Terrain","Team 1"}), LayerMask.GetMask(new string[]{"Terrain","Team 0"})};

	public static List<Squad> needMembers = new List<Squad>();
}
