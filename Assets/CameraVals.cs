using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraVals : MonoBehaviour {

	public Transform vmMuzzle;
	public GameObject viewModel;

	void Start()
	{
		/*CommandBuffer buf = new CommandBuffer();
		buf.DrawRenderer(viewModel.GetComponent<MeshRenderer>(), viewModel.GetComponent<MeshRenderer>().material);
		Camera.main.AddCommandBuffer(CameraEvent.AfterEverything, buf);*/
	}
}
