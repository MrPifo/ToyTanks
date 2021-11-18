using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sperlich.Debug.Draw;
using Sperlich.FSM;
using EpPathFinding.cs;
using System.Collections;
using SimpleMan.Extensions;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class TankAI : TankBase, IHittable {

	public enum TankState { Waiting, Move, Attack, Retreat }

	public byte playerDetectRadius = 25;
	public byte playerLoseRadius = 25;
	public byte playerTooClose = 5;
	public bool showDebug;
	public DiagonalMovement diagonalMethod;
	public float maxPathfindingRefreshSpeed = 0.2f;
	float pathfindingRefreshTime;
	protected float distToPlayer;
	protected float pathNodeReachTreshold = 0.5f;
	public Vector3[] currentPath = new Vector3[0];
	protected Vector3 currentDestination;
	protected Vector3 nextMoveTarget => currentPath.Length <= 1 ? Vector3.zero : currentPath[0];
	protected FSM<TankState> stateMachine = new FSM<TankState>();
	/// <summary>
	/// Default includes: Player, Ground, Destructable, LevelBoundary, Block
	/// </summary>
	private LayerMask _hitLayers = LayerMaskExtension.Create(GameMasks.Player, GameMasks.Ground, GameMasks.Destructable, GameMasks.LevelBoundary, GameMasks.Block);
	public LayerMask HitLayers => _hitLayers;
	// Properties
	protected bool IsAIEnabled { get; set; }
	protected bool IsPlayerInDetectRadius => distToPlayer < playerDetectRadius;
	protected bool IsPlayerOutsideLoseRadius => distToPlayer > playerLoseRadius;
	protected bool IsPlayerInShootRadius => distToPlayer < playerDetectRadius;
	protected bool HasPathRemaining => currentPath != null && currentPath.Length > 1 ? true : false;
	protected bool IsPlayerTooNearby => distToPlayer < playerTooClose;
	protected bool IsAimingAtPlayer => IsFacingTarget(Player.transform);
	protected bool HasSightContactToPlayer => HasSightContact(Player);
	protected bool PathToPlayerExists => PathExists(Pos, Player.Pos);
	protected bool IsReadyForRefresh {
		get {
			if(pathfindingRefreshTime > maxPathfindingRefreshSpeed) {
				pathfindingRefreshTime = 0;
				return true;
			}
			return false;
		}
	}
	protected bool HasReachedDestination => Vector3.Distance(Pos, currentDestination) < 1f;
	public bool IsPlayReady => !HasBeenDestroyed && HasBeenInitialized && IsAIEnabled;
	protected int PathNodeCount => currentPath == null ? 0 : currentPath.Length;
	protected bool WouldFriendlyFire {
		get {
			var hitList = PredictBulletPath();
			if(hitList.Any(t => t.gameObject.CompareTag("Bot"))) {
				return true;
			}
			return false;
		}
	}
	protected PlayerInput Player => LevelManager.player == null ? FindObjectOfType<PlayerInput>() : LevelManager.player;
	

	protected virtual void Update() {
		if(IsPlayReady) {
			distToPlayer = Vector3.Distance(Pos, Player.Pos);
			AdjustOccupiedGridPos();
			DrawDebug();

			pathfindingRefreshTime += Time.deltaTime;
		}
	}

	protected virtual void DrawDebug() {
		if(showDebug) {
			if(currentPath != null && currentPath.Length > 0) {
				AIGrid.DrawPathLines(currentPath);
			}
			Draw.Text(Pos + Vector3.up * 2, stateMachine.Text + " : " + healthPoints, 8, Color.black);
		}
	}

	public override void Revive() {
		base.Revive();
	}

	public override void InitializeTank() {
		base.InitializeTank();
		showDebug = Game.showTankDebugs;
		AdjustOccupiedGridPos();
	}

	protected void ProcessState(TankState state) {
		if(HasBeenDestroyed == false && IsAIEnabled) {
			stateMachine.Push(state);
			switch(state) {
				case TankState.Waiting:
					StartCoroutine(IAttack());
					break;
				case TankState.Move:
					StartCoroutine(IMove());
					break;
				case TankState.Attack:
					StartCoroutine(IAttack());
					break;
				case TankState.Retreat:
					StartCoroutine(IRetreat());
					break;
				default:
					break;
			}
		}
	}
	protected virtual void GoToNextState() { }
	protected virtual void GoToNextState(float delay = 0.0001f) { }
	protected virtual void GoToNextState(TankState state, float delay = 0.0001f) {
		if(IsPlayReady) {
			this.Delay(delay, () => {
				ProcessState(state);
			});
		}
	}

	public virtual void DisableAI() {
		IsAIEnabled = false;
	}
	public virtual void EnableAI() {
		IsAIEnabled = true;
	}

	public override void TakeDamage(IDamageEffector effector) {
		base.TakeDamage(effector);
		if(!IsInvincible && healthPoints <= 0) {
			FreeOccupiedGridPos();
			healthBar.gameObject.SetActive(false);
			enabled = false;
		} else if(IsFriendlyFireImmune == false || effector.fireFromPlayer) {
			healthBar.transform.parent.gameObject.SetActive(true);
		}
	}

	// TankAI Control Methods
	/// <summary>
	/// Tank will aim the player. Should be called in Update.
	/// </summary>
	protected virtual void AimAtPlayer() => MoveHead(Player.Pos);
	/// <summary>
	/// Prevents the tank head to rotate with the body.
	/// </summary>
	protected void KeepHeadRot() => tankHead.rotation = lastHeadRot;
	/// <summary>
	/// Moves the tank along the currently active path. Must be called in Update.
	/// If not called in Update, ConsumePath() needs to be called every frame instead.
	/// </summary>
	protected void MoveAlongPath() {
		if(PathNodeCount > 0) {
			var dir = GetLookDirection(currentPath[0]);
			var moveDir = new Vector2(dir.x, dir.z);
			Move(moveDir);
		}
	}
	/// <summary>
	/// Moves the tank directly to the player (For Pathfinding call ChasePlayer() instead!). Must be called in Update.
	/// </summary>
	protected void MoveToPlayer() {
		var moveDir = (Player.Pos - Pos).normalized;
		Move(moveDir);
		currentDestination = Player.Pos;
	}
	/// <summary>
	/// Moves the tank to the player
	/// </summary>
	protected void ChasePlayer() {
		if(HasSightContactToPlayer) {
			MoveToPlayer();
		} else {
			RefreshPathToPlayer(true);
			MoveAlongPath();
		}
	}
	/// <summary>
	/// Moves the tank along the current active path. But is the current destination in line of sight,
	/// pathfinding is ignored and the tank moves directly to the destination.
	/// </summary>
	protected void MoveSmart() {
		if(HasSightContact(currentDestination, 1f)) {
			MoveToTarget(currentDestination);
		} else {
			MoveAlongPath();
		}
	}
	/// <summary>
	/// Moves the tank to the given target. Must be called in Update.
	/// </summary>
	/// <param name="target"></param>
	protected void MoveToTarget(Vector3 target) {
		Move((target - Pos).normalized);
	}
	protected void TurnToTarget(Vector3 target) {
		target = new Vector3(target.x, Pos.y, target.z);
		AdjustRotation((target - Pos).normalized);
	}
	/// <summary>
	/// Calculates a path to the player.
	/// </summary>
	/// <param name="performanceMode"></param>
	protected void RefreshPathToPlayer(bool performanceMode = false) {
		if(FindPath(Player.Pos, performanceMode)) {
			currentDestination = currentPath[currentPath.Length - 1];
		}
	}
	/// <summary>
	/// Calculates a path which leads away from the given target.
	/// Should not be used within Update.
	/// </summary>
	/// <param name="target"></param>
	/// <param name="fleeRadius"></param>
	/// <param name="performanceMode"></param>
	protected void FleeFrom(Vector3 target, float fleeRadius) {
		var points = Game.ActiveGrid.GetPointsWithinRadius(Pos, fleeRadius);
		float fleeRadiusSqr = fleeRadius * fleeRadius;
		List<(Vector3 point, float distance)> distPoints = new List<(Vector3, float)>();

		foreach(Vector3 p in points) {
			var diff = target - p;
			distPoints.Add((p, diff.x * diff.x + diff.z * diff.z));
		}

		currentDestination = distPoints.OrderByDescending(p => p.distance).First().point;
		FindPath(currentDestination);
	}
	/// <summary>
	/// Calculates a path to the given target.
	/// Performance mode reduces calls in Update to the given refresh time.
	/// </summary>
	/// <param name="target"></param>
	/// <param name="performanceMode"></param>
	/// <returns></returns>
	protected bool FindPath(Vector3 target, bool performanceMode = false) {
		if(performanceMode == false || IsReadyForRefresh) {
			FreeOccupiedGridPos();
			currentPath = Game.ActiveGrid.FindPath(Pos, target, diagonalMethod);
			for(int i = 0; i < currentPath.Length; i++) {
				if(Vector3.Distance(Pos, currentPath[i]) < 1.5f) {
					var list = currentPath.ToList();
					list.RemoveAt(i);
					currentPath = list.ToArray();
				}
			}
			AdjustOccupiedGridPos();
			if(currentPath.Length > 0) {
				return true;
			}
		}
		return false;
	}

	protected IEnumerator IPauseTank() {
		if(IsPaused) {
			while(IsPaused) yield return null;   // Pause AI
		} else {
			yield return null;
		}
	}

	// TankAI Helper Methods
	protected bool IsFacingTarget(Transform target) => IsFacingTarget(target.position);
	protected bool IsFacingTarget(Vector3 target, float precision = 0f) {
		precision = precision == 0 ? 0.999f : precision;
		Vector3 dirFromAtoB = (tankHead.position - target).normalized;
		float dotProd = -Vector3.Dot(dirFromAtoB, tankHead.forward);
		if(dotProd > precision) {
			return true;
		}
		return false;
	}
	protected bool HasSightContact(TankBase target, float scanSize = 0.05f) => HasSightContact(target.transform, scanSize);
	protected bool HasSightContact(Transform target, float scanSize = 0.05f) {
		Ray ray = new Ray(Pos, (target.position - Pos).normalized);
		ray.direction = new Vector3(ray.direction.x, 0, ray.direction.z);

		if(Physics.SphereCast(ray, scanSize, out RaycastHit hit, Mathf.Infinity)) {
			if(hit.transform.gameObject == target.gameObject) {
				return true;
			}
		}
		return false;
	}
	protected bool HasSightContact(Vector3 target, float scanSize = 0.05f) {
		Ray ray = new Ray(Pos, (target - Pos).normalized);
		ray.direction = new Vector3(ray.direction.x, 0, ray.direction.z);

		if(Physics.SphereCast(ray, scanSize, Vector3.Distance(ray.origin, target))) {
			return false;
		}
		return true;
	}
	protected bool PathExists(Vector3 from, Vector3 to) {
		var path = Game.ActiveGrid.FindPath(from, to);
		if(path == null || path.Length == 0) {
			return false;
		}
		return true;
	}
	/// <summary>
	/// Needs to be called in Update as long as the tank is moving along a path.
	/// This function reduces and updates the current active path without calculating a new path to save performance.
	/// </summary>
	protected void ConsumePath(float reachTreshold = 1) {
		// Is required if path is not refreshed every frame
		if(PathNodeCount > 0) {
			if(Vector3.Distance(Pos, currentPath[0]) < reachTreshold) {
				var list = currentPath.ToList();
				list.RemoveAt(0);
				currentPath = list.ToArray();
			}
		}
	}

	public List<Transform> PredictBulletPath() {
		RaycastHit lastHit = new RaycastHit();
		Ray ray = new Ray(bulletOutput.position, tankHead.forward);
		var hitList = new List<Transform>();
		for(int i = 0; i < Bullet.maxBounces + 1; i++) {
			if(i > 0) {
				ray = new Ray(lastHit.point, Vector3.Reflect(ray.direction, lastHit.normal));
			}

			if(Physics.BoxCast(ray.origin, Bullet.bulletSize, ray.direction, out lastHit, Quaternion.identity, Mathf.Infinity, HitLayers)) {
				if(showDebug) Draw.Line(ray.origin, lastHit.point, Color.yellow);
				hitList.Add(lastHit.transform);
			}
		}
		return hitList;
	}

	public bool RandomPath(Vector3 origin, float radius, float? minRadius = null) {
		var points = Game.ActiveGrid.GetPointsWithinRadius(Pos, radius, minRadius).ToList();

		points = points.Where(p => PathExists(Pos, p)).ToList();
		points = points.OrderByDescending(v => Vector3.Distance(origin, v)).ToList();
		if(points.Count > 0) {
			currentDestination = points.RandomItem();
			//Game.ActiveGrid.PaintCells(points.ToArray(), Color.blue, 3);
			//Game.ActiveGrid.PaintCellAt(currentDestination, Color.blue);
			return FindPath(currentDestination);
		}
		return false;
	}

	public int Random(int min, int max) => Randomizer.Range(min, max);
	public float Random(float min, float max) => Randomizer.Range(min, max);

	// TankState Method Overrides
	protected virtual IEnumerator IAttack() { return null; }
	protected virtual IEnumerator IWaiting() { return null; }
	protected virtual IEnumerator IMove() { return null; }
	protected virtual IEnumerator IRetreat() { return null; }
}
