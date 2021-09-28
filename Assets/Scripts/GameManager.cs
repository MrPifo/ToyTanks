using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Security.Cryptography;
using System;

[RequireComponent(typeof(MMFeedbacks))]
public class GameManager : MonoBehaviour {

	public enum GameMode { None, Campaign, LevelOnly, Editor }

	[SerializeField] int startLevel;
	[SerializeField] Texture2D defaultCursor;
	[SerializeField] Texture2D pointerCursor;

	// Game information
	public static ulong LevelId {
		get => CurrentCampaign.levelId;
		set => CurrentCampaign.levelId = value;
	}
	public static byte PlayerLives {
		get => CurrentCampaign.lives;
		set => CurrentCampaign.lives = value;
	}
	public static short Score {
		get => CurrentCampaign.score;
		set => CurrentCampaign.score = value;
	}
	public static float PlayTime {
		get => CurrentCampaign.time;
		set => CurrentCampaign.time = value;
	}
	public static bool isLoading;
	public static SaveGame.Campaign.Difficulty Difficulty => CurrentCampaign.difficulty;
	public static GameMode CurrentMode;

	public static GameManager Instance;
	public static MMFeedbackLoadScene loader;
	public static MMFeedbacks feedbacks;
	public static LoadingScreen screen;
	public static List<LevelData> Levels { get; set; }
	static LevelData _currentLevel;
	public static LevelData CurrentLevel {
		get {
			LevelData level = Levels?.Find(l => l.levelId == LevelId);
			if(level == null) {
				return _currentLevel;
			}
			return level;
		}
		set => _currentLevel = value;
	}
	static SaveGame.Campaign CurrentCampaign => SaveGame.GetCampaign(SaveGame.SaveInstance.currentSaveSlot);
	UnityAction<Scene, LoadSceneMode> loadingScreenStartedCallback;
	UnityAction<Scene, Scene> loadingScreenExitCallback;
	private static readonly System.Random RandomGenerator = new System.Random();

	void Awake() {
		DontDestroyOnLoad(this);
		Game.AddCursor("default", defaultCursor);
		Game.AddCursor("pointer", pointerCursor);
		Instance = this;
		feedbacks = GetComponent<MMFeedbacks>();
		loader = feedbacks.GetComponent<MMFeedbackLoadScene>();
		SaveGame.GameStartUp();
		Game.SetCursor("default");
		GraphicSettings.Initialize();
	}

	public void CopyCamera() {
		var mainCam = Camera.main;
		var thisCam = GameObject.FindGameObjectWithTag("LoadingScreenCamera").GetComponent<Camera>();

		thisCam.transform.position = mainCam.transform.position;
		thisCam.transform.rotation = mainCam.transform.rotation;
		thisCam.orthographicSize = mainCam.orthographicSize;
		thisCam.nearClipPlane = mainCam.nearClipPlane;
		thisCam.farClipPlane = mainCam.farClipPlane;
	}

	public static void StartLevel(ulong levelId) {
		CurrentMode = GameMode.LevelOnly;
		PlayerLives = 1;
		LevelId = levelId;
		Instance.LoadAllAvailableLevels();
		Instance.LoadLevel();
	}

	public static void StartEditor(LevelData level) {
		CurrentMode = GameMode.Editor;
		LevelId = level.levelId;
		CurrentLevel = level;
		Instance.StartEditor();
	}
	public void StartEditor() {
		OnLoadingScreenEntered(() => {
			FindObjectOfType<MMAdditiveSceneLoadingManager>().OnAfterEntryFade.AddListener(() => {
				screen = FindObjectOfType<LoadingScreen>();
				screen.SetSingleMessage("");
			});
		});
		OnLoadingScreenEntered(() => {
			CopyCamera();
			OnLoadingScreenExit(() => {
				var level = FindObjectOfType<LevelManager>();
				LevelManager.Editor.StartLevelEditor();
				LevelManager.Editor.LoadUserLevel(CurrentLevel);
				CopyCamera();
			});
		});
		StartTransitionToScene("Level");
	}

	public void LoadLevel() {
		if(!isLoading) {
			isLoading = true;
			if(LevelExists(LevelId)) {
				OnLoadingScreenEntered(() => {
					FindObjectOfType<MMAdditiveSceneLoadingManager>().OnAfterEntryFade.AddListener(() => {
						screen = FindObjectOfType<LoadingScreen>();
						screen.SetInfo($"Level {LevelId}", $"{PlayerLives} x");
					});
				});
				OnLoadingScreenEntered(() => {
					CopyCamera();
					
					OnLoadingScreenExit(() => {
						var level = FindObjectOfType<LevelManager>();
						level.Initialize();
						CopyCamera();
						level.StartGame();
					});
				});
				StartTransitionToScene("Level");
			}
		}
	}

	public void ReturnToMenu(string customMessage) {
		isLoading = true;
		OnLoadingScreenEntered(() => {
			FindObjectOfType<MMAdditiveSceneLoadingManager>().OnAfterEntryFade.AddListener(() => {
				screen = FindObjectOfType<LoadingScreen>();
				screen.SetSingleMessage(customMessage);
			});
		});
		OnLoadingScreenExit(() => {
			ResetGameStatus();
		});
		StartTransitionToScene("Menu");
	}
	
	public void ResetGameStatus() {
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.Confined;
		PlayTime = 0;
		PlayerLives = 0;
		Score = 0;
		LevelId = 0;
		isLoading = false;
		CurrentMode = GameMode.None;
		Destroy(gameObject);
	}

	public void LoadAllAvailableLevels() {
		Levels = new List<LevelData>();
		foreach(var asset in Resources.LoadAll<TextAsset>("Levels")) {
			var levelData = JsonConvert.DeserializeObject<LevelData>(asset.text);
			Levels.Add(levelData);
		}
	}

	public void StartCampaign(byte saveSlot) {
		if(!isLoading) {
			LoadAllAvailableLevels();
			Debug.Log("Starting campaign on save slot " + saveSlot);
			var camp = SaveGame.GetCampaign(saveSlot);
			CurrentMode = GameMode.Campaign;
			LevelId = camp.levelId;
			PlayTime = camp.time;
			Score = camp.score;
			PlayerLives = camp.lives;
			LoadLevel();
		}
	}

	public static void UpdateCampaign() => SaveGame.UpdateCampaign(LevelId, PlayerLives, Score, PlayTime);

	public void OnLoadingScreenEntered(UnityAction callback) {
		loadingScreenStartedCallback = (Scene scene, LoadSceneMode mode) => {
			if(scene.name == "LoadingScreen") {
				SceneManager.sceneLoaded -= loadingScreenStartedCallback;
				FindObjectOfType<MMAdditiveSceneLoadingManager>().OnEntryFade.AddListener(() => {
					callback.Invoke();
				});
			}
		};
		SceneManager.sceneLoaded += loadingScreenStartedCallback;
	}

	public void OnLoadingScreenExit(UnityAction callback) {
		loadingScreenExitCallback = (Scene from, Scene to) => {
			if(to.name == "LoadingScreen") {
				SceneManager.activeSceneChanged -= loadingScreenExitCallback;
				FindObjectOfType<MMAdditiveSceneLoadingManager>().OnExitFade.AddListener(() => {
					isLoading = false;
					callback.Invoke();
				});
			}
		};
		SceneManager.activeSceneChanged += loadingScreenExitCallback;
	}

	public void StartTransitionToScene(string scene) {
		SetSceneLoadName(scene);
		feedbacks.PlayFeedbacks();
	}

	void SetSceneLoadName(string scene) => loader.DestinationSceneName = scene;

	// Helpers
	public bool LevelExists(ulong levelId) => Levels.Exists(t => t.levelId == levelId);
	// Return a random ulong between a min and max value.
	public static ulong GetRandomLevelId(ulong min, ulong max) {
		byte[] buffer = new byte[sizeof(ulong)];
		RandomGenerator.NextBytes(buffer);
		return BitConverter.ToUInt64(buffer, 0) % (max - min + 1) + min;
	}
}
