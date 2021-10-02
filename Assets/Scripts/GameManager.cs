using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Collections;

public class GameManager : MonoBehaviour {

	public enum GameMode { None, Campaign, LevelOnly, Editor }

	public float loadingScreenFadeDuration = 2f;
	public float bannerFadeDuration = 1f;
	public float totalLoadingFadeDuration = 2f;
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

	void Awake() {
		DontDestroyOnLoad(this);
		var menu = FindObjectOfType<ToyTanks.UI.MenuManager>();
		menu.Initialize();
		GraphicSettings.Initialize();
		Instance = this;

		if(GameBooted == false) {
			Game.AddCursor("default", defaultCursor);
			Game.AddCursor("pointer", pointerCursor);
			SaveGame.GameStartUp();
			Game.SetCursor("default");
			menu.mainMenu.FadeIn();
			menu.FadeOutBlur();
			GameBooted = true;
		}
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
		Instance.LoadLevel("Level " + levelId);
	}

	public static void StartEditor(LevelData level) {
		CurrentMode = GameMode.Editor;
		LevelId = level.levelId;
		CurrentLevel = level;
		Instance.LoadLevel("Starting Editor");
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
			LoadLevel("", true);
		}
	}

	public static void UpdateCampaign() => SaveGame.UpdateCampaign(LevelId, PlayerLives, Score, PlayTime);

	public void ReturnToMenu(string message) => StartCoroutine(TransitionToMenu(message));
	public void LoadLevel(string message, bool displayCampaignInformation = false) => StartCoroutine(TransitionToLevel(message, displayCampaignInformation));
	IEnumerator TransitionToLevel(string message, bool displayCampaignInformation = false) {
		Scene activeScene = SceneManager.GetActiveScene();

		// Load LoadingScreen
		AsyncOperation loadingScreen = SceneManager.LoadSceneAsync("LoadingScreen", LoadSceneMode.Additive);
		loadingScreen.allowSceneActivation = true;

		yield return new WaitUntil(() => loadingScreen.isDone);
		LoadingScreen transitionScreen = FindObjectOfType<LoadingScreen>();
		transitionScreen.FadeIn(loadingScreenFadeDuration);

		yield return new WaitUntil(() => transitionScreen.onFadeInFinished);
		if(activeScene.name == "Menu") {
			AsyncOperation unloadLevel = SceneManager.UnloadSceneAsync("Menu");
			unloadLevel.allowSceneActivation = true;
			yield return new WaitUntil(() => unloadLevel.isDone);
		}
		if(activeScene.name == "Level") {
			AsyncOperation unloadLevel = SceneManager.UnloadSceneAsync("Level");
			yield return new WaitUntil(() => unloadLevel.isDone);
		}
		AsyncOperation level = SceneManager.LoadSceneAsync("Level", LoadSceneMode.Additive);
		level.allowSceneActivation = true;
		yield return new WaitUntil(() => level.isDone);
		LevelManager levelManager = FindObjectOfType<LevelManager>();
		levelManager.Initialize();
		yield return new WaitForSeconds(1f);

		transitionScreen.FadeInBanner(bannerFadeDuration);
		if(displayCampaignInformation) {
			if(Difficulty == SaveGame.Campaign.Difficulty.Easy) {
				transitionScreen.SetSingleMessage("Mission " + LevelId);
			} else {
				transitionScreen.SetInfo("Mission " + LevelId, PlayerLives.ToString());
			}
		} else {
			transitionScreen.SetSingleMessage(message);
		}
		yield return new WaitUntil(() => transitionScreen.onBannerFadeInFinished);
		StartCoroutine(levelManager.LoadAndBuildMap(CurrentLevel, totalLoadingFadeDuration));
		yield return new WaitUntil(() => levelManager.HasLevelBeenBuild);

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
		FindObjectOfType<ToyTanks.UI.MenuManager>().mainMenu.FadeIn();
		FindObjectOfType<ToyTanks.UI.MenuManager>().FadeOutBlur();
		yield return new WaitUntil(() => transitionScreen.onFadeOutFinished);
		AsyncOperation loadingScreenUnload = SceneManager.UnloadSceneAsync("LoadingScreen");
		yield return new WaitUntil(() => loadingScreenUnload.isDone);
		Destroy(gameObject);
	}

	public static void ShowCursor() {
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
	}

	public static void HideCursor() {
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Confined;
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
