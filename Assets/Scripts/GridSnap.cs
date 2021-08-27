using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class GridSnap : MonoBehaviour {

	public float gridSize;
	public float gridSizeY;

#if UNITY_EDITOR
	void Update() {
		if(new List<Transform>(Selection.transforms).Contains(transform)) {
			Snap();
		}
	}

	public void Snap() {
		Vector3 pos = new Vector3(Mathf.Round(transform.position.x / gridSize) * gridSize, Mathf.Round(transform.position.y / gridSizeY) * gridSizeY, Mathf.Round(transform.position.z / gridSize) * gridSize);
		if(pos.x != float.NaN && pos.y != float.NaN && pos.z != float.NaN)
			transform.position = pos;
	}
#endif
}