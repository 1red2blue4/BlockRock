using UnityEngine;
using System.Collections;

public class WebLink : MonoBehaviour {

	public void OpenWebLink(string link){
		Application.OpenURL(link);
		Application.Quit();
	}
}
