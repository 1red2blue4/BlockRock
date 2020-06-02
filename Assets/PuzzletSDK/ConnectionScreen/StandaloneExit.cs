using UnityEngine;
using System.Collections;

public class StandaloneExit : MonoBehaviour {

	#if UNITY_STANDALONE
	// Use this for initialization
	void Start () {
		DontDestroyOnLoad(gameObject);
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetKeyDown(KeyCode.Escape)){
			Application.Quit();
		}
	}
	#endif
}
