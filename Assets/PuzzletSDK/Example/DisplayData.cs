using UnityEngine;
using System.Collections;
using Puzzlets;

[RequireComponent(typeof(GUIText))]
public class DisplayData : MonoBehaviour, IPuzzletReceiver {

	public int X,Y;

	public ParticleSystem AddedEffect, RemovedEffect;

	void Start () {
		PuzzletManager.RegisterReceiver(this);
	}

	// Update is called once per frame
	void Update () {
		GetComponent<GUIText>().text = PuzzletConnection.RawPuzzletData[X + PuzzletManager.PuzzletGridWidth * Y].ToString();
	}

	public void PuzzletChanged(PuzzletData[] removedTiles, PuzzletData[] addedTiles){
		if(!this)
			return;
		
		foreach(PuzzletData data in removedTiles){
			if(data.X == X && data.Y == Y){
				RemovedEffect.Play();
			}
		}
		foreach(PuzzletData data in addedTiles){
			if(data.X == X && data.Y == Y){
				AddedEffect.Play();
			}
		}
	}

}
