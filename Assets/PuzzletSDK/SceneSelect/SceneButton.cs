using UnityEngine;
using System.Collections;

public class SceneButton : MonoBehaviour
{
	public void OnClick()
	{
		Application.LoadLevel(gameObject.name);
	}
}