using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(LightProbeGroup))]
public class LightProbePlacer : MonoBehaviour {

	public float amount;
	public Vector3 size;
	LightProbeGroup lights;

#if UNITY_EDITOR
	private void OnValidate() {
		lights = GetComponent<LightProbeGroup>();
	}

	public void Generate() {
		var poses = new List<Vector3>();
		for(float x = -size.x; x <= size.x; x += 1f / amount) {
			for(float z = -size.z; z <= size.z; z += 1f / amount) {
				Vector3 pos = new Vector3(x, 0, z);
				poses.Add(pos);
				pos = new Vector3(x, size.y, z);
				poses.Add(pos);
			}
		}
		lights.probePositions = poses.ToArray();
	}

	void OnDrawGizmosSelected() {
		Gizmos.color = new Color32(25, 200, 25, 75);
		Gizmos.DrawCube(transform.position, size * 2);
	}
#endif
}
#if UNITY_EDITOR
[CustomEditor(typeof(LightProbePlacer))]
public class LightProbePlacerEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		var builder = (LightProbePlacer)target;
		if(GUILayout.Button("Generate")) {
			builder.Generate();
		}
	}
}
#endif