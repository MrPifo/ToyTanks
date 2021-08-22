using UnityEngine;
using CarterGames.Assets.AudioManager;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
			isDebug = true;
			if(!FindObjectOfType<LevelUI>()) {
				UnityAction<Scene, LoadSceneMode> debugLoad = (Scene scene, LoadSceneMode mode) => {
					StartGame();
				};
				SceneManager.sceneLoaded += debugLoad;
				SceneManager.LoadScene("LevelBase", LoadSceneMode.Additive);
			} else {
				StartGame();
			}
		} else {
			isDebug = false;
			UI = FindObjectOfType<LevelUI>();
			audioManager = FindObjectOfType<AudioManager>();
			UI.gameplay.SetActive(true);

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
		audioManager = FindObjectOfType<AudioManager>();
		Feedback = FindObjectOfType<LevelFeedbacks>();
		gameManager = FindObjectOfType<GameManager>();
		player.FindCrosshair();

		UI.EnableBlur();
		UI.gameplay.SetActive(true);
		UI.tankStartCounter.SetText(tankAIs.Length.ToString());

		UI.counterBanner.SetActive(true);
		UI.startCounter.SetText($"Level {levelNumber}");
		if(!isDebug) {
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Confined;

			UI.playerScore.SetText(gameManager.score.ToString());
			UI.playerLives.SetText(gameManager.playerLives.ToString());
			UI.levelStage.SetText($"Level {levelNumber}");
		}
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
			PlayerDead();
		} else {
			AddScore();
			foreach(TankAI t in tankAIs) {
				if(t.HasBeenDestroyed == false) {
					// All TankAIs must be destroyed or else returns
					return;
				}
			}
			GameOverWin();
		}
	}

	public void AddScore() {
		if(!isDebug) {
			gameManager.score++;
			UI.playerScore.SetText(gameManager.score.ToString());
			Feedback.PlayScore();
		} else {
			UI.playerScore.SetText(Random.Range(0, 9).ToString());
			Feedback.PlayScore();
		}
	}

	void PlayerDead() {
		playerDeadGameOver = true;
		player.disableCrosshair = true;
		player.disableControl = true;

		if(!isDebug) {
			gameManager.playerLives--;
			UI.playerLives.SetText(gameManager.playerLives.ToString());
			if(gameManager.playerLives <= 0) {
				GameOverWin();
			} else {
				Respawn();
			}
		}
	}

	public void Respawn() => StartCoroutine(IRespawnAnimate());
	IEnumerator IRespawnAnimate() {
		Feedback.PlayLives();
		yield return new WaitForSeconds(4f);
		player.Revive();
		
		foreach(TankAI t in FindObjectsOfType<TankAI>()) {
			t.enabled = true;
			t.IsAIEnabled = false;
			t.Revive();
		}
		yield return new WaitForSeconds(1f);
		playerDeadGameOver = false;
		StartGame();
	}

	void GameOverWin() {
		foreach(TankBase t in FindObjectsOfType<TankBase>()) {
			t.enabled = false;
		}
		EndGame();
	}

	void CheckTankTracks() {
		if(trackContainer.childCount > maxTracksOnStage) {
			Destroy(trackContainer.GetChild(0).gameObject);
		}
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(LevelManager))]
public class LevelManagerEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		LevelManager builder = (LevelManager)target;
		if(GUILayout.Button("Add Score")) {
			builder.AddScore();
		}
		if(GUILayout.Button("Reset")) {
			builder.Respawn();
		}
	}
}
#endif