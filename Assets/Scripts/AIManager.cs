using Sperlich.PrefabManager;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AIManager : MonoBehaviour {

    [SerializeField]
    private bool _showGrid;
    [SerializeField]
    private bool _showTankDebugs;
    [SerializeField]
    private AIGrid pathfindingGrid;
    [SerializeField]
    private PlayerTank registeredPlayer;
    [SerializeField]
    private List<TankAI> registeredAI;
    private List<AttackToken> _attackTokens;
    private static List<AttackToken> attackTokens;

    public static bool showGrid {
        get => Instance._showGrid;
        set => Instance._showGrid = value;
    }
    public static bool showTankDebugs {
        get => Instance._showTankDebugs;
        set => Instance._showTankDebugs = value;
    }
    public static PlayerTank Player {
        get => Instance.registeredPlayer;
        set => Instance.registeredPlayer = value;
    }
    public static List<TankAI> AIs {
        get => Instance.registeredAI;
        set => Instance.registeredAI = value;
    }
    private static AIManager _instance;
    public static AIGrid AIGrid {
        get => Instance.pathfindingGrid;
        set => Instance.pathfindingGrid = value;
    }
    public static AIManager Instance {
        get {
            if(_instance == null) {
                if (FindObjectOfType<AIManager>() != null) {
                    _instance = FindObjectOfType<AIManager>();
                } else {
                    _instance = new GameObject("AI Manager").AddComponent<AIManager>();
                    SceneManager.MoveGameObjectToScene(_instance.gameObject, SceneManager.GetSceneByName(PrefabManager.DefaultSceneSpawn));
                }
            }
            return _instance;
        }
    }
    public const int AttackTokens = 4;

    public static void Initialize() 
        {
        Player = null;
        AIs = new List<TankAI>();
        Instance.enabled = true;
        attackTokens = Enumerable.Repeat(new AttackToken(), AttackTokens).ToList();
        Physics.autoSyncTransforms = false;

        if (AIGrid == null) {
            Debug.LogError("Error: AI-Grid should be initialized before!");
        }
    }
    public static void RegisterPlayer(PlayerTank player) {
        Player = player;
    }
    public static void RegisterAI(TankAI ai) {
        if(AIs.Contains(ai) == false) {
            AIs.Add(ai);
        } else {
            Debug.LogWarning($"Attention: The AI {ai.name} has already been registered!");
        }
    }
    public static void CreateAIGrid(GridSizes size, LayerMask mask) {
        Logger.Log(Channel.System, $"Generating AI Pathfinding Grid ({size})");
        AIGrid = Instance.gameObject.AddComponent<AIGrid>();
        AIGrid.GenerateGrid(size, mask);
    }
    public static bool RequestToken(float borrowTime, out AttackToken token) {
        token = attackTokens.Find(t => t.inUse == false);
        if(token != null) {
            token.borrowTime = borrowTime;
            token.inUse = true;
            return true;
		}
        return false;
	}
    public static void FreeAllTokens() {
        if(attackTokens != null && attackTokens.Count > 0) {
            foreach(var t in attackTokens) {
                t.borrowTime = 0;
                t.inUse = false;
			}
		}
	}

	void Update() {
        if (Game.IsGameCurrentlyPlaying && attackTokens != null && attackTokens.Count > 0) {
            foreach (var t in attackTokens.Where(t => t.inUse)) {
                t.borrowTime -= Time.deltaTime;
                if(t.borrowTime < 0) {
                    t.borrowTime = 0;
                    t.inUse = false;
				}
            }
            _attackTokens = attackTokens;
        }
	}

	[System.Serializable]
    public class AttackToken {
        public string Token { get; }
        public float borrowTime;
        public bool inUse;

        public AttackToken() {
            Token = new System.Guid().ToString();
            borrowTime = 0;
            inUse = false;
		}
	}
}
