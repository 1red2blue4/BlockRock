using UnityEngine;
using System.Collections;

public class MacRetinaFullscreenFix : MonoBehaviour {

#if UNITY_STANDALONE_OSX
	// Use this for initialization
	IEnumerator Start () {
		if (Screen.fullScreen)
		{
			//MacBook Pro Retina 15: width = 2880 , MacBook Pro Retina 13: width = 2496 ?
			//could check device model name, but not sure now about retina 13 device model name
			//if last resolution is almost retina resolution...
			Resolution[] resolutions = Screen.resolutions;
			if ((resolutions.Length > 0) && resolutions[resolutions.Length - 1].width > 2048)
			{
				Screen.fullScreen = false;
				yield return null;
				Screen.fullScreen = true;
				yield return null;
			}
		}
	}
#endif
}
