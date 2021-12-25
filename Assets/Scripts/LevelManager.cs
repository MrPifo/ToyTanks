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
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelManager : MonoBehaviour {

	[SerializeField] private float gridDensity = 2;
	[SerializeField] private float gridPointDistance = 3f;
	[SerializeField] private float gridPointOverlapRadius = 1f;
	[SerializeField] private bool showGrid;
	[SerializeField] private CanvasGroup optionsMenu;
	[SerializeField] private Transform themePresets;
	[SerializeField] private LayerMask baseLayer;
	private int scoreOnLevelEnter;
	private List<TankAsset> tankPrefabs;
	private GameCamera gameCamera;
	bool isBossLevel;
	bool levelManagerInitializedCampaignLevelOnly;

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
			Editor.ClearLevel();
			Editor.StartLevelEditor();
			GraphicSettings.Initialize();
			PrefabManager.Initialize();
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
		player.SetupCross();
		player.DisableControls();
		DisableAllAIs();
	}

	void Update() {
		// Update and Add to time
		if(Game.IsGamePlaying && Game.GamePaused == false && Game.IsGameRunningDebug == false) {
			GameManager.PlayTime += Time.deltaTime;
			UI.playTime.SetText(Mathf.Round(GameManager.PlayTime * 100f) / 100f + "s");
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
		GameManager.ShowCursor();
		Time.timeScale = 1f;
		DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 0.1f, 0.1f).SetEase(Ease.Linear).OnComplete(() => {
			Time.timeScale = 0;
		});
	}

	public void ResumeGame() {
		Game.GamePaused = false;
		optionsMenu.DOFade(0, 0.3f);
		player.EnableControls();
		GameManager.HideCursor();
		Time.timeScale = 0.2f;
		DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 1, 0.1f).SetEase(Ease.Linear).OnComplete(() => {
			optionsMenu.gameObject.SetActive(false);
		});
	}

	public void ReturnToMenu() {
		Time.timeScale = 1f;
		Game.GamePaused = false;
		GameManager.ShowCursor();
		optionsMenu.DOFade(0, 0.5f);
		player.DisableCrossHair();
		GameManager.ReturnToMenu("Quitting");
	}

	public void OpenGraphicsMenu() {
		GraphicSettings.OpenOptionsMenu(0.2f);
    }

	public void ClearMap() {
		// Clear all GameEntities from level
		foreach(GameEntity entity in FindObjectsOfType<GameEntity>()) {
			Destroy(entity.gameObject);
		}
		var ground = GameObject.FindGameObjectWithTag("Ground").GetComponent<MeshRenderer>();
		ground.lightmapScaleOffset = new Vector4(0, 0, 0, 0);
	}

	public IEnumerator LoadAndBuildMap(LevelData data, float loadDuration) {
		ClearMap();
		var blockAssets = Resources.LoadAll<ThemeAsset>(GamePaths.ThemesPath).ToList().Find(t => t.theme == data.theme);
		float timePerBlock = loadDuration / data.blocks.Count;

		foreach(var block in data.blocks) {
			ThemeAsset.BlockAsset asset = blockAssets.GetAsset(block.type);
			var b = Instantiate(asset.prefab, block.pos, Quaternion.Euler(block.rotation)).GetComponent<LevelBlock>();
			b.transform.SetParent(BlocksContainer);
			b.Index = block.index;
			b.meshRender.sharedMaterial = asset.material;
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
		var floor = GameObject.FindGameObjectWithTag("Ground").GetComponent<MeshRenderer>();
		floor.sharedMaterial = blockAssets.floorMaterial;
		LevelLightmapper.SwitchLightmaps(CurrentLevel.levelId);
		// HDRP Relate: sunLight.SetShadowResolutionOverride(true);
		// HDRP Relate: sunLight.SetShadowResolution(GraphicSettings.GetShadowResolution());
	}

	public static void SetLevelBoundaryWalls(Int3 boundary) {
		GameObject.Find("WallRight").transform.position = new Vector3(boundary.x * 2, 0, 0);
		GameObject.Find("WallLeft").transform.position = new Vector3(boundary.x * -2 - 2, 0, 0);
		GameObject.Find("WallUp").transform.position = new Vector3(0, 0, boundary.z * 2);
		GameObject.Find("WallDown").transform.position = new Vector3(0, 0, boundary.z * -2 - 2);
	}

	/* HDRP Related: 
	public void ApplyLightData(LevelData.LightData lightData, HDAdditionalLightData light) {
		if(lightData != null && light != null) {
			light.transform.position = lightData.pos;
			light.transform.eulerAngles = lightData.rotation;
			light.SetIntensity(lightData.intensity);
			light.RequestShadowMapRendering();
			light.UpdateAllLightValues();
		}
	}*/

	// Game Logic
	public void StartGame() => StartCoroutine(nameof(IStartGame));
	IEnumerator IStartGame() {
		// Game Startup
		UI.counterBanner.SetActive(true);
		GameManager.HideCursor();
		Game.IsGameRunning = true;

		// Find theme
		ThemeAsset theme = Resources.LoadAll<ThemeAsset>(GamePaths.ThemesPath).ToList().Find(t => t.theme == CurrentLevel.theme);
		
		// Set Level Boundaries and Lights
		SetLevelBoundaryWalls(GridBoundary);
		// HDRP Relate: ApplyLightData(CurrentLevel.sunLight, sunLight);
		// HDRP Relate: ApplyLightData(CurrentLevel.spotLight, spotLight);

		// Set Camera to Overview
		gameCamera.camSettings.orthograpicSize = GetOrthographicSize(CurrentLevel.gridSize);
		gameCamera.ChangeState(GameCamera.GameCamState.Overview);

		// Generate AI Grid
		Game.CreateAIGrid(CurrentLevel.gridSize, baseLayer, GameObject.FindGameObjectWithTag("Ground"));
		InitializeTanks();

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
		yield return new WaitForSeconds(1.5f);
		// HDRP Relate: DOTween.To(x => UI.OutlinePass.threshold = x, UI.OutlinePass.threshold, 100, 2).SetEase(Ease.InCubic);
		foreach(BossAI boss in FindObjectsOfType<BossAI>()) {
			BossUI.RegisterBoss(boss);
		}
		BossUI.InitAnimateBossBar();
		UI.playerScore.SetText("0");
		UI.playerLives.SetText("0");
		UI.playTime.SetText("0");
		UI.levelStage.SetText($"Level {CurrentLevel.levelId}");

		// Start Game
		yield return new WaitForSeconds(1);
		if(GridSize == GridSizes.Size_17x14) {
			gameCamera.ChangeState(GameCamera.GameCamState.Focus);
		} else {
			gameCamera.ChangeState(GameCamera.GameCamState.Overview);
		}
		gameCamera.focusOnPlayerStrength = CurrentLevel.customCameraFocusIntensity == null ? gameCamera.focusOnPlayerStrength : (float)CurrentLevel.customCameraFocusIntensity;
		gameCamera.maxOrthographicSize = CurrentLevel.customMaxZoomOut == null ? gameCamera.maxOrthographicSize : (float)CurrentLevel.customMaxZoomOut;
		UI.counterBanner.SetActive(false);
		player.EnableControls();
		EnableAllAIs();

		foreach(var t in FindObjectsOfType<TankBase>()) {
			t.InitializeTank();
		}

		if(IsEditor) {
			Editor.playTestButton.interactable = true;
		}
		Game.IsGamePlaying = true;
	}

	public void TankDestroyedCheck() {
		AddScore();
		if(tankAIs != null) {
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

	public void AddScore() {
		if(Game.IsGameRunningDebug == false) {
			GameManager.Score++;
			UI.playerScore.SetText(GameManager.Score.ToString());
		} else {
			UI?.playerScore.SetText(Random.Range(0, 9).ToString());
		}
	}

	public void PlayerDead() {
		Game.IsGamePlaying = false;
		player.DisableControls();
		DisableAllAIs();

		if(Game.IsGameRunningDebug == false) {
			if(Difficulty != SaveGame.Campaign.Difficulty.Easy && Mode != GameManager.GameMode.LevelOnly && GameManager.PlayerLives > 0) {
				GameManager.PlayerLives--;
				UI.playerLives.SetText(GameManager.PlayerLives.ToString());
				GameManager.UpdateCampaign();
			}

			// Respawn and Continue Level if lives are sufficient
			if(GameManager.PlayerLives > 0 || GameManager.Difficulty == SaveGame.Campaign.Difficulty.Easy) {
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
			player.DisableCrossHair();
			player.DisableControls();
			DisableAllAIs();

			// Decide next GameOver step when player lives reach ZERO
			switch(GameManager.CurrentMode) {
				case GameManager.GameMode.Campaign:
					if(GameManager.PlayerLives > 0 || GameManager.Difficulty == SaveGame.Campaign.Difficulty.Easy) {
						// Continue when players live are sufficient OR playing in EASY
						SaveGame.UnlockLevel(SaveGame.SaveInstance.currentSaveSlot, GameManager.LevelId);
						GameManager.LevelId++;
						if(Random.Range(0f, 1f) > GameManager.LiveGainChance) {
							GameManager.PlayerLives++;
							GameManager.LiveGainChance = 0;
							UI.playerLives.SetText(GameManager.PlayerLives.ToString());
						}
						GameManager.LiveGainChance += 0.02f;
						GameManager.UpdateCampaign();
						gameCamera.PlayConfetti();
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
								Logger.Log(Channel.Gameplay, "Returning to Checkpoint: " + GameManager.LevelId);
								yield return new WaitForSeconds(3);
								GameManager.LoadLevel("", true);
								break;
							case SaveGame.Campaign.Difficulty.Hard:
								// Hard Mode resets to the currents world first level
								GameManager.PlayerLives = 2;
								GameManager.LevelId = Game.GetWorld(GameManager.CurrentLevel.levelId).Levels[0].LevelId;
								Logger.Log(Channel.Graphics, "Returning to Checkpoint: " + GameManager.LevelId);
								yield return new WaitForSeconds(2);
								GameManager.LoadLevel("Mission Failed");
								break;
							case SaveGame.Campaign.Difficulty.HardCore:
								// HardCore deletes SaveSlot
								Logger.Log(Channel.SaveGame, "Player failed, wiping SaveSlot: " + SaveGame.SaveInstance.currentSaveSlot);
								SaveGame.SaveInstance.WipeSlot(SaveGame.SaveInstance.currentSaveSlot);
								SaveGame.Save();
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
		}
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
			case GridSizes.Size_17x14:
				return 16;
			case GridSizes.Size_14x11:
				return 15;
			case GridSizes.Size_11x8:
				return 12;
			default:
				return 15;
		}
	}

	public static Int3 GetGridBoundary(GridSizes size) {
		switch(size) {
			case GridSizes.Size_17x14:
				return new Int3(17, 4, 14);
			case GridSizes.Size_14x11:
				return new Int3(14, 4, 11);
			case GridSizes.Size_11x8:
				return new Int3(11, 4, 9);
			default:
				return new Int3(15, 4, 12);
		}
	}

	public void ResetLevel() {
		foreach(GameEntity entity in FindObjectsOfType<GameEntity>()) {
			if(entity is IResettable) {
				IResettable r = entity as IResettable;
				r.ResetState();
			}
		}
	}

	public TankAsset GetTankAsset(TankTypes type) => tankPrefabs.Find(t => t.tankType == type);
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