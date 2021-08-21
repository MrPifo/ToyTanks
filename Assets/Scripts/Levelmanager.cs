using System.Collections.Generic;
using UnityEngine;
using CarterGames.Assets.AudioManager;
using UnityEngine.Events;
using SimpleMan.Extensions;
using UnityEngine.SceneManagement;
using MoreMountains.Feedbacks;
using System.Collections;

public class LevelManager : MonoBehaviour {

	public int maxTracksOnStage = 100;
	public bool isDebug;
	public static bool playerDeadGameOver;
	public LevelUI UI { get; set; }
	public static LevelFeedbacks Feedback { get; set; }
	Transform trackContainer;
	GameManager gameManager;
	int levelNumber = 0;
	string levelName;
	bool gameEnded;
	[HideInInspector] public PlayerInput player;
	[HideInInspector] public static AudioManager audioManager;
	[HideInInspector] public TankAI[] tankAIs;
	public Scene Scene => gameObject.scene;
	UnityAction<Scene, LoadSceneMode> debugLoad;

	void Awake() {
		// Must be called before TankBase Script		
		tankAIs = FindObjectsOfType<TankAI>();
		player = FindObjectOfType<PlayerInput>();
		trackContainer = new GameObject("TrackContainer").transform;
		player.disableControl = true;
		player.disableCrosshair = true;
		levelName = gameObject.scene.name;
		levelNumber = int.Parse(levelName.Replace("Level_", ""));
		SceneManager.MoveGameObjectToScene(trackContainer.gameObject, Scene);

		foreach(TankAI tank in tankAIs) {
			tank.IsAIEnabled = false;
		}
	}

	void Start() {
		if(!FindObjectOfType<GameManager>()) {
			UnityAction<Scene, LoadSceneMode> debugLoad = (Scene scene, LoadSceneMode mode) => {
				player.FindCrosshair();
				UI = FindObjectOfType<LevelUI>();
				audioManager = FindObjectOfType<AudioManager>();
				isDebug = true;
				StartGame();
			};
			SceneManager.sceneLoaded += debugLoad;
			SceneManager.LoadScene("LevelBase", LoadSceneMode.Additive);

		} else {
			isDebug = false;
			UI = FindObjectOfType<LevelUI>();
			audioManager = FindObjectOfType<AudioManager>();

			// Ensure debug is set off
			foreach(var t in FindObjectsOfType<TankBase>()) {
				t.makeInvincible = false;
				if(t as TankAI) {
					var ai = t as TankAI;
					ai.showDebug = false;
				}
			}
		}
	}

	void Update() {
		if(!playerDeadGameOver) {
			CheckTankTracks();

			Debug.DrawRay(Vector3.one, Vector3.one, Color.red);
		}
	}

	public void MoveLevelBase() {
		var blur = GameObject.Find("Blur");
		SceneManager.MoveGameObjectToScene(blur, gameObject.scene);
		blur.GetComponent<Canvas>().worldCamera = Camera.current;

		SceneManager.MoveGameObjectToScene(UI.gameObject, gameObject.scene);
		UI.canvas.worldCamera = Camera.main;
	}

	public void StartGame() => StartCoroutine(nameof(IStartGame));
	IEnumerator IStartGame() {
		if(isDebug) {
			SceneManager.sceneLoaded -= debugLoad;
		}
		
		UI = FindObjectOfType<LevelUI>();
		Feedback = FindObjectOfType<LevelFeedbacks>();
		gameManager = FindObjectOfType<GameManager>();
		player.FindCrosshair();

		UI.EnableBlur();
		UI.tankStartCounter.SetText(tankAIs.Length.ToString());
		if(!isDebug) {
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Confined;
		}

		UI.counterBanner.SetActive(true);
		UI.startCounter.SetText($"Level {levelNumber}");
		MoveLevelBase();
		yield return new WaitForSeconds(0.75f);
		UI.startCounter.SetText("2");
		yield return new WaitForSeconds(0.75f);
		UI.startCounter.SetText("1");
		yield return new WaitForSeconds(0.75f);
		UI.startCounter.SetText("Start");
		yield return new WaitForSeconds(1);
		UI.counterBanner.SetActive(false);
		player.disableCrosshair = false;
		player.disableControl = false;
		foreach(TankAI tank in tankAIs) {
			tank.IsAIEnabled = true;
		}
	}

	public void EndGame() {
		if(!isDebug && !gameEnded) {
			gameManager.levelId++;
			gameEnded = true;
			player.makeInvincible = true;
			StartCoroutine(IEndGame());
		}
	}
	IEnumerator IEndGame() {
		yield return new WaitForSeconds(2f);
		gameManager.LoadLevel(playerDeadGameOver);
	}

	public void TankDestroyedCheck() {
		if(player.HasBeenDestroyed) {
			GameOverWin();
		} else {
			foreach(TankAI t in tankAIs) {
				if(t.HasBeenDestroyed == false) {
					// All TankAIs must be destroyed or else returns
					return;
				}
			}
			GameOverWin();
		}
	}

	void GameOverWin() {
		foreach(TankBase t in FindObjectsOfType<TankBase>()) {
			t.enabled = false;
		}
		playerDeadGameOver = player.HasBeenDestroyed;
		EndGame();
	}

	void CheckTankTracks() {
		if(trackContainer.childCount > maxTracksOnStage) {
			Destroy(trackContainer.GetChild(0).gameObject);
		}
	}
}
