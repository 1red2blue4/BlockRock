using UnityEngine;
using System.Collections;
using Puzzlets;

public class ConectionNotification : MonoBehaviour {

	void Start () {
		StartCoroutine(CheckStatusLoop());
	}

	IEnumerator CheckStatusLoop() {
		while(true){
			if(PuzzletConnection.Connected){
				GetComponent<GUIText>().enabled = false;
			} else {
				GetComponent<GUIText>().enabled = true;
			}
			yield return new WaitForSeconds(.05f);
		}
	}
}
