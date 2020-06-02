using UnityEngine;
using System.Collections;

public class DisableInApplicationType : MonoBehaviour {

	public RuntimePlatform[] EnableOnPlatforms;

	// Use this for initialization
	void Start () {
		foreach(RuntimePlatform rp in EnableOnPlatforms){
			if(Application.platform == rp){
				return;
			}
		}
		gameObject.SetActive(false);
	}
}
