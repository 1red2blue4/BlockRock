using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class IsoSnapHelper : MonoBehaviour {
	public float XSnap = 1;
	public float YSnap = 1;
	
	public Vector2 localPos;
	
	void Start () {
		localPos = transform.localPosition;
	}
	
	void Update () {
		if(Application.isEditor){
			if((Vector2)transform.localPosition != localPos){
				transform.localPosition = (Vector3)localPos + Vector3.forward * transform.localPosition.z;
			}
		}
	}
}
