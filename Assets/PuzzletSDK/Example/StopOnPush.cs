using UnityEngine;
using System.Collections;
using Puzzlets;

public class StopOnPush : MonoBehaviour {
	void OnMouseDown(){
		PuzzletConnection.StopScanning();
	}
}
