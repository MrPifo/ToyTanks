using UnityEngine;
using CarterGames.Assets.AudioManager;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using Sperlich.Pathfinding;
using Sperlich.Types;
using Sperlich.Debug.Draw;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;
using ToyTanks.LevelEditor;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.Rendering.HighDefinition;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelManager : MonoBehaviour {

	public float gridDensity = 2;
	public float gridPointDistance = 3f;
	public float gridPointOverlapRadius = 1f;
	public bool showGrid;
	public bool IsGameOver;
	public bool IsDebug;
	public const int maxTracksOnStage = 1500;
	public bool IsBossLevel;
	public bool GameStarted;
	public bool IsPathMeshReady;
	public bool HasBeenInitialized;
	public bool HasLevelBeenBuild;
	public GameObject optionsMenu;
	public HDAdditionalLightData sunLight;
	public LayerMask baseLayer;
	public bool IsEditor => Mode == GameManager.GameMode.Editor;
	public bool isOptionsMenuOpen => optionsMenu.activeSelf;
	public static LevelEditor Editor;
	public static PathfindingMesh Grid;
	public static GameCamera gameCamera;
	public static GameManager.GameMode Mode {
		get => GameManager.CurrentMode;
		set => GameManager.CurrentMode = value;
	}
	public static bool ReviveDeadAI = false;
	public static bool GamePaused;
	public static LevelUI UI;
	public static LevelFeedbacks Feedback;
	public static LevelData CurrentLevel => GameManager.CurrentLevel;
	public static Int3 GridBoundary => GetGridBoundary(CurrentLevel.gridSize);
	public static Transform BlocksContainer => GameObject.FindGameObjectWithTag("LevelBlocks").transform;
	public static Transform TanksContainer => GameObject.FindGameObjectWithTag("LevelTanks").transform;
	public static SaveGame.Campaign.Difficulty Difficulty => SaveGame.GetCampaign(SaveGame.SaveInstance.currentSaveSlot).difficulty;
	public static Transform TrackContainer;
	GameManager GameManager;
	BossAI bossTank;

	[HideInInspector] public PlayerInput player;
	[HideInInspector] public static AudioManager audioManager;
	[HideInInspector] public TankAI[] tankAIs;
	public Scene Scene => gameObject.scene;

	// Initialization
	void Awake() {
		Editor = FindObjectOfType<LevelEditor>();
		Grid = FindObjectOfType<PathfindingMesh>();
		GameManager = FindObjectOfType<GameManager>();
		UI = FindObjectOfType<LevelUI>();
		audioManager = FindObjectOfType<AudioManager>();
		Feedback = FindObjectOfType<LevelFeedbacks>();
		gameCamera = FindObjectOfType<GameCamera>();
		TrackContainer = new GameObject("TrackContainer").transform;
		UI.gameplay.SetActive(false);
		UI.crossHair.gameObject.SetActive(false);
		optionsMenu.SetActive(false);

		// Start Editor if no GameManager is present
		if(GameManager == false) {
			Mode = GameManager.GameMode.Editor;
			IsDebug = true;
			Editor.ClearLevel();
			Editor.StartLevelEditor();
		}

		GraphicSettings.OnGraphicSettingsOpen.AddListener(CloseOptionsMenu);
		GraphicSettings.OnGraphicSettingsClose.AddListener(OpenOptionsMenu);
		CloseOptionsMenu();
	}

	public void Initialize() {
		if(HasBeenInitialized == false) {
			// Must be called before TankBase Script
			if(GameManager.CurrentMode == GameManager.GameMode.Campaign || GameManager.CurrentMode == GameManager.GameMode.LevelOnly) {
				ReviveDeadAI = false;
				HasBeenInitialized = true;
				Destroy(Editor.gameObject);
			}
		}
	}

	void InitializeTanks() {
		player = FindObjectOfType<PlayerInput>();
		tankAIs = FindObjectsOfType<TankAI>();

		// Ensure debug is set off
		foreach(var t in FindObjectsOfType<TankBase>()) {
			if(t as TankAI) {
				var ai = t as TankAI;
				if(t is BossAI) {
					bossTank = t as BossAI;
					IsBossLevel = true;
				}
			}
		}
		player.DisablePlayer();
		DisableAllAIs();
	}

	void Update() {
		if(IsGameOver == false && GameStarted) {
			CheckTankTracks();
			UpdateRunTime();

#if UNITY_EDITOR
			if(showGrid) {
				DrawGridLines();
				DrawGridPoints();
			}
#endif
		}
		if(Input.GetKeyDown(KeyCode.Escape) && HasBeenInitialized) {
			if(isOptionsMenuOpen) {
				CloseOptionsMenu();
			} else {
				OpenOptionsMenu();
			}
		}
	}

	void OpenOptionsMenu() {
		optionsMenu.SetActive(true);
		GamePaused = true;
		Time.timeScale = 0;
	}

	public void CloseOptionsMenu() {
		optionsMenu.SetActive(false);
		GamePaused = false;
		Time.timeScale = 1;
	}

	void UpdateRunTime() {
		if(GameStarted) {
			GameManager.PlayTime += Time.deltaTime;
			UI.playTime.SetText(Mathf.Round(GameManager.PlayTime * 100f) / 100f + "s");
		}
	}

	public void ClearMap() {
		foreach(var b in FindObjectsOfType<LevelBlock>()) {
			Destroy(b.gameObject);
		}
		foreach(var t in FindObjectsOfType<TankBase>()) {
			Destroy(t.gameObject);
		}
		var ground = GameObject.FindGameObjectWithTag("Ground").GetComponent<MeshRenderer>();
		ground.lightmapScaleOffset = new Vector4(0, 0, 0, 0);
	}

	public bool LoadAndBuildMap(LevelData data) {
		ClearMap();
		var blockAssets = Resources.LoadAll<ThemeAsset>("LevelAssets").ToList().Find(t => t.theme == data.theme);
		var tanks = Resources.LoadAll<TankAsset>("Tanks").ToList();
		gameCamera.SetOrthographicSize(GetOrthographicSize(CurrentLevel.gridSize));

		foreach(var block in data.blocks) {
			ThemeAsset.BlockAsset asset = blockAssets.GetAsset(block.type);
			var b = Instantiate(asset.prefab, block.pos, Quaternion.Euler(block.rotation)).GetComponent<LevelBlock>();
			b.transform.SetParent(BlocksContainer);
			b.Index = block.index;
			//b.gameObject.layer = LayerMask.NameToLayer("Level");
			b.meshRender.sharedMaterial = asset.material;
			b.gameObject.isStatic = true;
			b.GetComponent<LevelBlock>().SetPosition(block.pos);
		}

		foreach(var tank in data.tanks) {
			var asset = tanks.Find(t => t.tankType == tank.tankType);
			var t = Instantiate(asset.prefab, tank.pos + asset.tankSpawnOffset, Quaternion.Euler(tank.rotation)).GetComponent<TankBase>();
		}
		var floor = GameObject.FindGameObjectWithTag("Ground").GetComponent<MeshRenderer>();
		floor.sharedMaterial = blockAssets.floorMaterial;
		LevelLightmapper.SwitchLightmaps(CurrentLevel.levelId);
		sunLight.SetShadowResolutionOverride(true);
		sunLight.SetShadowResolution(GraphicSettings.GetShadowResolution());
		return true;
	}

	void SetLevelBoundaryWalls() {
		GameObject.Find("WallRight").transform.position = new Vector3(GridBoundary.x * 2, 0, 0);
		GameObject.Find("WallLeft").transform.position = new Vector3(GridBoundary.x * -2 - 2, 0, 0);
		GameObject.Find("WallUp").transform.position = new Vector3(0, 0, GridBoundary.z * 2);
		GameObject.Find("WallDown").transform.position = new Vector3(0, 0, GridBoundary.z * -2 - 2);
	}

	IEnumerator GenerateDynamicGrid() {
		Grid.gridName = "temp";
		Grid.ClearGrid();
		int total = 0;

		for(float x = -GridBoundary.x; x < GridBoundary.x; x++) {
			for(float z = -GridBoundary.z; z < GridBoundary.z; z++) {
				var ray = new Ray(new Vector3(gridDensity * x, transform.position.y + 20, gridDensity * z) + transform.position, Vector3.down);
				if(Physics.SphereCast(ray.origin, gridPointOverlapRadius, ray.direction, out RaycastHit hit, Mathf.Infinity, baseLayer)) {
					if(hit.transform.CompareTag("Ground")) {
						Grid.AddNode(new Float3(hit.point), gridPointDistance, Node.NodeType.ground, true);
						if(total % 5 == 0) {
							yield return null;
						}
					}
				}
				total++;
			}
		}
		Grid.gridName = gameObject.scene.name;
		Grid.Reload();
		if(IsEditor) {
			Editor.pathMeshGeneratorProgressBar.gameObject.SetActive(false);
		}
		IsPathMeshReady = true;
	}

	// Game Logic
	public void StartGame() => StartCoroutine(nameof(IStartGame));
	IEnumerator IStartGame() {
		if(Mode != GameManager.GameMode.Editor && HasLevelBeenBuild == false) {
			yield return new WaitUntil(() => LoadAndBuildMap(CurrentLevel));
			HasLevelBeenBuild = true;
		}

		SetLevelBoundaryWalls();
		UI.gameplay.SetActive(true);
		UI.levelStage.SetText($"Level {CurrentLevel.levelId}");
		UI.counterBanner.SetActive(true);
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Confined;

		if(!IsDebug) {
			UI.playerScore.SetText(GameManager.Score.ToString());
			UI.playerLives.SetText(GameManager.PlayerLives.ToString());
			UI.playTime.SetText(Mathf.Round(GameManager.PlayTime * 100f) / 100f + "s");
		} else {
			UI.playerScore.SetText("0");
			UI.playerLives.SetText("0");
			UI.playTime.SetText("0");
		}

		Feedback.FadeInGameplayUI();
		InitializeTanks();
		player.SetupCross();
		StartCoroutine(GenerateDynamicGrid());
		if(IsBossLevel) {
			UI.InitBossBar(bossTank.MaxHealth, 3);
		}
		yield return new WaitForSeconds(2.5f);
		yield return new WaitUntil(() => IsPathMeshReady);
		Feedback.PlayStartFadeText();
		DOTween.To(x => UI.OutlinePass.threshold = x, UI.OutlinePass.threshold, 100, 2).SetEase(Ease.InCubic);
		yield return new WaitForSeconds(1);
		IsGameOver = false;
		GameStarted = true;
		UI.counterBanner.SetActive(false);
		player.EnablePlayer();
		EnableAllAIs();

		foreach(var t in FindObjectsOfType<TankBase>()) {
			t.InitializeTank();
		}

		if(IsEditor) {
			Editor.playTestButton.interactable = true;
		}
	}

	public void TankDestroyedCheck() {
		AddScore();
		foreach(TankAI t in tankAIs) {
			if(t.HasBeenDestroyed == false) {
				// All TankAIs must be destroyed or else returns
				return;
			}
		}
		StartCoroutine(GameOver());
	}

	public void AddScore() {
		if(!IsDebug) {
			GameManager.Score++;
			UI.playerScore.SetText(GameManager.Score.ToString());
			Feedback.PlayScore();
		} else {
			UI.playerScore.SetText(Random.Range(0, 9).ToString());
			Feedback.PlayScore();
		}
	}

	public void PlayerDead() {
		player.DisablePlayer();
		DisableAllAIs();

		if(!IsDebug) {
			if(Difficulty != SaveGame.Campaign.Difficulty.Easy && Mode != GameManager.GameMode.LevelOnly && GameManager.PlayerLives > 0) {
				GameManager.PlayerLives--;
				UI.playerLives.SetText(GameManager.PlayerLives.ToString());
				GameManager.UpdateCampaign();
			}

			// Respawn and Continue Level if lives are sufficient
			if(GameManager.PlayerLives > 0 || GameManager.Difficulty == SaveGame.Campaign.Difficulty.Easy) {
				Respawn();
			} else {
				StartCoroutine(GameOver());
			}
		} else if(Mode == GameManager.GameMode.Editor) {
			Editor.StopTestPlay();
		}
	}

	public void Respawn() => StartCoroutine(IRespawnAnimate());
	IEnumerator IRespawnAnimate() {
		Feedback.PlayLives();
		yield return new WaitForSeconds(4f);
		player.Revive();

		foreach(TankAI t in FindObjectsOfType<TankAI>()) {
			if(t.HasBeenDestroyed == false) {
				t.Revive();
			} else if(ReviveDeadAI) {
				t.Revive();
			}
		}
		yield return new WaitForSeconds(1f);
		StartGame();
	}

	IEnumerator GameOver() {
		if(!IsDebug && IsGameOver == false) {
			IsGameOver = true;
			player.DisablePlayer();
			DisableAllAIs();

			// Decide next GameOver step when player lives reach ZERO
			switch(GameManager.CurrentMode) {
				case GameManager.GameMode.Campaign:
					if(GameManager.PlayerLives > 0 || GameManager.Difficulty == SaveGame.Campaign.Difficulty.Easy) {
						// Continue when players live are sufficient OR playing in EASY
						SaveGame.UnlockLevel(SaveGame.SaveInstance.currentSaveSlot, GameManager.LevelId);
						GameManager.LevelId++;
						GameManager.UpdateCampaign();
						Debug.Log("Continue to next Level: " + GameManager.LevelId);
						yield return new WaitForSeconds(2);
						GameManager.LoadLevel();
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
								Debug.Log("Returning to Checkpoint in Level: " + GameManager.LevelId);
								yield return new WaitForSeconds(2);
								GameManager.LoadLevel();
								break;
							case SaveGame.Campaign.Difficulty.Hard:
								// Hard Mode resets to the currents world first level
								GameManager.PlayerLives = 2;
								GameManager.LevelId = Game.GetWorld(GameManager.CurrentLevel.levelId).Levels[0].LevelId;
								Debug.Log("Returning to Checkpoint in Level: " + GameManager.LevelId);
								yield return new WaitForSeconds(2);
								GameManager.LoadLevel();
								break;
							case SaveGame.Campaign.Difficulty.HardCore:
								// HardCore deletes SaveSlot
								Debug.Log("Wiping SaveSlot " + SaveGame.SaveInstance.currentSaveSlot);
								SaveGame.SaveInstance.WipeSlot(SaveGame.SaveInstance.currentSaveSlot);
								SaveGame.Save();
								yield return new WaitForSeconds(2);
								GameManager.ReturnToMenu("Mission Failed");
								break;
						}
					}
					break;
				case GameManager.GameMode.LevelOnly:
					yield return new WaitForSeconds(2);
					Feedback.FadeOutGameplayUI();
					GameManager.ReturnToMenu("Returning to Menu");
					break;
				case GameManager.GameMode.Editor:
					yield return null;
					Editor.StopTestPlay();
					break;
			}
		}
	}

	// Check and Helper
	void CheckTankTracks() {
		if(TrackContainer != null && TrackContainer.childCount > maxTracksOnStage) {
			Destroy(TrackContainer.GetChild(0).gameObject);
		}
	}

	void DisableAllAIs() {
		foreach(TankAI t in tankAIs) {
			t.DisableAI();
			t.makeInvincible = true;
		}
	}

	void EnableAllAIs() {
		foreach(TankAI t in tankAIs) {
			t.EnableAI();
			t.makeInvincible = false;
		}
	}

	public static string GetGridBoundaryText(GridSizes size) => size.ToString().Replace("Size_", "");

	public static float GetOrthographicSize(GridSizes size) {
		switch(size) {
			case GridSizes.Size_17x14:
				return 19;
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

#if UNITY_EDITOR
	public void GenerateGrid() {
		Grid.ClearGrid();
		StopAllCoroutines();

		for(float x = -GridBoundary.x; x <= GridBoundary.x; x++) {
			for(float z = -GridBoundary.y; z <= GridBoundary.y; z++) {
				var ray = new Ray(new Vector3(x * gridDensity, transform.position.y + 20, z * gridDensity) + transform.position, Vector3.down);
				float dist = Grid.painter.paintRadius;
				if(Grid.enableCrossConnections) {
					dist *= Grid.crossDistance;
				}
				if(Physics.SphereCast(ray.origin, 0.25f, ray.direction, out RaycastHit hit, Mathf.Infinity, LayerMask.NameToLayer("Level"))) {
					if(hit.transform.CompareTag("Ground")) {
						Grid.AddNode(new Float3(hit.point), dist, Node.NodeType.ground);
					}
				}
			}
		}
		Grid.gridName = gameObject.scene.name;
		Grid.Reload();
		Grid.SaveGrid();
	}

	void OnDrawGizmos() {
		if(showGrid) {
			DrawGridLines();
			DrawGridPoints();
		}
	}

	public void DrawGridLines() {
		if(Grid != null) {
			foreach(Node n in Grid.Nodes) {
				foreach(KeyValuePair<Node, float> neigh in n.Neighbours) {
					Draw.Line(n.pos, neigh.Key.pos, Color.white, true);
				}
			}
		}
	}

	public void DrawGridPoints() {
		if(Grid != null) {
			foreach(Node n in Grid.Nodes) {
				if(n.type == Node.NodeType.ground) {
					Draw.Sphere(n.pos, 0.2f, Color.white, true);
				} else {
					Draw.Sphere(n.pos, 0.2f, Color.red, true);
				}
			}
		}
	}
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(LevelManager))]
public class LevelManagerEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		LevelManager builder = (LevelManager)target;
		if(GUILayout.Button("Reset")) {
			builder.Respawn();
		}
		if(GUILayout.Button("Generate Grid")) {
			builder.GenerateGrid();
		}
	}
}
#endif