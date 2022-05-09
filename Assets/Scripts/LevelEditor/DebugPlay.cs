using CommandTerminal;
using SimpleMan.Extensions;
using Sperlich.PrefabManager;
using UnityEngine;

public class DebugPlay : MonoBehaviour {

	public CampaignV1.Difficulty difficulty;
	public GridSizes gridSize;
	public LayerMask mask;
	bool ContainsBoss => FindObjectOfType<BossAI>() != null;

	async void Start() {
		Game.IsGameRunningDebug = true;
		await Game.Initialize();
		PrefabManager.Initialize("Debug");
		GameManager.CurrentLevel = new LevelData() { gridSize = gridSize };
		Game.IsGameRunning = true;
		Game.IsGamePlaying = true;
		FindObjectOfType<PlayerTank>().ApplyCustomizations(GameSaver.SaveInstance.tankPreset);
		Terminal.InitializeCommandConsole();

		GameManager.HideCursor();
		PlayerInputManager.ShowControls();
		FindObjectOfType<GameCamera>().ChangeState(GameCamera.GameCamState.Overview, gridSize);
		AIManager.CreateAIGrid(gridSize, mask);
		AIManager.Initialize();

		foreach(var t in FindObjectsOfType<TankBase>()) {
			if(t is TankAI) {
				var ai = t as TankAI;
				ai.EnableAI();
			}
			t.InitializeTank();
			t.SetDifficulty(difficulty);
		}

		if(ContainsBoss) {
			BossUI.ResetBossBar();
			foreach(BossAI boss in FindObjectsOfType<BossAI>()) {
				BossUI.RegisterBoss(boss);
			}
			BossUI.InitAnimateBossBar();
		}
	}
}
