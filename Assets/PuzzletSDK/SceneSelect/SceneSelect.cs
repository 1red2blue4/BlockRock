using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneSelect : MonoBehaviour
{
	public RectTransform buttonProto;

	public void DestroySceneButtons()
	{
		RectTransform parent = GetComponent<RectTransform>();

		while(parent.childCount > 0)
			DestroyImmediate(parent.GetChild(0).gameObject);
	}

	public void AddSceneButton(string sceneName)
	{
		RectTransform parent = GetComponent<RectTransform>();

		RectTransform button = Instantiate(buttonProto) as RectTransform;
		button.SetParent(parent);
		button.localScale = Vector3.one;

		button.gameObject.name = sceneName;
		button.GetChild(0).GetComponent<Text>().text = sceneName;
	}
}