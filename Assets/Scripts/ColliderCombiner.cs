using System.Collections;
using System.Collections.Generic;
using UnityEditor;
#if UNITY_EDITOR
using UnityEngine;
#endif

[ExecuteAlways]
public class ColliderCombiner : MonoBehaviour {

	public Collider c1;
	public MeshCollider c2;
	public MeshCollider target;
	public MeshFilter filter;
	public MeshRenderer rend;

    public void Combine() {
		MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
		CombineInstance[] combine = new CombineInstance[meshFilters.Length];

		int i = 0;
		while(i < meshFilters.Length) {
			combine[i].mesh = meshFilters[i].sharedMesh;
			combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
			meshFilters[i].gameObject.SetActive(false);

			i++;
		}
		transform.GetComponent<MeshFilter>().mesh = new Mesh();
		transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
		transform.gameObject.SetActive(true);
	}


}

#if UNITY_EDITOR
[CustomEditor(typeof(ColliderCombiner))]
public class NodeGridEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		var builder = (ColliderCombiner)target;
		if(GUILayout.Button("Combine")) {
			builder.Combine();
		}
	}
}
#endif