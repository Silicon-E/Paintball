using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplatParticles : MonoBehaviour { // Mostly lifted from tutorial on Unity's site

	public int maxDecals = 100;
	public float decalSizeMin = .5f;
	public float decalSizeMax = 1.5f;

	private ParticleSystem splatSystem;
	private int particleDecalDataIndex;
	private ParticleDecalData[] particleData;
	private ParticleSystem.Particle[] particles;

	public struct ParticleDecalData 
	{
		public Vector3 position;
		public float size;
		public Vector3 rotation;
		public Color color;
	}



	void Start () 
	{
		splatSystem = GetComponent<ParticleSystem> ();
		particles = new ParticleSystem.Particle[maxDecals];
		particleData = new ParticleDecalData[maxDecals];
		for (int i = 0; i < maxDecals; i++) 
		{
			particleData [i] = new ParticleDecalData ();    
		}
	}

	public void CreateParticle(RaycastHit rayHit, Color colIn)
	{
		SetParticleData (rayHit, colIn);
		DisplayParticles ();
	}

	void SetParticleData(RaycastHit rayHit, Color colIn)
	{
		if (particleDecalDataIndex >= maxDecals) 
		{
			particleDecalDataIndex = 0;
		}

		particleData [particleDecalDataIndex].position = rayHit.point + (rayHit.normal * 0.01f);
		Vector3 particleRotationEuler = Quaternion.LookRotation (-rayHit.normal).eulerAngles;
		particleRotationEuler.z = Random.Range (0, 360);
		particleData [particleDecalDataIndex].rotation = particleRotationEuler;
		particleData [particleDecalDataIndex].size = Random.Range (decalSizeMin, decalSizeMax);
		particleData [particleDecalDataIndex].color = colIn;

		particleDecalDataIndex++;
	}

	void DisplayParticles()
	{
		for (int i = 0; i < particleData.Length; i++) 
		{
			particles [i].position = particleData [i].position;
			particles [i].rotation3D = particleData [i].rotation;
			particles [i].startSize = particleData [i].size;
			particles [i].startColor = particleData [i].color;
		}

		splatSystem.SetParticles (particles, particles.Length);
	}
}
