using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using Sperlich.PrefabManager;
using System.Threading.Tasks;

public class GameManager : Singleton<MonoBehaviour> {

	public enum GameMode { None, Campaign, LevelOnly, Editor }

	public const float loadingScreenFadeDuration = 0.3f;
	public const float bannerFadeDuration = 0.3f;
	public const float totalLoadingFadeDuration = 0.3f;

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
	private static int _score;
	public static int Score {
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
	public static CampaignV1.Difficulty Difficulty => CurrentCampaign.difficulty;
	public static GameMode CurrentMode;

	public static LoadingScreen screen;
	public static bool GameBooted = false;
	public static LevelData[] Levels => AssetLoader.LevelAssets;
	private static LevelData _currentLevel;
	public static LevelData CurrentLevel {
		get {
			LevelData level = Levels?.ToList().Find(l => l.levelId == LevelId);
			if(level == null) {
				return _currentLevel;
			}
			return level;
		}
		set => _currentLevel = value;
	}
	private static CampaignV1 CurrentCampaign => GameSaver.GetCampaign(GameSaver.SaveInstance.currentSaveSlot);
	private static UnityAction<Scene, LoadSceneMode> loadingScreenStartedCallback;
	private static UnityAction<Scene, Scene> loadingScreenExitCallback;
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

	public static void CopyCamera() {
		var mainCam = Camera.main;
		var thisCam = GameObject.FindGameObjectWithTag("LoadingScreenCamera").GetComponent<Camera>();

		thisCam.transform.position = mainCam.transform.position;
		thisCam.transform.rotation = mainCam.transform.rotation;
		thisCam.orthographicSize = mainCam.orthographicSize;
		thisCam.nearClipPlane = mainCam.nearClipPlane;
		thisCam.farClipPlane = mainCam.farClipPlane;
	}

	public static void StartLevel(ulong levelId) {
		if(isLoading == false) {
			isLoading = true;
			CurrentMode = GameMode.LevelOnly;
			LevelId = levelId;
			PlayerLives = 0;
			Score = 0;
			PlayTime = 0;
			LoadLevel("Level " + levelId);
		}
	}

	public static void StartEditor(LevelData level) {
		CurrentMode = GameMode.Editor;
		LevelId = level.levelId;
		CurrentLevel = level;
		LoadLevel("Starting Editor");
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

	public void StartCampaign(byte saveSlot) {
		if(!isLoading) {
			isLoading = true;
			Logger.Log(Channel.SaveGame, "Starting campaign on save slot " + saveSlot);
			var camp = GameSaver.GetCampaign(saveSlot);
			CurrentMode = GameMode.Campaign;
			LevelId = camp.levelId;
			PlayTime = camp.time;
			Score = camp.score;
			PlayerLives = camp.lives;
			LoadLevel("", true);
		}
	}

	public static void UpdateCampaign() => GameSaver.UpdateCampaign(LevelId, PlayerLives, Score, PlayTime);

	public static void ReturnToMenu(string message) => TransitionToMenu(message);
	public static void LoadLevel(string message, bool displayCampaignInformation = false) {
		if(AssetLoader.GetOfficialLevel(LevelId) != null) {
			if(SceneManager.GetSceneByName("Level").IsValid() == false && SceneManager.GetSceneByName("Level").isLoaded == false) {
				try {
					TransitionToLevel(message, displayCampaignInformation);
				} catch(Exception ex) {
					Debug.LogError(ex.Message);
				}
			}
		} else {
			ReturnToMenu("Campaign End");
		}
	}
	static async void TransitionToLevel(string message, bool displayCampaignInformation = false) {
		Scene activeScene = SceneManager.GetActiveScene();

		#region Loading LoadingScreen
		// Load LoadingScreen
		await SceneEx.LoadSceneAsync("LoadingScreen");
		while(FindObjectOfType<LoadingScreen>() == null) {
			await Task.Delay(100);
		}
		LoadingScreen transitionScreen = FindObjectOfType<LoadingScreen>();
		await transitionScreen.FadeIn(loadingScreenFadeDuration);
		#endregion

		#region LoadingLevel
		await SceneEx.LoadSceneAsync("Level");

		while(FindObjectOfType<LevelManager>() == null) {
			await Task.Delay(100);
		}
		LevelManager levelManager = FindObjectOfType<LevelManager>();
		#endregion

		#region Unloading Menu/Level
		if(activeScene.name == "Menu") {
			await SceneEx.UnloadSceneAsync("Menu");
		} else if(activeScene.name == "Level") {
			await SceneEx.UnloadSceneAsync("level");
		}
		#endregion

		#region BannerInformation
		if(displayCampaignInformation) {
			if(Difficulty == CampaignV1.Difficulty.Easy) {
				transitionScreen.SetSingleMessage("Mission " + LevelId);
			} else {
				transitionScreen.SetInfo("Mission " + LevelId, PlayerLives);
			}
		} else {
			transitionScreen.SetSingleMessage(message);
		}
		await transitionScreen.FadeInBanner(bannerFadeDuration);
		#endregion

		#region Initializing and Building Level
		PrefabManager.ResetPrefabManager();
		PrefabManager.Initialize("Level");
		GraphicSettings.ApplySettings();
		levelManager.Initialize();
		if(LevelId == 1) {
			LevelManager.Instance.UI.tutorial.gameObject.SetActive(true);
		}
		await levelManager.LoadAndBuildMap(CurrentLevel, totalLoadingFadeDuration);
		#endregion

		await transitionScreen.FadeOutBanner(bannerFadeDuration);
		PrefabManager.DefaultSceneSpawn = "Level";
		isLoading = false;

		_ = levelManager.UI.HideTransitionScreen();
		await transitionScreen.FadeOut(loadingScreenFadeDuration);
		await SceneEx.UnloadSceneAsync("LoadingScreen");

		switch(CurrentMode) {
			case GameMode.Campaign:
				await levelManager.StartGame();
				break;
			case GameMode.LevelOnly:
				await levelManager.StartGame();
				break;
			case GameMode.Editor:
				LevelManager.Editor.StartLevelEditor();
				LevelManager.Editor.LoadUserLevel(CurrentLevel);
				break;
		}
	}
	static async void TransitionToMenu(string message) {
		Scene activeScene = SceneManager.GetActiveScene();

		// Load LoadingScreen
		await SceneEx.LoadSceneAsync("LoadingScreen");

		while(FindObjectOfType<LoadingScreen>() == null) {
			await Task.Delay(100);
		}
		LoadingScreen transitionScreen = FindObjectOfType<LoadingScreen>();
		await transitionScreen.FadeIn(loadingScreenFadeDuration);

		transitionScreen.SetSingleMessage(message);
		await transitionScreen.FadeInBanner(bannerFadeDuration);

		await SceneEx.UnloadSceneAsync("Level");
		await SceneEx.LoadSceneAsync("Menu");

		await transitionScreen.FadeOutBanner(bannerFadeDuration);
		FindObjectOfType<MenuManager>().mainMenu.FadeIn();
		FindObjectOfType<MenuManager>().FadeOutBlur();
		await transitionScreen.FadeOut(loadingScreenFadeDuration);
		await SceneEx.UnloadSceneAsync("LoadingScreen");

		isLoading = false;
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
	public static bool LevelExists(ulong levelId) => AssetLoader.LevelAssets.ToList().Exists(t => t.levelId == levelId);
	// Return a random ulong between a min and max value.
	public static ulong GetRandomLevelId(ulong min, ulong max) {
		byte[] buffer = new byte[sizeof(ulong)];
		RandomGenerator.NextBytes(buffer);
		return BitConverter.ToUInt64(buffer, 0) % (max - min + 1) + min;
	}
}
