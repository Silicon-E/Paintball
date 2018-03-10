using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor.MemoryProfiler;

public class ProbeBaker : MonoBehaviour {

	public Camera[] cams;

	[ExecuteInEditMode]
	void OnEnable()
	{
		string name = "A";
		foreach(Camera virtuCamera in cams)
		{
			int sqr = 512;

			virtuCamera.aspect = 1.0f;
			// recall that the height is now the "actual" size from now on

			RenderTexture tempRT = new RenderTexture(sqr,sqr, 24 );
			// the 24 can be 0,16,24, formats like
			// RenderTextureFormat.Default, ARGB32 etc.

			virtuCamera.targetTexture = tempRT;
			virtuCamera.Render();

			RenderTexture.active = tempRT;
			Texture2D virtualPhoto =
				new Texture2D(sqr,sqr, TextureFormat.RGB24, false);
			// false, meaning no need for mipmaps
			virtualPhoto.ReadPixels( new Rect(0, 0, sqr,sqr), 0, 0);

			RenderTexture.active = null; //can help avoid errors 
			virtuCamera.targetTexture = null;
			// consider ... Destroy(tempRT);

			byte[] bytes;
			bytes = virtualPhoto.EncodeToPNG();

			System.IO.File.WriteAllBytes(
				"Assets/MC "+virtuCamera.gameObject+".png", bytes );
			// virtualCam.SetActive(false); ... no great need for this.



			name += "A";
		}
	}
}
#endif