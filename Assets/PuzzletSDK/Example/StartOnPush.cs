using UnityEngine;
using System.Collections;
using Puzzlets;

public class StartOnPush : MonoBehaviour {

	void OnMouseDown(){
		PuzzletConnection.StartScanning();
	}
}
