using UnityEngine;
using System.Collections;

public class ReloadScene : MonoBehaviour {

	void OnMouseDown() {
		Application.LoadLevel(Application.loadedLevel);
	}
}
