using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class ChangeSpriteLayer {

	[MenuItem("Edit/MoveSpriteForward %]")]
	public static void MoveSpriteForward(){
		foreach(GameObject go in Selection.gameObjects){
			SpriteRenderer spRend = go.GetComponent<SpriteRenderer>();
			if(spRend != null){
				spRend.sortingOrder++;
				EditorUtility.SetDirty(spRend);
			}
		}
	}

	[MenuItem("Edit/MoveSpriteBack %[")]
	public static void MoveSpriteBack(){
		int minLayer = 0;
		foreach(GameObject go in Selection.gameObjects){
			SpriteRenderer spRend = go.GetComponent<SpriteRenderer>();
			if(spRend != null){
				spRend.sortingOrder--;
				EditorUtility.SetDirty(spRend);
				if(spRend.sortingOrder < minLayer){
					minLayer = spRend.sortingOrder;
				}
			}
		}
		if(minLayer < 0){
			MoveAllSpritesForward(-minLayer);
		}
	}

	[MenuItem("Edit/MoveSpriteAndChildrenForward %#]")]
	public static void MoveSpritesForward(){
		//compile a non-duplicative list of sprite renderers selected or in children of selection objects
		List<SpriteRenderer> spList = new List<SpriteRenderer>();
		foreach(GameObject go in Selection.gameObjects){
			foreach(SpriteRenderer spRend in go.GetComponentsInChildren<SpriteRenderer>()){
				if(!spList.Contains(spRend)){
					spList.Add(spRend);
				}
			}
		}
		foreach(SpriteRenderer spRend in spList){
			spRend.sortingOrder++;
			EditorUtility.SetDirty(spRend);
		}
	}

	[MenuItem("Edit/MoveSpriteAndChildrenBack %#[")]
	public static void MoveSpritesBack(){
		//compile a non-duplicative list of sprite renderers selected or in children of selection objects
		List<SpriteRenderer> spList = new List<SpriteRenderer>();
		foreach(GameObject go in Selection.gameObjects){
			foreach(SpriteRenderer spRend in go.GetComponentsInChildren<SpriteRenderer>()){
				if(!spList.Contains(spRend)){
					spList.Add(spRend);
				}
			}
		}
		int minLayer = 0;
		foreach(SpriteRenderer spRend in spList){
			spRend.sortingOrder--;
			EditorUtility.SetDirty(spRend);
			if(spRend.sortingOrder < minLayer){
				minLayer = spRend.sortingOrder;
			}
		}
		if(minLayer < 0){
			MoveAllSpritesForward(-minLayer);
		}
	}

	//used to keep all sorting layers non-negative, since negative sorting layers don't seem to save.
	public static void MoveAllSpritesForward(int amount){
		foreach(SpriteRenderer spRend in GameObject.FindObjectsOfType<SpriteRenderer>()){
			spRend.sortingOrder += amount;
			EditorUtility.SetDirty(spRend);
		}
	}
}
