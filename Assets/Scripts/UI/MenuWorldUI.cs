using System.Collections;
using System.Collections.Generic;
using ToyTanks.UI;
using UnityEngine;

public class MenuWorldUI : MonoBehaviour {

	public WorldTheme world;
	public MenuCameraSettings cameraSettings = new MenuCameraSettings();
	public List<(ulong id, MenuLevelUI)> levels = new List<(ulong id, MenuLevelUI)>();

	private void Awake() {
		ulong count = (ulong)world * 10 + 1;
		foreach(Transform t in transform) {
			if(t.TryGetComponent(out MenuLevelUI ui) && Game.LevelExists(count)) {
				levels.Add((count, ui));
			}
			if(ui != null) {
				count++;
			}
		}
	}

	public void RenderWorld() {
		foreach(var level in levels) {
			if(GameSaver.HasLevelBeenPlayed(level.id) && GameSaver.HasLevelBeenUnlocked(level.id)) {
				level.Item2.UnlockLevel();
			} else {
				level.Item2.LockLevel();
			}
			if(Game.LevelExists(level.id)) {
				level.Item2.Initialize(Game.GetLevelById(level.id));
			}
		}
	}

}