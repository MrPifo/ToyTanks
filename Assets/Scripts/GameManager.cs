using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Collections;
using Sperlich.PrefabManager;

public class GameManager : Singleton<GameManager> {

	public enum GameMode { None, Campaign, LevelOnly, Editor }

	public float loadingScreenFadeDuration = 2f;
	public float bannerFadeDuration = 1f;
	public float totalLoadingFadeDuration = 2f;

	// Game information
	private static ulong _levelId;
	public static ulong LevelId {
		get => CurrentCampaign is null ? _levelId : CurrentCampaign.levelId;
		set { if(CurrentCampaign is null) _levelId = value; else CurrentCampaign.levelId = value; }
	}
	private static byte _playerLives;
	public static byte PlayerLives {
		get => CurrentCampaign is null ? _playerLives : CurrentCampaign.lives;
		set { if(CurrentCampaign is null) _playerLives = value; else CurrentCampaign.lives = value; }
	}
	private static short _score;
	public static short Score {
		get => CurrentCampaign is null ? _score : CurrentCampaign.score;
		set { if(CurrentCampaign is null) _score = value; else CurrentCampaign.score = value; }
	}
	private static float _playTime;
	public static float PlayTime {
		get => CurrentCampaign is null ? _playTime : CurrentCampaign.time;
		set {
			if(CurrentCampaign is null) _playTime = value; else CurrentCampaign.time = value;
		}
	}
	private static float _liveGainChance;
	public static float LiveGainChance {
		get => CurrentCampaign is null ? _liveGainChance : CurrentCampaign.liveGainChance;
		set {
			if(CurrentCampaign is null) _liveGainChance = value; else CurrentCampaign.liveGainChance = value;
		}
	}
	public static bool isLoading;
	public static bool rewardLive;
	public static SaveGame.Campaign.Difficulty Difficulty => CurrentCampaign.difficulty;
	public static GameMode CurrentMode;

	public static LoadingScreen screen;
	public static bool GameBooted = false;
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

	protected override void Awake() {
		Game.Initialize();
		base.Awake();
		PrefabManager.ResetPrefabManager();
		var menu = FindObjectOfType<MenuManager>();
		menu.Initialize();
		if(GameBooted == false) {
			menu.mainMenu.FadeIn();
			menu.FadeOutBlur();
			GameBooted = true;
		}
		ShowCursor();
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
		LevelId = levelId;
		PlayerLives = 0;
		Score = 0;
		PlayTime = 0;
		Instance.LoadAllAvailableLevels();
		Instance.LoadLevel("Level " + levelId);
	}

	public static void StartEditor(LevelData level) {
		CurrentMode = GameMode.Editor;
		LevelId = level.levelId;
		CurrentLevel = level;
		Instance.LoadLevel("Starting Editor");
	}

	public void ResetGameStatus() {
		PlayTime = 0;
		PlayerLives = 0;
		Score = 0;
		LevelId = 0;
		isLoading = false;
		CurrentMode = GameMode.None;
		ShowCursor();
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
			Logger.Log(Channel.SaveGame, "Starting campaign on save slot " + saveSlot);
			var camp = SaveGame.GetCampaign(saveSlot);
			CurrentMode = GameMode.Campaign;
			LevelId = camp.levelId;
			PlayTime = camp.time;
			Score = camp.score;
			PlayerLives = camp.lives;
			LoadLevel("", true);
		}
	}

	public static void UpdateCampaign() => SaveGame.UpdateCampaign(LevelId, PlayerLives, Score, PlayTime);

	public void ReturnToMenu(string message) => StartCoroutine(TransitionToMenu(message));
	public void LoadLevel(string message, bool displayCampaignInformation = false) {
		if(Levels.Find(l => l.levelId == LevelId) != null) {
			if(SceneManager.GetSceneByName("Level").IsValid() == false && SceneManager.GetSceneByName("Level").isLoaded == false) {
				StartCoroutine(TransitionToLevel(message, displayCampaignInformation));
			} else {
				StartCoroutine(TransitionToNextLevel());
			}
		} else {
			ReturnToMenu("Campaign End");
		}
	}
	IEnumerator TransitionToLevel(string message, bool displayCampaignInformation = false) {
		Scene activeScene = SceneManager.GetActiveScene();

		#region Loading LoadingScreen
		// Load LoadingScreen
		AsyncOperation loadingScreen = SceneManager.LoadSceneAsync("LoadingScreen", LoadSceneMode.Additive);
		loadingScreen.allowSceneActivation = true;

		yield return new WaitUntil(() => loadingScreen.isDone);
		LoadingScreen transitionScreen = FindObjectOfType<LoadingScreen>();
		transitionScreen.FadeIn(loadingScreenFadeDuration);

		yield return new WaitUntil(() => transitionScreen.onFadeInFinished);
		#endregion

		#region LoadingLevel
		AsyncOperation level = SceneManager.LoadSceneAsync("Level", LoadSceneMode.Additive);
		level.allowSceneActivation = true;
		yield return new WaitUntil(() => level.isDone);
		LevelManager levelManager = FindObjectOfType<LevelManager>();
		yield return new WaitForSeconds(0.25f);
		#endregion

		#region Unloading Menu/Level
		if(activeScene.name == "Menu") {
			AsyncOperation unloadLevel = SceneManager.UnloadSceneAsync("Menu");
			unloadLevel.allowSceneActivation = true;
			yield return new WaitUntil(() => unloadLevel.isDone);
		} else if(activeScene.name == "Level") {
			AsyncOperation unloadLevel = SceneManager.UnloadSceneAsync("Level");
			yield return new WaitUntil(() => unloadLevel.isDone);
		}
		#endregion

		#region BannerInformation
		transitionScreen.FadeInBanner(bannerFadeDuration);
		if(displayCampaignInformation) {
			if(Difficulty == SaveGame.Campaign.Difficulty.Easy) {
				transitionScreen.SetSingleMessage("Mission " + LevelId);
			} else {
				transitionScreen.SetInfo("Mission " + LevelId, PlayerLives);
			}
		} else {
			transitionScreen.SetSingleMessage(message);
		}
		yield return new WaitUntil(() => transitionScreen.onBannerFadeInFinished);
		#endregion

		#region Initializing and Building Level
		PrefabManager.ResetPrefabManager();
		yield return new WaitForSeconds(0.15f);
		PrefabManager.Initialize();
		yield return new WaitForSeconds(0.15f);
		levelManager.Initialize();
		yield return new WaitForSeconds(0.15f);
		yield return levelManager.LoadAndBuildMap(CurrentLevel, totalLoadingFadeDuration);
		#endregion

		transitionScreen.FadeOutBanner(bannerFadeDuration);
		yield return new WaitUntil(() => transitionScreen.onBannerFadeOutFinished);

		transitionScreen.FadeOut(loadingScreenFadeDuration);
		yield return new WaitUntil(() => transitionScreen.onFadeOutFinished);
		AsyncOperation loadingScreenUnload = SceneManager.UnloadSceneAsync("LoadingScreen");
		yield return new WaitUntil(() => loadingScreenUnload.isDone);
		switch(CurrentMode) {
			case GameMode.Campaign:
				levelManager.StartGame();
				break;
			case GameMode.LevelOnly:
				levelManager.StartGame();
				break;
			case GameMode.Editor:
				LevelManager.Editor.StartLevelEditor();
				LevelManager.Editor.LoadUserLevel(CurrentLevel);
				break;
		}
	}
	IEnumerator TransitionToMenu(string message) {
		Scene activeScene = SceneManager.GetActiveScene();

		// Load LoadingScreen
		AsyncOperation loadingScreen = SceneManager.LoadSceneAsync("LoadingScreen", LoadSceneMode.Additive);
		loadingScreen.allowSceneActivation = true;
		yield return new WaitUntil(() => loadingScreen.isDone);

		LoadingScreen transitionScreen = FindObjectOfType<LoadingScreen>();
		transitionScreen.FadeIn(loadingScreenFadeDuration);
		yield return new WaitUntil(() => transitionScreen.onFadeInFinished);

		transitionScreen.FadeInBanner(bannerFadeDuration);
		transitionScreen.SetSingleMessage(message);
		yield return new WaitUntil(() => transitionScreen.onBannerFadeInFinished);

		AsyncOperation unloadLevel = SceneManager.UnloadSceneAsync("Level");
		yield return new WaitUntil(() => unloadLevel.isDone);

		AsyncOperation menu = SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Additive);
		menu.allowSceneActivation = true;
		yield return new WaitUntil(() => menu.isDone);

		yield return new WaitForSeconds(totalLoadingFadeDuration);
		transitionScreen.FadeOutBanner(bannerFadeDuration);
		yield return new WaitUntil(() => transitionScreen.onBannerFadeOutFinished);

		transitionScreen.FadeOut(loadingScreenFadeDuration);
		FindObjectOfType<MenuManager>().mainMenu.FadeIn();
		FindObjectOfType<MenuManager>().FadeOutBlur();
		yield return new WaitUntil(() => transitionScreen.onFadeOutFinished);
		AsyncOperation loadingScreenUnload = SceneManager.UnloadSceneAsync("LoadingScreen");
		yield return new WaitUntil(() => loadingScreenUnload.isDone);
	}
	IEnumerator TransitionToNextLevel() {
		LevelManager levelManager = FindObjectOfType<LevelManager>();

		#region Loading LoadingScreen
		// Load LoadingScreen
		AsyncOperation loadingScreen = SceneManager.LoadSceneAsync("LoadingScreen", LoadSceneMode.Additive);
		loadingScreen.allowSceneActivation = true;

		yield return new WaitUntil(() => loadingScreen.isDone);
		LoadingScreen transitionScreen = FindObjectOfType<LoadingScreen>();
		transitionScreen.FadeIn(loadingScreenFadeDuration);

		yield return new WaitUntil(() => transitionScreen.onFadeInFinished);
		#endregion

		#region BannerInformation
		transitionScreen.FadeInBanner(bannerFadeDuration);
		if(Difficulty == SaveGame.Campaign.Difficulty.Easy) {
			transitionScreen.SetSingleMessage("Mission " + LevelId);
		} else {
			transitionScreen.SetInfo("Mission " + LevelId, PlayerLives);
		}
		yield return new WaitUntil(() => transitionScreen.onBannerFadeInFinished);
		#endregion

		#region Initializing and Building Level
		levelManager.ClearMap();
		PrefabManager.ResetPrefabManager();
		yield return new WaitForSeconds(0.15f);
		PrefabManager.Initialize();
		yield return new WaitForSeconds(0.15f);
		levelManager.Initialize();
		yield return new WaitForSeconds(0.15f);
		yield return levelManager.LoadAndBuildMap(CurrentLevel, totalLoadingFadeDuration);
		#endregion

		transitionScreen.FadeOutBanner(bannerFadeDuration);
		yield return new WaitUntil(() => transitionScreen.onBannerFadeOutFinished);

		transitionScreen.FadeOut(loadingScreenFadeDuration);
		yield return new WaitUntil(() => transitionScreen.onFadeOutFinished);
		AsyncOperation loadingScreenUnload = SceneManager.UnloadSceneAsync("LoadingScreen");
		yield return new WaitUntil(() => loadingScreenUnload.isDone);
		switch(CurrentMode) {
			case GameMode.Campaign:
				levelManager.StartGame();
				break;
			case GameMode.LevelOnly:
				levelManager.StartGame();
				break;
			case GameMode.Editor:
				LevelManager.Editor.StartLevelEditor();
				LevelManager.Editor.LoadUserLevel(CurrentLevel);
				break;
		}
	}

	public static void ShowCursor() {
		if (Game.Platform == GamePlatform.Desktop) {
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
	}

	public static void HideCursor() {
		if (Game.Platform == GamePlatform.Desktop) {
			Cursor.lockState = CursorLockMode.Confined;
			Cursor.visible = false;
		}
	}

	// Helpers
	public bool LevelExists(ulong levelId) => Levels.Exists(t => t.levelId == levelId);
	// Return a random ulong between a min and max value.
	public static ulong GetRandomLevelId(ulong min, ulong max) {
		byte[] buffer = new byte[sizeof(ulong)];
		RandomGenerator.NextBytes(buffer);
		return BitConverter.ToUInt64(buffer, 0) % (max - min + 1) + min;
	}
}
