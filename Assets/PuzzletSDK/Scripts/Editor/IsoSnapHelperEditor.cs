using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(IsoSnapHelper))]
[CanEditMultipleObjects]
public class IsoSnapHelperEditor : Editor {
	
	private Vector3 internalPos;
	private Vector2 dPos;
	private int counter;
	private int numTargets;

	private Tool lastTool;

	void OnEnable () {
		//save the internal position for where to place the handle
		internalPos = ((IsoSnapHelper)target).transform.position;
		counter = 0;
		numTargets = targets.Length;

		lastTool = Tools.current;
		Tools.current = Tool.None;
	}

	void OnDisable () {
		Tools.current = lastTool;
	}

	void OnSceneGUI () {
		Tools.current = Tool.None;
		//update the counter
		counter = (counter + 1) % numTargets;
		IsoSnapHelper ish = (IsoSnapHelper)target;
		
		//if the target is not supposed to be editable, don't allow editing
		if((ish.gameObject.hideFlags & HideFlags.NotEditable) > 0){
			return;
		}
		//if this is the first target to run, calculate the movement, otherwise used the cached movement
		if(counter == 1 || numTargets == 1){
			//get the previous snapped value
			Vector2 prevLocal = internalPos;
			if(ish.transform.parent != null){
				prevLocal = ish.transform.parent.InverseTransformPoint(prevLocal);
			}
			Vector2 prevSnapped = new Vector2(snap (prevLocal.x, ish.XSnap), snap (prevLocal.y, ish.YSnap));
			
			Handles.color = new Color(0,0,0,.7f);

			//get input from the movement handle
			internalPos = Handles.FreeMoveHandle(internalPos, Quaternion.identity, .5f, Vector3.zero, Handles.CubeCap);
			//internalPos = Tools.handlePosition;

			if (GUI.changed){
				//get the new snapped value
				Vector2 localPos = internalPos;
				if(ish.transform.parent != null){
					localPos = ish.transform.parent.InverseTransformPoint(internalPos);
				}
				Vector2 localPosSnapped = new Vector2(snap(localPos.x, ish.XSnap), snap (localPos.y, ish.YSnap));
				
				dPos = localPosSnapped - prevSnapped;
			} else {
				dPos = Vector2.zero;
			}
		}
		//if the two snapped positions are different, move the target
		if(dPos.sqrMagnitude != 0){
			ish.localPos += dPos;
			ish.transform.localPosition += (Vector3)dPos;
			EditorUtility.SetDirty(target);
			EditorUtility.SetDirty(ish.transform);
		}
	}
	
	private float snap (float rawData, float step){
		int numSteps = Mathf.RoundToInt(rawData / step);
		return numSteps * step;
	}
}
