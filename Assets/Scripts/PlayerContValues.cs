using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerContValues : MonoBehaviour {

	public Canvas HUDCanvas;
	public Canvas pauseCanvas;

	public Image hitIndicator;
	public DmgIndicator dmgIndicator;
	public Camera minimapCamera;
	public RectTransform minimapMask;
	public RectTransform minimapImage;
	public RectTransform minimapCanvas;
	public GameObject commandStuff;
	public Button newSqButton;

	[HideInInspector] public PlayerControl localPlayerControl;

	public void UnPause()
	{
		localPlayerControl.UnPause();
	}
}
