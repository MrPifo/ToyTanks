using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(MMFeedbacks))]
public class GameManager : MonoBehaviour {

	public int levelId;
	public int playerLives;
	public int score;
	public bool isLoading;
	MMFeedbackLoadScene loader;
	MMFeedbacks feedbacks;
	UnityAction<Scene, LoadSceneMode> loadingScreenStartedCallback;
	UnityAction<Scene, Scene> loadingScreenExitCallback;
	UnityAction<Scene, LoadSceneMode> levelBaseLoadedCallback;

	void Awake() {
		feedbacks = GetComponent<MMFeedbacks>();
		loader = feedbacks.GetComponent<MMFeedbackLoadScene>();
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

	public void LoadLevel(bool returnToMenu = false) {
		if(!isLoading) {
			isLoading = true;
			if(!returnToMenu && Application.CanStreamedLevelBeLoaded("Level_" + levelId)) {
				OnLoadingScreenEntered(() => LoadLevelBase(() => {
					CopyCamera();
					OnLoadingScreenExit(() => {
						CopyCamera();
						FindObjectOfType<LevelManager>().StartGame();
					});
				}));
				StartTransitionToScene("Level_" + levelId);
			} else {
				OnLoadingScreenExit(() => {
					ResetGameStatus();
				});
				StartTransitionToScene("Menu");
			}
		}
	}
	
	public void ResetGameStatus() {
		LevelManager.playerDeadGameOver = false;
		Destroy(gameObject);
	}

	public void StartCampaign() {
		if(!isLoading) {
			DontDestroyOnLoad(this);
			levelId = 0;
			playerLives = 3;
			LoadLevel();
		}
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
