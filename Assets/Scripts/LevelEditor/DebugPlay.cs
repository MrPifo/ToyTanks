using CommandTerminal;
using Sperlich.PrefabManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugPlay : MonoBehaviour {

	public bool showDebug;
	public LayerMask mask;
	public GridSizes gridSize;
	public static bool isDebug;

	private void Start() {
		Game.Initialize();
		PrefabManager.Initialize("Debug");
		GameManager.CurrentLevel = new LevelData() { gridSize = GridSizes.Size_15x12 };
		Game.showTankDebugs = showDebug;
		Game.IsGameRunning = true;
		Game.IsGameRunningDebug = true;
		Game.IsGamePlaying = true;
		Terminal.InitializeCommandConsole();

		isDebug = true;
		GameManager.HideCursor();
		PlayerInputManager.ShowControls();
		FindObjectOfType<GameCamera>().camSettings.orthograpicSize = 19;
		FindObjectOfType<GameCamera>().ChangeState(GameCamera.GameCamState.Focus);
		Game.CreateAIGrid(gridSize, mask, true);
		//LevelManager.SetLevelBoundaryWalls(LevelManager.GetGridBoundary(Game.ActiveGrid.gridSize));

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
	}
}
