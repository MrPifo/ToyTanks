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

	public byte playerDetectRadius = 25;
	public byte playerLoseRadius = 25;
	public bool showDebug;
	public LayerMask hitLayers;
	public LayerMask levelPredictionMask;
	protected float distToPlayer;
	protected float pathNodeReachTreshold = 0.5f;
	protected List<Node> currentPath;
	protected Node activePatrolPoint;
	// Properties
	protected bool IsAIEnabled { get; set; }
	protected bool IsPlayerInDetectRadius => distToPlayer < playerDetectRadius;
	protected bool IsPlayerOutsideLoseRadius => distToPlayer > playerLoseRadius;
	protected bool IsPlayerInShootRadius => distToPlayer < playerDetectRadius;
	protected bool IsAimingAtPlayer => IsAimingOnTarget(Player.transform);
	protected bool HasActivePatrolPoint => activePatrolPoint != null;
	protected bool HasSightContactToPlayer => HasSightContact(Player);
	public bool IsPlayReady => !HasBeenDestroyed && HasBeenInitialized && IsAIEnabled;
	protected int PathNodeCount => currentPath == null ? 0 : currentPath.Count;
	protected bool WouldFriendlyFire {
		get {
			var hitList = PredictBulletPath();
			if(hitList.Any(t => t.gameObject.CompareTag("Bot"))) {
				return true;
			}
			return false;
		}
	}
	protected PlayerInput Player => LevelManager.player;
	protected PathfindingMesh PathMesh => LevelManager.Grid;

	void Update() {
		if(IsAIEnabled) {
			distToPlayer = Vector3.Distance(Pos, Player.Pos);
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
				if(!HasSightContact(Player)) {
					Draw.Ring(Pos, Vector3.up, distToPlayer, 1f, new Color32(150, 20, 20, 255), true);
				}
			}
			if(!(IsPlayerInShootRadius && HasSightContact(Player)) && PathMesh != null) {
				PathMesh.DrawPathLines(currentPath);
			}
		}
	}

	public override void Revive() {
		base.Revive();
	}

	protected abstract void ProcessState();
	protected abstract void GoToNextState(float delay);

	public virtual void DisableAI() {
		IsAIEnabled = false;
	}
	public virtual void EnableAI() {
		IsAIEnabled = true;
	}

	public virtual void Aim() {
		MoveHead(Player.Pos);
	}

	public void KeepHeadRot() => tankHead.rotation = lastHeadRot;

	public override void GotHitByBullet() {
		base.GotHitByBullet();
		if(!IsInvincible && healthPoints <= 0) {
			healthBar.gameObject.SetActive(false);
			enabled = false;
		} else {
			healthBar.transform.parent.gameObject.SetActive(true);
		}
	}

	public void MoveAlongPath() {
		if(PathNodeCount > 0) {
			var dir = GetLookDirection(currentPath[0].pos);
			//PathMesh.UpdateNode(currentPath[0], currentPath[0].dist, Node.NodeType.wall);
			var moveDir = new Vector2(dir.x, dir.z);
			Move(moveDir);
		}
	}

	public void FaceDirection(Vector3 target) {
		target = new Vector3(target.x, Pos.y, target.z);
		AdjustRotation((target - Pos).normalized);
	}

	public void AimOnSight() {
		if(HasSightContact(Player)) {
			MoveHead(Player.Pos);
		}
	}

	public bool IsAimingOnTarget(Transform target) => IsAimingOnTarget(target.position);
	public bool IsAimingOnTarget(Vector3 target, float precision = 0f) {
		precision = precision == 0 ? 0.999f : precision;
		Vector3 dirFromAtoB = (tankHead.position - target).normalized;
		float dotProd = -Vector3.Dot(dirFromAtoB, tankHead.forward);
		if(dotProd > precision) {
			return true;
		}
		return false;
	}

	public bool IsFacing(Vector3 target, float precision = 0f) {
		precision = precision == 0 ? 0.999f : precision;
		Vector3 dirFromAtoB = (Pos - target).normalized;
		float dotProd = Mathf.Abs(Vector3.Dot(dirFromAtoB, transform.forward));
		if(dotProd > precision) {
			return true;
		}
		return false;
	}

	public List<Transform> PredictBulletPath() {
		RaycastHit lastHit = new RaycastHit();
		Ray ray = new Ray(bulletOutput.position, tankHead.forward);
		var hitList = new List<Transform>();
		for(int i = 0; i < Bullet.maxBounces + 1; i++) {
			if(i > 0) {
				ray = new Ray(lastHit.point, Vector3.Reflect(ray.direction, lastHit.normal));
			}

			if(Physics.BoxCast(ray.origin, Bullet.bulletSize, ray.direction, out lastHit, Quaternion.identity, Mathf.Infinity, levelPredictionMask)) {
				if(showDebug) Draw.Line(ray.origin, lastHit.point, Color.yellow);
				hitList.Add(lastHit.transform);
			}
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
		var nearestPoints = PathMesh.GetNodesWithinRadius(origin, radius);
		Node point = nearestPoints[Random(0, nearestPoints.Count - 1)];
		return point;
	}

	public void RefreshRandomPath(Vector3 origin, float radius, int nodeTreshold = 2) {
		if(HasActivePatrolPoint == false || PathNodeCount <= nodeTreshold) {
			activePatrolPoint = GetRandomPointOnMap(origin, radius);
		}
		FetchPathToPoint(activePatrolPoint);
	}

	public Node GetFurthestPointFrom(Vector3 origin, Vector3 from, float radius) {
		var points = PathMesh.GetNodesWithinRadius(origin, radius);
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

	public int Random(int min, int max) => UnityEngine.Random.Range(min, max);
	public float Random(float min, float max) => UnityEngine.Random.Range(min, max);

	protected void FetchPathToPlayer() => currentPath = GetPathToPlayer();
	protected void FetchPathToPoint(Node point) => currentPath = PathMesh.FindPath(Pos, point.pos);
	protected List<Node> GetPathToPlayer() {
		var path = PathMesh.FindPath(Pos, Player.Pos);
		if(path != null && path.Count > 1) {
			if(Vector3.Distance(path[0].pos, Pos) < pathNodeReachTreshold) {
				path.RemoveAt(0);
			}
		}
		return path;
	}
}
