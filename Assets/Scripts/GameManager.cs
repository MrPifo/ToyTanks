using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(MMFeedbacks))]
public class GameManager : MonoBehaviour {

	public enum GameMode { Campaign, LevelOnly}

	[SerializeField] int startLevel;
	[SerializeField] Texture2D defaultCursor;
	[SerializeField] Texture2D pointerCursor;
	public static int LevelId { get; set; } = 1;
	public static byte PlayerLives { get; set; } = 3;
	public static short Score { get; set; } = 0;
	public static float PlayTime { get; set; } = 0;
	public static bool isLoading;
	public static GameMode CurrentMode { get; set; }
	public static GameManager Instance { get; set; }
	public static MMFeedbackLoadScene loader;
	public static MMFeedbacks feedbacks;
	public static LoadingScreen screen;
	UnityAction<Scene, LoadSceneMode> loadingScreenStartedCallback;
	UnityAction<Scene, Scene> loadingScreenExitCallback;
	UnityAction<Scene, LoadSceneMode> levelBaseLoadedCallback;

	void Awake() {
		DontDestroyOnLoad(this);
		Game.AddCursor("default", defaultCursor);
		Game.AddCursor("pointer", pointerCursor);
		Instance = this;
		feedbacks = GetComponent<MMFeedbacks>();
		loader = feedbacks.GetComponent<MMFeedbackLoadScene>();
		SaveGame.GameStartUp();
		Game.SetCursor("default");
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

	public static void StartLevel(int levelId) {
		CurrentMode = GameMode.LevelOnly;
		PlayerLives = 0;
		GameManager.LevelId = levelId;
		Instance.LoadLevel();
	}

	public void LoadLevel(bool returnToMenu = false) {
		if(!isLoading) {
			isLoading = true;
			if(!returnToMenu && Application.CanStreamedLevelBeLoaded("Level_" + LevelId)) {
				OnLoadingScreenEntered(() => {
					FindObjectOfType<MMAdditiveSceneLoadingManager>().OnAfterEntryFade.AddListener(() => {
						screen = FindObjectOfType<LoadingScreen>();
						screen.SetInfo($"Level {LevelId}", $"{PlayerLives} x");
					});
				});
				OnLoadingScreenEntered(() => LoadLevelBase(() => {
					CopyCamera();
					var level = FindObjectOfType<LevelManager>();
					
					OnLoadingScreenExit(() => {
						CopyCamera();
						FindObjectOfType<LevelManager>().StartGame();
					});
				}));
				StartTransitionToScene("Level_" + LevelId);
			} else {
				OnLoadingScreenEntered(() => {
					FindObjectOfType<MMAdditiveSceneLoadingManager>().OnAfterEntryFade.AddListener(() => {
						screen = FindObjectOfType<LoadingScreen>();
						screen.SetInfo($"Returning to Menu", $"");
					});
				});
				OnLoadingScreenExit(() => {
					ResetGameStatus();
				});
				StartTransitionToScene("Menu");
			}
		}
	}
	
	public void ResetGameStatus() {
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.Confined;
		Destroy(gameObject);
	}

	public void StartCampaign() {
		if(!isLoading) {
			var camp = SaveGame.CurrentCampaign;
			CurrentMode = GameMode.Campaign;
			LevelId = camp.levelId;
			PlayTime = camp.time;
			Score = camp.score;
			PlayerLives = camp.lives;
			LoadLevel();
		}
	}

	public static void UpdateCampaign() {
		SaveGame.UpdateCampaign(LevelId, PlayerLives, Score, PlayTime);
	}

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

	public void LoadLevelBase(UnityAction onFinished) {
		levelBaseLoadedCallback = (Scene scene, LoadSceneMode mode) => {
			if(scene.name == "LevelBase") {
				SceneManager.sceneLoaded -= levelBaseLoadedCallback;
				onFinished.Invoke();
			}
		};
		SceneManager.sceneLoaded += levelBaseLoadedCallback;
		SceneManager.LoadSceneAsync("LevelBase", LoadSceneMode.Additive);
	}

	public void StartTransitionToScene(string scene) {
		SetSceneLoadName(scene);
		feedbacks.PlayFeedbacks();
	}

	void SetSceneLoadName(string scene) => loader.DestinationSceneName = scene;
}
