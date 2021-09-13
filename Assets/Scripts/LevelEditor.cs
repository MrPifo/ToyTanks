using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[ExecuteInEditMode]
[RequireComponent(typeof(LevelManager))]
public class LevelEditor : MonoBehaviour {
	
	public enum LevelTheme { LightWood, FirWood, Floor }

	public LevelManager level;
	public GameObject prefab;
	public GameObject instance;
	public LevelTheme theme;
	public List<GameObject> themeAssets;
	public int selection;
	LevelTheme lastTheme;
	int lastSelect;
	HashSet<string> exludeList = new HashSet<string>() {"Ground"};

#if UNITY_EDITOR
	[DidReloadScripts]
	public static void Initialize() {
		var editor = FindObjectOfType<LevelEditor>();
		if(editor != null) {
			editor.Awake();
		}
	}

	public void Awake() {
		CustomLevelEditor.spaceDown = new UnityEvent();
		CustomLevelEditor.spaceDown.AddListener(Place);
	}

	void Update() {
		if(theme != lastTheme) {
			FetchAssets();
		}
		if(selection != lastSelect && selection < themeAssets.Count && selection >= 0 || instance == null) {
			if(instance != null) {
				DestroyImmediate(instance);
			}
			prefab = themeAssets[selection];
			instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
			instance.layer = 2;
			lastSelect = selection;
		} else if(selection > themeAssets.Count || selection < 0) {
			selection = themeAssets.Count - 1;
		}
		if(instance != null && level != null && Physics.Raycast(CustomLevelEditor.mouseRay, out RaycastHit hit, Mathf.Infinity, level.generationLayer)) {
			instance.transform.position = hit.point;
			if(instance.TryGetComponent(out GridSnap snap)) {
				snap.Snap();
			}
		}
	}

	void Place() {
		var parent = GameObject.FindGameObjectWithTag("Level");
		if(parent != null) {
			var rend = instance.GetComponent<MeshRenderer>();
			var overlaps = Physics.OverlapBox(instance.transform.position, rend.bounds.extents / 2.2f);
			foreach(var c in overlaps) {
				Debug.Log(c.name);
				if(!exludeList.Contains(c.name) && c.gameObject != instance) {
					return;
				}
			}
			instance.layer = LayerMask.NameToLayer("Level");
			instance.transform.parent = parent.transform;
			instance = null;
		} else {
			Debug.LogError("Couldnt find a parent with the tag [Level]");
		}
	}

	void FetchAssets() {
		var path = $"{Application.dataPath}/Prefabs/LevelAssets/{theme}";
		lastTheme = theme;
		var asssets = LoadAllPrefabsOfType<Transform>(path);
		themeAssets = new List<GameObject>();
		selection = 0;
		lastSelect = -1;
		if(instance != null) {
			DestroyImmediate(instance);
		}
		foreach(var t in asssets) {
			themeAssets.Add(t.gameObject);
		}
	}

	void OnDrawGizmosSelected() {
		if(Selection.activeGameObject == gameObject)
		Update();
	}

	public List<T> LoadAllPrefabsOfType<T>(string path) {
		if(path != "") {
			if(path.EndsWith("/")) {
				path = path.TrimEnd('/');
			}
		}

		DirectoryInfo dirInfo = new DirectoryInfo(path);
		FileInfo[] fileInf = dirInfo.GetFiles("*.prefab");

		//loop through directory loading the game object and checking if it has the component you want
		List<T> prefabComponents = new List<T>();
		foreach(FileInfo fileInfo in fileInf) {
			string fullPath = fileInfo.FullName.Replace(@"\", "/");
			string assetPath = "Assets" + fullPath.Replace(Application.dataPath, "");
			GameObject prefab = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;

			if(prefab != null) {
				T hasT = prefab.GetComponent<T>();
				if(hasT != null) {
					prefabComponents.Add(hasT);
				}
			}
		}
		return prefabComponents;
	}
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(LevelEditor))]
public class CustomLevelEditor : Editor {

	public static Ray mouseRay;
	public static bool spaceKey;
	public static UnityEvent spaceDown;

	void OnSceneGUI() {
		Event guiEvent = Event.current;
		if(guiEvent.type == EventType.MouseDrag && Event.current.button == 0 || guiEvent.type == EventType.MouseDown && Event.current.button == 0) {
			spaceDown.Invoke();
		}
		mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
		if(guiEvent.type == EventType.Layout) {
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
		}
	}
}
#endif