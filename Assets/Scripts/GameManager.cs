using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {

	public LevelManager currentLevel;
	public Scene? currentScene;
	public Scene gameManagerScene;
	public int levelNumber;
	public bool IsPlayerInvincible { get; set; }
	UnityAction<Scene> menuUnloadAction;
	UnityAction<Scene> levelUnloadAction;
	UnityAction<Scene, LoadSceneMode> levelLoadAction;

	public void StartCampaign() {
		gameManagerScene = SceneManager.GetActiveScene();
		levelNumber = 0;
		currentScene = null;

		menuUnloadAction = (Scene s) => {
			gameManagerScene = SceneManager.GetActiveScene();
			SceneManager.sceneUnloaded -= menuUnloadAction;
			LoadAndUnloadLevel();
		};
		SceneManager.sceneUnloaded += menuUnloadAction;
		SceneManager.UnloadSceneAsync("Menu");
	}

	void StartLevel() {
		currentLevel = FindObjectOfType<LevelManager>();
		currentLevel.OnLevelEnd.AddListener(LoadAndUnloadLevel);
		currentLevel.StartGame();
	}

	void LoadAndUnloadLevel() {
		if(currentScene != null) {
			SceneManager.UnloadSceneAsync((Scene)currentScene);
			Debug.Log("Unloading Current Level");
			levelUnloadAction = (Scene scene) => {
				SceneManager.sceneUnloaded -= levelUnloadAction;
				currentScene = null;
				levelNumber++;
				LoadLevel();
			};
			SceneManager.sceneUnloaded += levelUnloadAction;
		} else {
			LoadLevel();
		}
	}

	void LoadLevel() {
		if(Application.CanStreamedLevelBeLoaded($"Level_{levelNumber}")) {
			levelLoadAction = (Scene scene, LoadSceneMode mode) => {
				Debug.Log("<color=red>Next Level has been loaded!</color>");
				SceneManager.sceneLoaded -= levelLoadAction;
				SceneManager.SetActiveScene(scene);
				currentScene = scene;
				StartLevel();
			};
			SceneManager.sceneLoaded += levelLoadAction;
			SceneManager.LoadScene($"Level_{levelNumber}", LoadSceneMode.Additive);
		} else {
			EndLevelCompleted();
		}
	}

	void EndLevelCompleted() {
		SceneManager.LoadScene("Menu");
	}
}
