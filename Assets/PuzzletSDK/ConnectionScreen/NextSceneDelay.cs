using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class NextSceneDelay : MonoBehaviour
{
	public float delay;

	void Update ()
	{
		delay -= Time.unscaledDeltaTime;
		Debug.Log("NextSceneDelay = " + delay);

		if(delay <= 0)
		{
			Debug.Log("NextSceneDelay progressed");
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
		}
	}
}