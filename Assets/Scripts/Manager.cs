using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Manager
{
	static public Color[] teamColors = {
		new Color(1f, 0f, 0.5f),
		new Color(0f, 0.5f, 1f)
	};
	static public Color[] squadColors = {
		new Color(1f, 0.5f, 0.5f),
		new Color(0f, 0.75f, 1f)
	};

	public static String[] teamLayers = {"Team0", "Team1"};

	public static LayerMask[] losMasks = {LayerMask.GetMask(new string[]{"Terrain","Team 1"}), LayerMask.GetMask(new string[]{"Terrain","Team 0"})};
}
