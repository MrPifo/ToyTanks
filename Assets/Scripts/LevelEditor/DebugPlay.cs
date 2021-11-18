using CommandTerminal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugPlay : MonoBehaviour {

	public bool showDebug;
	public LayerMask mask;
	public GridSizes gridSize;
	public static bool isDebug;

	private void Start() {
		GraphicSettings.Initialize();
		GameManager.CurrentLevel = new LevelData() { gridSize = GridSizes.Size_17x14, isNight = false };
		Game.showTankDebugs = showDebug;
		Terminal.InitializeCommandConsole();

		isDebug = true;
		GameManager.HideCursor();
		FindObjectOfType<GameCamera>().camSettings.orthograpicSize = 19;
		FindObjectOfType<GameCamera>().ChangeState(GameCamera.GameCamState.Focus);
		Game.CreateAIGrid(gridSize, mask, true);
		LevelManager.SetLevelBoundaryWalls(LevelManager.GetGridBoundary(Game.ActiveGrid.gridSize));

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
		LevelManager.player.SetupCross();
	}
}
