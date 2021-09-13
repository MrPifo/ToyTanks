using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class MenuWorldUI : MonoBehaviour {

	public Worlds worldType;
	public List<MenuLevelUI> Levels;

	void Start() {
		foreach(Transform t in transform) {
			if(t.TryGetComponent(out MenuLevelUI c)) {
				c.Initialize(null, false);
				Levels.Add(c);
			}
		}
	}

	public void FillConnections(float duration, int fillFrom = 0, int fillTo = 0) {
		if(fillFrom != 0 && fillTo != 0) {
			Assert.IsTrue(fillTo > fillFrom, $"FillTo[{fillTo}] must be greater than FillFrom[{fillFrom}]");
		}
		Assert.IsTrue(fillFrom >= 0 && fillFrom < Levels.Count - 1, $"FillFrom {fillFrom} must not exceed Level Count - 1");
		Assert.IsTrue(fillTo >= 0 && fillTo < Levels.Count, $"FillTo {fillTo} must not exceed Level Count.");

		float timeOffset = 0;
		fillFrom = fillFrom > 1 ? fillFrom - 1 : 0;
		fillTo = fillTo > 1 ? fillTo - 1 : 0;
		float timeEachElement = duration / Levels.Count;
		if(fillFrom > 0) {
			for(int i = 0; i < fillFrom; i++) {
				Levels[i].FillTransition(0, 0);
			}
		}
		for(int i = fillFrom; i < Levels.Count && fillTo == 0 || i < fillTo; i++) {
			float time = timeEachElement * (i - fillFrom);
			Levels[i].FillTransition(time + timeOffset, duration / Levels.Count);
			if(Levels[i].elements > 1) {
				timeOffset += timeEachElement * Levels[i].elements - timeEachElement;
			}
		}
	}

	public void ResetLines() {
		int count = 0;
		foreach(var level in Levels) {
			level.ResetShapes();
			var gameLevel = Game.GetLevelByOrder(worldType, count);
			level.Initialize(gameLevel, SaveGame.IsLevelUnlocked(worldType, gameLevel.LevelId));
			count++;
		}
	}

	public void DisableWorld() {
		foreach(Transform t in transform) {
			t.gameObject.SetActive(false);
		}
	}

	public void EnableWorld() {
		foreach(Transform t in transform) {
			t.gameObject.SetActive(true);
		}
	}

	public void EnableLevel(int levelOrder) {
		Levels[levelOrder].UnlockLevel();
	}

	public void DisableLevel(int levelOrder) {
		Levels[levelOrder].LockLevel();
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(MenuWorldUI))]
public class MenuWorldUIEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		var builder = (MenuWorldUI)target;
		if(GUILayout.Button("Fill")) {
			builder.ResetLines();
			builder.FillConnections(3);
		}
		if(GUILayout.Button("Fill 5 Levels")) {
			builder.ResetLines();
			builder.FillConnections(3, 0, 5);
		}
		if(GUILayout.Button("Fill From Level 5")) {
			builder.ResetLines();
			builder.FillConnections(3, 5);
		}
		if(GUILayout.Button("Fill From Level 5 to 8")) {
			builder.ResetLines();
			builder.FillConnections(3, 5, 8);
		}
	}
}
#endif