using System.Collections.Generic;
using UnityEngine;
using CarterGames.Assets.AudioManager;
using UnityEngine.Events;
using SimpleMan.Extensions;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour {

	public int prepareTime = 5;
	public int maxTracksOnStage = 100;
	public LevelUI UI { get; set; }
	Transform trackContainer;
	int levelNumber = 0;
	string levelName;
	public static bool isDebug;
	[HideInInspector] public PlayerInput player;
	[HideInInspector] public static AudioManager audioManager;
	[HideInInspector] public TankAI[] tankAIs;
	[HideInInspector] public UnityEvent OnLevelEnd;
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

	private void Start() {
		if(FindObjectOfType<GameManager>() == null) {
			isDebug = true;
			UnityAction<Scene, LoadSceneMode> debugLoad = (Scene scene, LoadSceneMode mode) => {
				Debug.Log("Starting Debug game");

				player.FindCrosshair();
				UI = FindObjectOfType<LevelUI>();
				audioManager = FindObjectOfType<AudioManager>();
				StartGame();
			};
			SceneManager.sceneLoaded += debugLoad;
			SceneManager.LoadScene("LevelBase", LoadSceneMode.Additive);

		} else {
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
		CheckTankTracks();

		Debug.DrawRay(Vector3.one, Vector3.one, Color.red);
		//Draw.Ray(Vector3.one, Vector3.one, Color.red);
	}

	public void StartGame() {
		if(isDebug) {
			SceneManager.sceneLoaded -= debugLoad;
		}
		int prepareTime = this.prepareTime;
		UI = FindObjectOfType<LevelUI>();
		player.FindCrosshair();
		UI.tankStartCounter.SetText(tankAIs.Length.ToString());
		this.RepeatForever(() => {
			switch(prepareTime) {
				case -1:
					UI.counterBanner.SetActive(false);
					player.disableCrosshair = false;
					player.disableControl = false;
					foreach(TankAI tank in tankAIs) {
						tank.IsAIEnabled = true;
					}
					break;
				case 0:
					UI.startCounter.SetText("Start");
					break;
				case 1:
					UI.startCounter.SetText("1");
					break;
				case 2:
					UI.startCounter.SetText("2");
					break;
				case 3:
					UI.startCounter.SetText("3");
					break;
				case 5:
					UI.counterBanner.SetActive(true);
					UI.startCounter.SetText($"Level {levelNumber}");
					break;
			}
			prepareTime--;
		}, 1f);
		
	}

	public void EndGame() {

	}

	public void NextGame() {
		OnLevelEnd.Invoke();
	}

	public void TankDestroyedCheck() {
		foreach(TankAI t in tankAIs) {
			if(t.HasBeenDestroyed == false) {
				// All TankAIs must be destroyed or else returns
				return;
			}
		}
		GameOverWin();
	}

	void GameOverWin() {
		OnLevelEnd.Invoke();
	}

	void CheckTankTracks() {
		if(trackContainer.childCount > maxTracksOnStage) {
			Destroy(trackContainer.GetChild(0).gameObject);
		}
	}
}
