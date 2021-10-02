using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugPlay : MonoBehaviour {

	public static bool isDebug;

	private void Start() {
		GraphicSettings.Initialize();
		LevelManager.IsDebug = true;

		foreach(var t in FindObjectsOfType<TankBase>()) {

			if(t is TankAI) {
				var ai = t as TankAI;
				ai.EnableAI();
			}
			if(t is PlayerInput) {
				LevelManager.player = t as PlayerInput;
			}
			t.InitializeTank();
		}

		isDebug = true;
	}

}
