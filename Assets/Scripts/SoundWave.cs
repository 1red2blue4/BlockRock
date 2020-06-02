using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundWave : MonoBehaviour {
	public int resolution=2400;
	public float[] yPos=new float[2400];
	private ParticleSystem.Particle[] points;
	public Color songColor;
	public float size;
	public float height;
	// Use this for initialization
	void Start () {
		points = new ParticleSystem.Particle[resolution];
		float increment = 10f / (resolution - 1);
		for (int i = 0; i < resolution; i++) {
			float x = i * increment;
			points[i].position = new Vector3(x, yPos[i]*height, 0f);
			points[i].color = songColor;
			points[i].size = size;
		}
	}
	
	// Update is called once per frame
	void Update () {
		transform.GetComponent<ParticleSystem>().SetParticles(points, points.Length);
	}
	public void reGraph(){
		points = new ParticleSystem.Particle[resolution];
		float increment = 10f / (resolution - 1);
		for (int i = 0; i < resolution; i++) {
			float x = i * increment;
			points[i].position = new Vector3(x, yPos[i]*height, 0f);
			points[i].color = songColor;
			points[i].size = size;
		}
	}
}
