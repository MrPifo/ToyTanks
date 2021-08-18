#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[RequireComponent(typeof(LightProbeGroup))]
public class LightProbePlacer : MonoBehaviour {

	public float amount;
	public Vector3 size;
	LightProbeGroup lights;

	private void OnValidate() {
		lights = GetComponent<LightProbeGroup>();
	}

	public void Generate() {
		var poses = new List<Vector3>();
		for(float x = -size.x; x <= size.x; x += 1f / amount) {
			for(float z = -size.z; z <= size.z; z += 1f / amount) {
				Vector3 pos = new Vector3(x, 0, z);
				poses.Add(pos);
			}
		}
		lights.probePositions = poses.ToArray();
	}

	void OnDrawGizmosSelected() {
		Gizmos.color = new Color32(25, 200, 25, 75);
		Gizmos.DrawCube(transform.position, size * 2);
	}
}

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
