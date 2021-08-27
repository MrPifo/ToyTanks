using Sperlich.Pathfinding;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sperlich.Debug.Draw;
using Sperlich.FSM;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class TankAI : TankBase {

	public enum TankState { Idle, Defense, Attack, Patrol }

	[HideInInspector]
	public TankBase player;
	[HideInInspector]
	public LevelManager level;
	public LayerMask levelPredictionMask;
	public float minAlwaysShootDistance;
	public float shootRadius;
	public float playerDetectRadius;
	public float playerLoseRadius;
	public float pathNodeReachTreshold;
	public int pathUpdateInvervall = 1;
	public bool showDebug;
	public bool disableAI;
	public Node activePatrolPoint;
	public FSM<TankState> stateMachine;
	protected float distToPlayer;
	protected PathfindingMesh pathMesh;
	protected List<Node> currentPath;
	private bool isAIEnabled;
	public bool IsAIEnabled {
		set => isAIEnabled = value;
		get => isAIEnabled && !disableAI;
	}
	public bool IsPlayerMinShootRange => distToPlayer < minAlwaysShootDistance;
	public bool IsPlayerInDetectRadius => distToPlayer < playerDetectRadius;
	public bool IsPlayerOutsideLoseRadius => distToPlayer > playerLoseRadius;
	public bool IsPlayerInShootRadius => distToPlayer < shootRadius;
	public bool HasActivePatrolPoint => activePatrolPoint != null;
	public bool HasSightContactToPlayer => HasSightContact(player);
	public int PathNodeCount => currentPath == null ? 0 : currentPath.Count;

	protected override void Awake() {
		base.Awake();
		stateMachine = new FSM<TankState>();
		stateMachine.Push(TankState.Idle);
		pathMesh = FindObjectOfType<PathfindingMesh>();
		player = FindObjectOfType<PlayerInput>().GetComponent<TankBase>();
		level = FindObjectOfType<LevelManager>();
	}

	void Update() {
		if(IsAIEnabled) {
			distToPlayer = Vector3.Distance(Pos, player.Pos);
			ProcessState();
		}
	}

	protected override void LateUpdate() {
		base.LateUpdate();
		if(showDebug) {
			DrawDebug();
		}
	}

	public virtual void DrawDebug() {
		if(IsPlayerInDetectRadius) {
			Draw.Ring(Pos, Vector3.up, playerDetectRadius, 1f, Color.black, Color.black, true);
			if(IsPlayerInShootRadius) {
				if(HasSightContact(player)) {
					Draw.Ring(Pos, Vector3.up, distToPlayer, 1f, Color.white, Color.red, true);
				} else {
					Draw.Ring(Pos, Vector3.up, distToPlayer, 1f, new Color32(150, 20, 20, 255), true);
				}
				if(distToPlayer < minAlwaysShootDistance) {
					Draw.Ring(Pos, Vector3.up, minAlwaysShootDistance, 0.2f, Color.blue, false);
				}
			}
			if(!(IsPlayerInShootRadius && HasSightContact(player))) {
				pathMesh.DrawPathLines(currentPath);
			}
		}
		Draw.Text(Pos + Vector3.up, stateMachine.Text);
	}

	void ProcessState() {
		switch(stateMachine.State) {
			case TankState.Idle:
				Idle();
				break;
			case TankState.Defense:
				Defense();
				break;
			case TankState.Attack:
				Attack();
				break;
			case TankState.Patrol:
				Patrol();
				break;
		}
	}

	public override void Revive() {
		base.Revive();
		stateMachine.Push(TankState.Idle);
	}

	public abstract void Idle();
	public abstract void Attack();
	public abstract void Defense();
	public abstract void Patrol();

	public virtual void Aim() {
		MoveHead(player.Pos);
	}

	public override void GotHitByBullet() {
		base.GotHitByBullet();
		if(!makeInvincible && healthPoints <= 0) {
			healthBar.gameObject.SetActive(false);
			enabled = false;
		} else {
			healthBar.transform.parent.gameObject.SetActive(true);
		}
	}

	public void MoveAlongPath() {
		if(PathNodeCount > 0) {
			var dir = GetLookDirection(currentPath[0].pos);
			var moveDir = new Vector2(dir.x, dir.z);
			Move(moveDir);
		}
	}

	public void FaceDirection(Vector3 target) {
		target = new Vector3(target.x, Pos.y, target.z);
		AdjustRotation((target - Pos).normalized);
	}

	public void AimOnSight() {
		if(HasSightContact(player)) {
			MoveHead(player.Pos);
		}
	}

	public bool IsAimingOnTarget(Transform target) => IsAimingOnTarget(target.position);
	public bool IsAimingOnTarget(Vector3 target, float precision = 0f) {
		precision = precision == 0 ? 0.999f : precision;
		Vector3 dirFromAtoB = (tankHead.position - target).normalized;
		float dotProd = Mathf.Abs(Vector3.Dot(dirFromAtoB, tankHead.forward));
		if(dotProd > precision) {
			return true;
		}
		return false;
	}

	public bool PotentialFriendlyFire(float precision = 0f) {
		precision = precision == 0 ? 0.999f : precision;

		var hitList = PredictBulletPath();
		if(hitList.Any(t => t.CompareTag("Bot"))) {
			return true;
		}
		return false;
	}

	public List<Transform> PredictBulletPath() {
		RaycastHit lastHit = new RaycastHit();
		Vector3 lastPos = bulletOutput.position;
		Ray ray = new Ray(bulletOutput.position, tankHead.forward);
		var hitList = new List<Transform>();
		for(int i = 0; i < bullet.maxBounces + 1; i++) {
			if(i > 0) {
				ray = new Ray(lastHit.point, Vector3.Reflect(ray.direction, lastHit.normal));
			}
			//Physics.Raycast(ray, out lastHit, Mathf.Infinity, levelPredictionMask);
			Physics.BoxCast(ray.origin, new Vector3(0.5f, 0.5f, 0.5f), ray.direction, out lastHit, Quaternion.identity, Mathf.Infinity, levelPredictionMask);
			if(showDebug) Draw.Line(ray.origin, lastHit.point, Color.yellow);
			hitList.Add(lastHit.transform);
		}
		return hitList;
	}

	public bool HasSightContact(TankBase tank) => HasSightContact(tank.Pos);
	public bool HasSightContact(Vector3 target) {
		Ray ray = new Ray(Pos, (target - Pos).normalized);
		
		if(Physics.BoxCast(ray.origin, Bullet.bulletSize, ray.direction, out RaycastHit hit, Quaternion.identity, Mathf.Infinity, hitLayers)) {
			if(hit.transform.CompareTag("Player")) {
				if(showDebug) {
					Draw.Line(ray.origin, hit.point, Color.green);
				}
				return true;
			}
		}
		if(showDebug) Draw.Ray(ray.origin, ray.direction, Color.red);
		return false;
	}

	public Node GetRandomPointOnMap(Vector3 origin, float radius) {
		var nearestPoints = pathMesh.GetNodesWithinRadius(origin, radius);
		Node point = nearestPoints[Random.Range(0, nearestPoints.Count - 1)];
		return point;
	}

	public void RefreshRandomPath(Vector3 origin, float radius, int nodeTreshold = 2) {
		if(HasActivePatrolPoint == false || PathNodeCount <= nodeTreshold) {
			activePatrolPoint = GetRandomPointOnMap(origin, radius);
		}
		FetchPathToPoint(activePatrolPoint);
	}

	public Node GetFurthestPointFrom(Vector3 origin, Vector3 from, float radius) {
		var points = pathMesh.GetNodesWithinRadius(origin, radius);
		float max = 0;
		Node maxNode = null;
		foreach(Node n in points) {
			float dist = Vector3.Distance(from, n.pos);
			if(dist > max) {
				max = dist;
				maxNode = n;
			}
		}
		return maxNode;
	}

	protected void FetchPathToPlayer() => currentPath = GetPathToPlayer();
	protected void FetchPathToPoint(Node point) => currentPath = pathMesh.FindPath(Pos, point.pos);
	protected List<Node> GetPathToPlayer() {
		var path = pathMesh.FindPath(Pos, player.Pos);
		if(path.Count > 1) {
			if(Vector3.Distance(path[0].pos, Pos) < pathNodeReachTreshold) {
				path.RemoveAt(0);
			}
		}
		return path;
	}
}
