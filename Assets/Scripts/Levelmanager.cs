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
	public int maxTracksOnStage = 1000;
	public float gridDensity = 1;
	public Vector2 playgroundSize;
	public Vector3 customBlurSize = new Vector3(1.2f, 1.2f, 1f);
	public LayerMask generationLayer;
	public bool IsGameOver { get; set; }
	public bool isDebug;
	public bool showGrid;
	public bool IsBossLevel { get; set; }
	public static bool ReviveDeadAI { get; set; } = false;
	public static LevelUI UI { get; set; }
	public static LevelFeedbacks Feedback { get; set; }
	Transform trackContainer;
	GameManager gameManager;
	BossAI bossTank;
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
		levelName = gameObject.scene.name;
		IsGameOver = true;
		LevelNumber = int.Parse(levelName.Replace("Level_", ""));
		SceneManager.MoveGameObjectToScene(trackContainer.gameObject, Scene);

		// Ensure debug is set off
		foreach(var t in FindObjectsOfType<TankBase>()) {
			if(t as TankAI) {
				var ai = t as TankAI;
				if(t is BossAI) {
					bossTank = t as BossAI;
					IsBossLevel = true;
				}
			}
		}
		player.DisablePlayer();
		DisableAllAIs();
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
			UI.gameplay.SetActive(true);
			audioManager = FindObjectOfType<AudioManager>();
		}
		IsGameOver = true;
	}

	void Update() {
		if(IsGameOver == false) {
			CheckTankTracks();
			UpdateRunTime();

			if(showGrid) {
				DrawGridLines();
				DrawGridPoints();
			}
		}
	}

	public void UpdateRunTime() {
		if(!isDebug) {
			GameManager.PlayTime += Time.deltaTime;
			UI.playTime.SetText(Mathf.Round(GameManager.PlayTime * 100f)/100f + "s");
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
		} else {
			player.makeInvincible = false;
		}
		
		UI = FindObjectOfType<LevelUI>();
		audioManager = FindObjectOfType<AudioManager>();
		Feedback = FindObjectOfType<LevelFeedbacks>();
		gameManager = FindObjectOfType<GameManager>();
		player.SetupCross();

		UI.EnableBlur();
		UI.gameplay.SetActive(true);
		Feedback.FadeInGameplayUI();
		if(IsBossLevel) {
			bossTank.InitializeAI();
		}

		UI.counterBanner.SetActive(true);
		if(!isDebug) {
			UI.playerScore.SetText(GameManager.Score.ToString());
			UI.playerLives.SetText(GameManager.PlayerLives.ToString());
			UI.levelStage.SetText($"Level {LevelNumber}");
			UI.playTime.SetText(Mathf.Round(GameManager.PlayTime * 100f) / 100f + "s");
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Confined;
		}
		MoveLevelBase();
		yield return new WaitForSeconds(2.5f);
		Feedback.PlayStartFadeText();
		yield return new WaitForSeconds(1);
		IsGameOver = false;
		UI.counterBanner.SetActive(false);
		player.EnablePlayer();
		EnableAllAIs();

		foreach(var ai in tankAIs) {
			ai.InitializeAI();
		}
	}

	public void TankDestroyedCheck() {
		AddScore();
		foreach(TankAI t in tankAIs) {
			if(t.HasBeenDestroyed == false) {
				// All TankAIs must be destroyed or else returns
				return;
			}
		}
		GameOver();
	}

	void AddScore() {
		if(!isDebug) {
			GameManager.Score++;
			UI.playerScore.SetText(GameManager.Score.ToString());
			Feedback.PlayScore();
		} else {
			UI.playerScore.SetText(Random.Range(0, 9).ToString());
			Feedback.PlayScore();
		}
	}

	public void PlayerDead() {
		player.DisablePlayer();
		DisableAllAIs();

		if(!isDebug) {
			GameManager.PlayerLives--;
			UI.playerLives.SetText(GameManager.PlayerLives.ToString());
			if(GameManager.PlayerLives <= 0) {
				GameOver();
			} else {
				IsGameOver = true;
				GameManager.UpdateCampaign();
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
			if(t.HasBeenDestroyed == false) {
				t.Revive();
			} else if(ReviveDeadAI) {
				t.Revive();
			}
		}
		yield return new WaitForSeconds(1f);
		StartGame();
	}

	void GameOver() {
		if(!isDebug && IsGameOver == false) {
			if(GameManager.CurrentMode == GameManager.GameMode.Campaign) {
				GameManager.LevelId++;
			}

			IsGameOver = true;
			GameManager.UpdateCampaign();
			player.DisablePlayer();
			DisableAllAIs();
			StartCoroutine(IGameOver());
		}
	}
	IEnumerator IGameOver() {
		yield return new WaitForSeconds(2f);
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Confined;
		Feedback.FadeOutGameplayUI();
		if(GameManager.CurrentMode == GameManager.GameMode.Campaign && GameManager.PlayerLives > 0) {
			gameManager.LoadLevel();
		} else {
			gameManager.LoadLevel(true);
		}
	}

	void CheckTankTracks() {
		if(trackContainer.childCount > maxTracksOnStage) {
			Destroy(trackContainer.GetChild(0).gameObject);
		}
	}

	public void DisableAllAIs() {
		foreach(TankAI t in tankAIs) {
			t.DisableAI();
			t.makeInvincible = true;
		}
	}

	public void EnableAllAIs() {
		foreach(TankAI t in tankAIs) {
			t.EnableAI();
			t.makeInvincible = false;
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

	void OnDrawGizmos() {
		if(showGrid) {
			DrawGridLines();
			DrawGridPoints();
		}
	}

	public void DrawGridLines() {
		if(grid != null) {
			foreach(Node n in grid.Nodes) {
				foreach(KeyValuePair<Node, float> neigh in n.Neighbours) {
					Draw.Line(n.pos, neigh.Key.pos, Color.white, true);
				}
			}
		}
	}

	public void DrawGridPoints() {
		if(grid != null) {
			foreach(Node n in grid.Nodes) {
				if(n.type == Node.NodeType.ground) {
					Draw.Sphere(n.pos, 0.2f, Color.white, true);
				} else {
					Draw.Sphere(n.pos, 0.2f, Color.red, true);
				}
			}
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
		if(GUILayout.Button("Reset")) {
			builder.Respawn();
		}
		if(GUILayout.Button("Generate Grid")) {
			builder.GenerateGrid();
		}
	}
}
#endif