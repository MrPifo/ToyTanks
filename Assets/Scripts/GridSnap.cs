#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class GridSnap : MonoBehaviour {

	public float gridSize;

	void Update() {
		if(Selection.activeGameObject == gameObject) {
			Vector3 pos = new Vector3(Mathf.Round(transform.position.x / gridSize) * gridSize, Mathf.Round(transform.position.y / gridSize) * gridSize, Mathf.Round(transform.position.z / gridSize) * gridSize);
			if(pos.x != float.NaN && pos.y != float.NaN && pos.z != float.NaN)
				transform.position = pos;
		}
	}
}
#endif