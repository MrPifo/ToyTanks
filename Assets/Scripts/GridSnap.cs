using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class GridSnap : MonoBehaviour {

	bool isSelected;

	void OnEnable() {
		Selection.selectionChanged += CheckSelect;
	}
	public void Update() {
		if(isSelected) {
			Vector3 pos = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), Mathf.Round(transform.position.z));
			transform.position = pos;
		}
	}

	public void CheckSelect() {
		if(Selection.activeGameObject == gameObject || new List<GameObject>(Selection.gameObjects).Contains(gameObject)) {
			isSelected = true;
		} else {
			isSelected = false;
		}
	}

	void OnDestroy() {
		Selection.selectionChanged -= CheckSelect;
	}
}
