using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Sperlich.Types;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;
using ToyTanks.LevelEditor;
// HDRP Related: using UnityEngine.Rendering.HighDefinition;
using CommandTerminal;
using Sperlich.PrefabManager;
using SimpleMan.Extensions;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelManager : MonoBehaviour {

	[SerializeField] private float gridDensity = 2;
	[SerializeField] private float gridPointDistance = 3f;
	[SerializeField] private float gridPointOverlapRadius = 1f;
	[SerializeField] private CanvasGroup optionsMenu;
	[SerializeField] private Transform themePresets;
	[SerializeField] private LayerMask baseLayer;
	[SerializeField] public List<LevelPreset> presets;
	[SerializeField] private Camera mainCamera;
	[SerializeField] private Camera pauseBlurCamera;
	[SerializeField] private Camera pausePanelBlurCamera;
	[SerializeField] private Camera bannerBlurCamera;
	private int scoreOnLevelEnter;
	private float elapsedTime;
	private float scoreEarned;
	private int playerDeaths;
	private List<TankAsset> tankPrefabs;
	private GameCamera gameCamera;
	bool levelManagerInitializedCampaignLevelOnly;
	bool ContainsBoss => FindObjectOfType<BossAI>() != null;

	private GameManager.GameMode Mode {
		get => GameManager.CurrentMode;
		set => GameManager.CurrentMode = value;
	}
	private LevelData CurrentLevel => GameManager.CurrentLevel;
	private GameManager GameManager => FindObjectOfType<GameManager>();
	public LevelUI UI => FindObjectOfType<LevelUI>();
	private bool IsEditor => Mode == GameManager.GameMode.Editor;
	public Int3 GridBoundary => GetGridBoundary(CurrentLevel.gridSize);
	private Transform _trackContainer;
	private Transform TrackContainer {
		get {
			if(_trackContainer == null) {
				if(GameObject.Find("TrackContainer") == null) {
					_trackContainer = new GameObject("TrackContainer").transform;
				} else {
					_trackContainer = GameObject.Find("TrackContainer").transform;
				}
			}
			return _trackContainer;
		}
	}
	public static LevelEditor Editor => FindObjectOfType<LevelEditor>();
	public static Transform BlocksContainer => GameObject.FindGameObjectWithTag("LevelBlocks").transform;
	public static Transform TanksContainer => GameObject.FindGameObjectWithTag("LevelTanks").transform;
	public static SaveGame.Campaign.Difficulty Difficulty => SaveGame.GetCampaign(SaveGame.SaveInstance.currentSaveSlot).difficulty;
	private static LevelManager _instance;
	public static LevelManager Instance {
		get {
			if(_instance == null) {
				_instance = FindObjectOfType<LevelManager>();
			}
			return _instance;
		}
	}
	public static GridSizes GridSize => GameManager.CurrentLevel.gridSize;

	[HideInInspector] public static PlayerInput player;
	[HideInInspector] public static TankAI[] tankAIs;
	public Scene Scene => gameObject.scene;

	// Initialization
	void Awake() {
		gameCamera = FindObjectOfType<GameCamera>();
		optionsMenu.alpha = 0;
		optionsMenu.gameObject.SetActive(false);
		tankPrefabs = Resources.LoadAll<TankAsset>("Tanks").ToList();
		Terminal.InitializeCommandConsole();
		
		// Start Editor if no GameManager is present
		if(GameManager == false) {
			Mode = GameManager.GameMode.Editor;
			Game.Initialize();
			Editor.ClearLevel();
			Editor.StartLevelEditor();
		}
	}

	public void Initialize() {
		if(levelManagerInitializedCampaignLevelOnly == false) {
			// Must be called before TankBase Script
			if(GameManager.CurrentMode == GameManager.GameMode.Campaign || GameManager.CurrentMode == GameManager.GameMode.LevelOnly) {
				levelManagerInitializedCampaignLevelOnly = true;
				Destroy(Editor.gameObject);
			}
		}
	}

	void InitializeTanks() {
		player = FindObjectOfType<PlayerInput>();
		tankAIs = FindObjectsOfType<TankAI>();
		player.DisableControls();
		DisableAllAIs();
	}

	void Update() {
		// Update and Add to time
		if(Game.IsGamePlaying && Game.GamePaused == false && Game.IsGameRunningDebug == false) {
			GameManager.PlayTime += Time.deltaTime;
			elapsedTime += Time.deltaTime;
			UI.playTime.SetText((Mathf.Round(GameManager.PlayTime * 100f) / 100f + "s").Replace(",", "."));
		}
		if(Input.GetKeyDown(KeyCode.Escape) && Game.IsGamePlaying && (Time.timeScale == 1f || Time.timeScale == 0f)) {
			if(Game.GamePaused) {
				ResumeGame();
			} else {
				PauseGame();
			}
		}
	}

	public void PauseGame() {
		Game.GamePaused = true;
		optionsMenu.gameObject.SetActive(true);
		optionsMenu.DOFade(1, 0.15f);
		player.DisableControls();
		player.CrossHair.DisableCrossHair();
		UI.pauseBlur.Show();
		GameManager.ShowCursor();
		Time.timeScale = 1f;
		BossUI.HideUI(0.1f);

		DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 0.1f, 0.1f).SetEase(Ease.Linear).OnComplete(() => {
			Time.timeScale = 0;
		});
	}

	public void ResumeGame() {
		Game.GamePaused = false;
		optionsMenu.DOFade(0, 0.3f);
		player.EnableControls();
		player.CrossHair.EnableCrossHair();
		UI.pauseBlur.Hide();
		GameManager.HideCursor();
		Time.timeScale = 1f;
		BossUI.ShowUI(0.5f);

		DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 1, 0.1f).SetEase(Ease.Linear).OnComplete(() => {
			optionsMenu.gameObject.SetActive(false);
		});
	}

	public void ReturnToMenu() {
		Time.timeScale = 1f;
		Game.GamePaused = false;
		GameManager.ShowCursor();
		player.CrossHair.EnableCrossHair();
		optionsMenu.DOFade(0, 0.5f);
		player.CrossHair.DisableCrossHair();
		GameManager.ReturnToMenu("Quitting");
	}

	public void OpenGraphicsMenu() {
		GraphicSettings.OpenOptionsMenu(0.2f);
    }

	public void ClearMap() {
		// Clear all GameEntities from level
		foreach(GameEntity entity in FindObjectsOfType<GameEntity>().Where(g => g != null && g.CompareTag("LevelPreset") == false)) {
			if(entity is TankBase) {
				(entity as TankBase).CleanUpDestroyedPieces();
			}
			DestroyImmediate(entity.gameObject);
		}
		LevelGround.Instance.Clear();
		presets.ForEach(preset => preset.gameobject.Hide());
	}

	public IEnumerator LoadAndBuildMap(LevelData data, float loadDuration) {
		ClearMap();
		var blockAssets = Resources.LoadAll<ThemeAsset>(GamePaths.ThemesPath).ToList().Find(t => t.theme == data.theme);
		float timePerBlock = loadDuration / data.blocks.Count;
		EnablePreset(data.gridSize, data.theme);

		foreach(var block in data.blocks) {
			ThemeAsset.BlockAsset asset = blockAssets.GetAsset(block.type);
			var b = Instantiate(asset.prefab, block.pos, Quaternion.Euler(block.rotation)).GetComponent<LevelBlock>();
			b.transform.SetParent(BlocksContainer);
			b.MeshRender.sharedMaterial = asset.material;
			if(asset.isDynamic == false) {
				b.gameObject.isStatic = true;
			} else {
				b.gameObject.isStatic = false;
			}
			b.SetPosition(block.pos);
			yield return new WaitForSeconds(timePerBlock);
		}

		foreach(var tank in data.tanks) {
			var asset = GetTankAsset(tank.tankType);
			var t = Instantiate(asset.prefab, tank.pos + asset.tankSpawnOffset, Quaternion.Euler(tank.rotation)).GetComponent<TankBase>();
			t.transform.SetParent(TanksContainer);
		}
		foreach(Transform t in themePresets) {
			t.gameObject.SetActive(false);
		}
		themePresets.Find(data.theme.ToString()).gameObject.SetActive(true);
		LevelGround.Instance.GenerateAndPatch(GridSize, data.groundTiles);
		LevelLightmapper.SwitchLightmaps(CurrentLevel.levelId);
	}

	public static void SetLevelBoundaryWalls(Int3 boundary) {
		GameObject.Find("WallRight").transform.position = new Vector3(boundary.x * 2, 0, 0);
		GameObject.Find("WallLeft").transform.position = new Vector3(boundary.x * -2 - 2, 0, 0);
		GameObject.Find("WallUp").transform.position = new Vector3(0, 0, boundary.z * 2);
		GameObject.Find("WallDown").transform.position = new Vector3(0, 0, boundary.z * -2 - 2);
	}

	// Game Logic
	public void StartGame() => StartCoroutine(nameof(IStartGame));
	IEnumerator IStartGame() {
		// Game Startup
		UI.PlayBannerAnimation();
		GameManager.HideCursor();
		Game.IsGameRunning = true;
		//this.RepeatUntil(() => Game.IsGamePlaying == false, () => bannerBlurCamera.Copy(mainCamera), () => { });

		// Find theme
		ThemeAsset theme = Resources.LoadAll<ThemeAsset>(GamePaths.ThemesPath).ToList().Find(t => t.theme == CurrentLevel.theme);
		
		// Set Level Boundaries and Lights
		SetLevelBoundaryWalls(GridBoundary);

		// Set Camera to Overview
		gameCamera.camSettings.orthograpicSize = GetOrthographicSize(CurrentLevel.gridSize);
		gameCamera.ChangeState(GameCamera.GameCamState.Overview);
		if(Mode == GameManager.GameMode.Campaign) {
			//RuntimeAnalytics.ArcadeLevelStarted(CurrentLevel.levelId);
		}

		// Generate AI Grid
		Game.CreateAIGrid(CurrentLevel.gridSize, baseLayer, GameObject.FindGameObjectWithTag("Ground"));
		InitializeTanks();
		
		if(ContainsBoss) {
			BossUI.ResetBossBar();
			foreach(BossAI boss in FindObjectsOfType<BossAI>()) {
				BossUI.RegisterBoss(boss);
				boss.BossSpawnAnimate();
			}
			BossUI.InitAnimateBossBar();
		}

		// Turn On/Off tank lights
		yield return new WaitForSeconds(1f);
		foreach(var t in FindObjectsOfType<TankBase>()) {
			if(theme.isDark) {
				t.TurnLightsOn();
			} else {
				t.TurnLightsOff();
			}
		}


		// Initialize Gameplay UI
		yield return new WaitForSeconds(1f);
		UI.playerScore.SetText(GameManager.Score.ToString());
		UI.playerLives.SetText(GameManager.PlayerLives.ToString());
		UI.playTime.SetText(GameManager.PlayTime.ToString());
		UI.levelStage.SetText($"Level {CurrentLevel.levelId}");
		UI.ShowGameplayUI(1);

		// Start Game
		yield return new WaitForSeconds(1f);
		StreakBubble.Interrupt();
		if(GridSize == GridSizes.Size_15x12) {
			gameCamera.ChangeState(GameCamera.GameCamState.Focus);
		} else {
			gameCamera.ChangeState(GameCamera.GameCamState.Overview);
		}
		player.EnableControls();
		EnableAllAIs();

		foreach(var t in FindObjectsOfType<TankBase>()) {
			t.InitializeTank();
		}

		Game.IsGamePlaying = true;
	}

	public void TankDestroyedCheck(TankBase destroyedTank) {
		AddScore(destroyedTank.tankAsset.scoreAmount);
		StreakBubble.DisplayBubble(mainCamera.WorldToScreenPoint(destroyedTank.Pos), GameManager.Score);
		if(tankAIs != null) {
			if(destroyedTank is BossAI) {
				PlayerStats.AddBossesKilled();
			}
			PlayerStats.AddKill();
			foreach(TankAI t in tankAIs) {
				if(t.HasBeenDestroyed == false) {
					// All TankAIs must be destroyed or else returns
					return;
				}
			}
		}
		if(Game.IsGameRunningDebug == false) {
			Instance.StartCoroutine(Instance.GameOver());
		}
	}

	public void AddScore(int amount) {
		if(Game.IsGameRunningDebug == false) {
			int prev = GameManager.Score;
			GameManager.Score += amount * StreakBubble.Streak;
			UI.playerScore.SetText(GameManager.Score.ToString());
			UI.playerScore.CountUp(prev, GameManager.Score, 1);
			PlayerStats.AddScore(amount * StreakBubble.Streak);
		} else {
			UI?.playerScore.SetText(Random.Range(0, 9).ToString());
		}
	}

	public void PlayerDead() {
		Game.IsGamePlaying = false;
		player.DisableControls();
		DisableAllAIs();
		PlayerStats.AddDeath();
		playerDeaths++;

		if(Game.IsGameRunningDebug == false) {
			if(Mode != GameManager.GameMode.LevelOnly && Difficulty != SaveGame.Campaign.Difficulty.Easy && GameManager.PlayerLives > 0) {
				GameManager.PlayerLives--;
				UI.playerLives.SetText(GameManager.PlayerLives.ToString());
				GameManager.UpdateCampaign();
			}

			// Respawn and Continue Level if lives are sufficient
			if(GameManager.CurrentMode == GameManager.GameMode.LevelOnly || GameManager.PlayerLives > 0 || GameManager.Difficulty == SaveGame.Campaign.Difficulty.Easy) {
				Respawn();
			} else {
				Instance.StartCoroutine(Instance.GameOver());
			}
		} else if(Mode == GameManager.GameMode.Editor) {
			Editor.StopTestPlay();
		}
	}

	public static void Respawn() => Instance.StartCoroutine(Instance.IRespawnAnimate());
	IEnumerator IRespawnAnimate() {
		Logger.Log(Channel.Gameplay, "Respawning player.");
		yield return new WaitForSeconds(3f);
		ResetLevel();
		yield return new WaitForSeconds(1f);
		StartGame();
	}

	public IEnumerator GameOver() {
		if(Game.IsGameRunningDebug == false) {
			Game.IsGamePlaying = false;
			player.CrossHair.DisableCrossHair();
			player.DisableControls();
			DisableAllAIs();
			StreakBubble.Interrupt();

			// Update PlayerStats
			PlayerStats.AddTotalPlaytime((int)elapsedTime);
			

			// Decide next GameOver step when player lives reach ZERO
			switch(GameManager.CurrentMode) {
				case GameManager.GameMode.Campaign:
					if(GameManager.PlayerLives > 0 || GameManager.Difficulty == SaveGame.Campaign.Difficulty.Easy) {
						// Continue when players live are sufficient OR playing in EASY
						SaveGame.UnlockLevel(SaveGame.SaveInstance.currentSaveSlot, GameManager.LevelId);
						GameManager.LevelId++;

						CheckRewardLive();
						GameManager.UpdateCampaign();
						gameCamera.PlayConfetti();
						RuntimeAnalytics.AracadeLevelEnded(true, CurrentLevel.levelId, elapsedTime, playerDeaths, GameManager.Difficulty);
						PlayerStats.AddLevelsCompleted();
						Logger.Log(Channel.Gameplay, "Continue to next level: " + GameManager.LevelId);
						yield return new WaitForSeconds(3);
						// Reward Extra Lives
						GameManager.LoadLevel("", true);
					} else {
						// Reset Player to CheckPoint
						switch(GameManager.Difficulty) {
							case SaveGame.Campaign.Difficulty.Medium:
								// Medium Mode resets to the currents world most middle Level (Typicially Level 5 of a World)
								GameManager.PlayerLives = 4;
								ulong halfCheckpoint = Game.GetWorld(GameManager.CurrentLevel.levelId).Levels[Game.GetWorld(GameManager.CurrentLevel.levelId).Levels.Length / 2].LevelId;

								// Dont reset to Half-Checkpoint if the players hasn't been there yet
								if(halfCheckpoint > GameManager.LevelId) {
									GameManager.LevelId = Game.GetWorld(GameManager.CurrentLevel.levelId).Levels[0].LevelId;
								} else {
									GameManager.LevelId = halfCheckpoint;
								}
								RuntimeAnalytics.AracadeLevelEnded(false, CurrentLevel.levelId, elapsedTime, playerDeaths, SaveGame.Campaign.Difficulty.Medium);
								Logger.Log(Channel.Gameplay, "Returning to Checkpoint: " + GameManager.LevelId);
								yield return new WaitForSeconds(3);
								GameManager.LoadLevel("", true);
								break;
							case SaveGame.Campaign.Difficulty.Hard:
								// Hard Mode resets to the currents world first level
								GameManager.PlayerLives = 2;
								GameManager.LevelId = Game.GetWorld(GameManager.CurrentLevel.levelId).Levels[0].LevelId;
								Logger.Log(Channel.Graphics, "Returning to Checkpoint: " + GameManager.LevelId);
								RuntimeAnalytics.AracadeLevelEnded(false, CurrentLevel.levelId, elapsedTime, playerDeaths, SaveGame.Campaign.Difficulty.Hard);
								yield return new WaitForSeconds(2);
								GameManager.LoadLevel("Mission Failed");
								break;
							case SaveGame.Campaign.Difficulty.Original:
								// HardCore deletes SaveSlot
								Logger.Log(Channel.SaveGame, "Player failed, wiping SaveSlot: " + SaveGame.SaveInstance.currentSaveSlot);
								SaveGame.SaveInstance.WipeSlot(SaveGame.SaveInstance.currentSaveSlot);
								SaveGame.Save();

								RuntimeAnalytics.AracadeLevelEnded(true, CurrentLevel.levelId, elapsedTime, playerDeaths, SaveGame.Campaign.Difficulty.Original);
								yield return new WaitForSeconds(3);
								GameManager.ReturnToMenu("Mission Failed");
								break;
						}
					}
					break;
				case GameManager.GameMode.LevelOnly:
					if(player.HasBeenDestroyed == false) {
						gameCamera.PlayConfetti();
					}
					yield return new WaitForSeconds(3);
					GameManager.ReturnToMenu("Returning to Menu");
					break;
				case GameManager.GameMode.Editor:
					yield return null;
					Editor.StopTestPlay();
					break;
			}
			playerDeaths = 0;
			elapsedTime = 0;
		}
	}

	static void CheckRewardLive() {
		float addPercentage = 0;
		switch(GameManager.Difficulty) {
			case SaveGame.Campaign.Difficulty.Easy:
				break;
			case SaveGame.Campaign.Difficulty.Medium:
				addPercentage = 0.05f;
				break;
			case SaveGame.Campaign.Difficulty.Hard:
				addPercentage = 0.035f;
				break;
			case SaveGame.Campaign.Difficulty.Original:
				break;
		}
		GameManager.LiveGainChance += addPercentage;
		if(Random.Range(0f, 1f) < GameManager.LiveGainChance) {
			GameManager.PlayerLives++;
			GameManager.LiveGainChance = 0;
			GameManager.rewardLive = true;
		}
		GameManager.UpdateCampaign();
	}

	static void DisableAllAIs() {
		if(tankAIs != null) {
			foreach(TankAI t in tankAIs) {
				t.DisableAI();
				t.makeInvincible = true;
			}
		}
	}

	static void EnableAllAIs() {
		if(tankAIs != null) {
			foreach(TankAI t in tankAIs) {
				t.EnableAI();
				t.makeInvincible = false;
			}
		}
	}

	public static string GetGridBoundaryText(GridSizes size) => size.ToString().Replace("Size_", "");

	public static float GetOrthographicSize(GridSizes size) {
		switch(size) {
			case GridSizes.Size_15x12:
				return 16;
			case GridSizes.Size_12x9:
				return 15;
			case GridSizes.Size_9x6:
				return 12;
			default:
				return 15;
		}
	}

	public static Int3 GetGridBoundary(GridSizes size) {
		switch(size) {
			case GridSizes.Size_15x12:
				return new Int3(15, 4, 12);
			case GridSizes.Size_12x9:
				return new Int3(12, 4, 9);
			case GridSizes.Size_9x6:
				return new Int3(9, 4, 7);
			default:
				return new Int3(9, 4, 11);
		}
	}

	public void ResetLevel() {
		foreach(GameEntity entity in FindObjectsOfType<GameEntity>().Where(g => g.CompareTag("LevelPreset") == false)) {
			if(entity is IResettable) {
				IResettable r = entity as IResettable;
				r.ResetState();
			}
		}
	}

	public TankAsset GetTankAsset(TankTypes type) => tankPrefabs.Find(t => t.tankType == type);

	//public static LevelPreset GetPreset(GridSizes size, LevelEditor.Themes theme) => Instance.presets.Where(p => p.gridSize == size && p.theme == theme).FirstOrDefault();

	public static void EnablePreset(GridSizes size, WorldTheme theme) {
		Instance.presets.ForEach(preset => { preset.gameobject.Hide(); preset.gameobject.transform.parent.Hide(); });
		var preset = Instance.presets.Where(p => p.gridSize == size && p.theme == theme).FirstOrDefault().gameobject;
		preset.transform.parent.Show();
		preset.Show();
	}

	[System.Serializable]
	public class LevelPreset {
		public WorldTheme theme;
		public GridSizes gridSize;
		public GameObject gameobject;
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(LevelManager))]
public class LevelManagerEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		LevelManager builder = (LevelManager)target;
		if(GUILayout.Button("Reset")) {
			LevelManager.Respawn();
		}
	}
}
#endif