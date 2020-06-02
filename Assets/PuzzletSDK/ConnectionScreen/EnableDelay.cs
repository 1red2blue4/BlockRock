using UnityEngine;
using System.Collections;

public class EnableDelay : MonoBehaviour
{
	public GameObject[] targets;
	public bool[] enables;
	public float delay;

	void Update ()
	{
		delay -= Time.deltaTime;
		if(delay <= 0)
		{
			for(int a = 0; a < targets.Length; ++a)
				targets[a].SetActive(enables[a]);

			Destroy(gameObject);
		}
	}
}