using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerContValues : MonoBehaviour {

	public GameManager gameManager;

	public Canvas HUDCanvas;
	public Canvas pauseCanvas;

	public Image hitIndicator;
	public DmgIndicator dmgIndicator;
	public Slider healthSlider;
	public Image healthSliderBG;
	public Camera minimapCamera;
	public RectTransform minimapMask;
	public RectTransform minimapImage;
	public RectTransform minimapCanvas;
	public GameObject commandStuff;
	public Button newSqButton;
	public MeshRenderer viewModel;

	public RectTransform waitingPane;
	[HideInInspector] public JoinGameHUD joinHud = null;

	[HideInInspector] public PlayerControl localPlayerControl;

	public void UnPause()
	{
		localPlayerControl.UnPause();
	}

	public void BackWhileWaiting()
	{
		if(joinHud != null)
			joinHud.BackWhileWaiting();
	}
}
