using UnityEngine;
using CarterGames.Assets.AudioManager;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using Sperlich.Pathfinding;
using Sperlich.Types;
using Sperlich.Debug.Draw;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelManager : MonoBehaviour {

	public PathfindingMesh grid;
	public int maxTracksOnStage = 100;
	public float gridDensity = 1;
	public Vector2 playgroundSize;
	public Vector3 customBlurSize = new Vector3(1.2f, 1.2f, 1f);
	public LayerMask generationLayer;
	public bool gameEnded;
	public bool isDebug;
	public bool showGrid;
	public static bool playerDeadGameOver;
	public LevelUI UI { get; set; }
	public static LevelFeedbacks Feedback { get; set; }
	Transform trackContainer;
	GameManager gameManager;
	int levelId;
	int LevelNumber {
		get => levelId + 1;
		set => levelId = value;
	}
	string levelName;
	
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
		LevelNumber = int.Parse(levelName.Replace("Level_", ""));
		LevelManager.playerDeadGameOver = false;
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
		}
	}

	public void MoveLevelBase() {
		var blur = GameObject.Find("Blur");
		SceneManager.MoveGameObjectToScene(blur, gameObject.scene);
		blur.GetComponent<Canvas>().worldCamera = Camera.current;
		blur.GetComponentInChildren<UnityEngine.UI.Image>().transform.localScale = customBlurSize;

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
		UI.startCounter.SetText($"Level {LevelNumber}");
		if(!isDebug) {
			UI.playerScore.SetText(gameManager.score.ToString());
			UI.playerLives.SetText(gameManager.playerLives.ToString());
			UI.levelStage.SetText($"Level {LevelNumber}");
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Confined;
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
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Confined;
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

#if UNITY_EDITOR
	public void GenerateGrid() {
		grid.ClearGrid();
		StopAllCoroutines();

		for(float x = -playgroundSize.x; x <= playgroundSize.x; x++) {
			for(float z = -playgroundSize.y; z <= playgroundSize.y; z++) {
				var ray = new Ray(new Vector3(x * gridDensity, transform.position.y + 20, z * gridDensity) + transform.position, Vector3.down);
				float dist = grid.painter.paintRadius;
				if(grid.enableCrossConnections) {
					dist *= grid.crossDistance;
				}
				if(Physics.SphereCast(ray.origin, 0.25f, ray.direction, out RaycastHit hit, Mathf.Infinity, generationLayer)) {
					if(hit.transform.CompareTag("Ground")) {
						grid.AddNode(new Float3(hit.point), dist, Node.NodeType.ground);
					}
				}
			}
		}
		grid.gridName = gameObject.scene.name;
		grid.Reload();
		grid.SaveGrid();
	}

	void OnDrawGizmosSelected() {
		if(showGrid) {
			DrawGridLines();
			//DrawGridPoints();
		}
	}

	public void DrawGridLines() {
		foreach(Node n in grid.Nodes) {
			foreach(KeyValuePair<Node, float> neigh in n.Neighbours) {
				Draw.Line(n.pos, neigh.Key.pos, Color.white);
			}
		}
	}

	public void DrawGridPoints() {
		foreach(Node n in grid.Nodes) {
			Draw.Disc(n.pos, Vector3.up);
		}
	}
#endif
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
		if(GUILayout.Button("Generate Grid")) {
			builder.GenerateGrid();
		}
	}
}
#endif