using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sperlich.Debug.Draw;
using Sperlich.FSM;
using EpPathFinding.cs;
using System.Collections;
using SimpleMan.Extensions;
using DG.Tweening;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class TankAI : TankBase, IHittable {

	public enum TankState { Waiting, Move, Attack, Retreat, Charge }
	public enum MovementType {
		/// <summary>
		/// Use this if movement gets an Input Direction.
		/// </summary>
		None,
		/// <summary>
		/// Moves the Tank in Forward direction.
		/// </summary>
		Move,
		/// <summary>
		/// Moves the Tank along a path and ignores unncessery path point if possible.
		/// Note: A path must be calculated for this method.
		/// </summary>
		MoveSmart,
		/// <summary>
		/// Moves the Tank exactly along the path.
		/// Note: A path must be calculated for this method.
		/// </summary>
		MovePath,
		/// <summary>
		/// Let the tank chase the player.
		/// </summary>
		Chase,
	}
	public enum TankHeadMode { None, RotateWithBody, KeepRotation, AimAtPlayer, AimAtPlayerOnSight }

	public byte playerDetectRadius = 25;
	public byte playerLoseRadius = 25;
	public byte playerTooClose = 5;
	public float avoidanceRadius = 1f;
	public float avoidanceDistance = 3f;
	public bool showDebug;
	public bool disableAvoidanceSystem;
	public bool disableSmartMove;
	public DiagonalMovement diagonalMethod;
	public FSM<MovementType> MoveMode = new FSM<MovementType>();
	public FSM<TankHeadMode> HeadMode = new FSM<TankHeadMode>();
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
	protected static LayerMask HitLayers = LayerMaskExtension.Create(GameMasks.Player, GameMasks.Ground, GameMasks.Destructable, GameMasks.LevelBoundary, GameMasks.Block);
	/// <summary>
	/// Layermask used for the avoidance system to prevent AI from being stuck
	/// </summary>
	protected static LayerMask AvoidcanceLayers = LayerMaskExtension.Create(GameMasks.Block, GameMasks.Destructable, GameMasks.Player);
	protected static LayerMask MapLayers = LayerMaskExtension.Create(GameMasks.Block, GameMasks.Destructable, GameMasks.BulletTraverse, GameMasks.LevelBoundary);
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
			if(APITimeMode == TimeMode.DeltaTime) {
				ComputeAI();
			}
			AdjustOccupiedGridPos();
			DrawDebug();
			
			if(showDebug) {
				Draw.Sphere(DirectionLeader.position, 0.5f, Color.green);
				Draw.Line(rig.position, DirectionLeader.position, 3, Color.blue);
			}
		}
	}

	private void FixedUpdate() {
		if(APITimeMode == TimeMode.FixedUpdate) {
			ComputeAI();
		}
	}

	private void ComputeAI() {
		if(IsPlayReady && IsPaused == false) {
			AvoidanceSystem();
			distToPlayer = Vector3.Distance(Pos, Player.Pos);
			pathfindingRefreshTime += GetTime;
			switch(MoveMode.State) {
				case MovementType.None:
					break;
				case MovementType.Move:
					Move();
					break;
				case MovementType.MovePath:
					MoveAlongPath();
					ConsumePath();
					break;
				case MovementType.MoveSmart:
					MoveSmart();
					ConsumePath();
					break;
				case MovementType.Chase:
					ChasePlayer();
					break;
			}

			switch(HeadMode.State) {
				case TankHeadMode.None:
					break;
				case TankHeadMode.RotateWithBody:
					break;
				case TankHeadMode.KeepRotation:
					KeepHeadRot();
					break;
				case TankHeadMode.AimAtPlayer:
					AimAtPlayer();
					break;
				case TankHeadMode.AimAtPlayerOnSight:
					if(HasSightContactToPlayer) {
						AimAtPlayer();
					} else {
						KeepHeadRot();
					}
					break;
			}
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
				case TankState.Charge:
					StartCoroutine(ICharge());
					break;
				default:
					break;
			}
		}
	}
	protected virtual void GoToNextState() { }
	protected virtual void GoToNextState(float delay = 0.0001f) { }
	protected virtual void GoToNextState(TankState state, float delay = 0) {
		if(IsPlayReady) {
			this.Delay(delay < 0.05f ? Time.deltaTime : delay, () => {
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

	/// <summary>
	/// System for preventing AI from being stuck or better movement.
	/// </summary>
	public virtual void AvoidanceSystem() {
		evadeDir = Vector3.zero;
		if(disableAvoidanceSystem == false && IsPlayReady && IsStatic == false) {
			if(showDebug) {
				Draw.Line(rig.position, rig.position + transform.forward * currentDirFactor * avoidanceDistance, 2, Color.red);
			}
			if(Physics.SphereCast(new Ray(rig.position, transform.forward * currentDirFactor), avoidanceRadius, out RaycastHit hit, avoidanceDistance, AvoidcanceLayers)) {
				if(showDebug) {
					Draw.Sphere(hit.point, avoidanceRadius, Color.yellow, true);
				}
				evadeDir = (rig.position - hit.point).normalized;
				if(Mathf.Abs(evadeDir.x) >= 0.15) {
					evadeDir.x = Mathf.Sign(evadeDir.x);
				} else {
					evadeDir.x = 0;
				}
				evadeDir = new Vector3(evadeDir.x, 0, 0);
			}
		}
	}

	// TankAI Control Methods

	/// <summary>
	/// TankAI movement override
	/// </summary>
	/// <param name="inputDir"></param>
	public override void Move(Vector2 inputDir) {
		DirectionLeader.position = Vector3.Lerp(DirectionLeader.position, rig.position + new Vector3(inputDir.x, 0, inputDir.y) * 2, GetTime * turnSpeed);
		if(evadeDir != Vector3.zero) {
			DirectionLeader.position = Vector3.Lerp(DirectionLeader.position, DirectionLeader.position + evadeDir, GetTime * turnSpeed);
		}
		moveDir = (DirectionLeader.position - rig.position).normalized;

		currentDirFactor = Mathf.Sign(Vector3.Dot(moveDir.normalized, rig.transform.forward));
		if(disable2DirectionMovement) {
			currentDirFactor = 1;
		}

		/*float maxDir = Mathf.Max(Mathf.Abs(inputDir.x), Mathf.Abs(inputDir.y));
		if(maxDir > 0.7f) {
			maxDir = 1;
		}
		float factor = currentDirFactor * maxDir;*/
		Vector3 movePos = currentDirFactor * moveSpeed * GetTime * rig.transform.forward;

		if(!isShootStunned && canMove) {
			rig.MovePosition(rig.position + movePos);
			rig.MoveRotation(Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(moveDir * currentDirFactor, Vector3.up), GetTime * turnSpeed));
		}
		TrackTracer(currentDirFactor);
		rig.velocity = Vector3.zero;
	}
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
		if(HasSightContact(currentDestination, 1f) && disableSmartMove == false) {
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
		RotateTank((target - Pos).normalized);
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
	protected bool FleeFrom(Vector3 target, float fleeRadius, float? minRadius = null) {
		var points = Game.ActiveGrid.GetPointsWithinRadius(Pos, fleeRadius, minRadius);
		List<(Vector3 point, float distance)> distPoints = new List<(Vector3, float)>();

		foreach(Vector3 p in points) {
			var diff = target - p;
			distPoints.Add((p, diff.x * diff.x + diff.z * diff.z));
		}
		
		distPoints = distPoints.Where(p => Physics.Linecast(p.point, Player.Pos, MapLayers)).OrderByDescending(p => p.distance).ToList();
		//Game.ActiveGrid.PaintCells(distPoints.Select(p => p.point).ToArray(), Color.blue, 3);
		if(distPoints.Count > 0) {
			currentDestination = distPoints.RandomItem().point;
			return FindPath(currentDestination);
		} else {
			return false;
		}
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
			switch(APITimeMode) {
				case TimeMode.DeltaTime:
					while(IsPaused)
						yield return null;   // Pause AI
					break;
				case TimeMode.FixedUpdate:
					while(IsPaused)
						yield return new WaitForFixedUpdate();   // Pause AI
					break;
			}
		} else {
			switch(APITimeMode) {
				case TimeMode.DeltaTime:
					yield return null;   // Pause AI
					break;
				case TimeMode.FixedUpdate:
					yield return new WaitForFixedUpdate();   // Pause AI
					break;
			}
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
		for(int i = 0; i < 2; i++) {
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

	public bool RandomPath(Vector3 origin, float radius, float? minRadius = null, bool requireSightContact = false) {
		var points = Game.ActiveGrid.GetPointsWithinRadius(Pos, radius, minRadius).ToList();

		points = points.Where(p => PathExists(Pos, p)).ToList();
		points = points.OrderByDescending(v => Vector3.Distance(origin, v)).ToList();
		if(requireSightContact) {
			points = points.Where(p => Physics.Linecast(p, Pos, MapLayers) == false).ToList();
		}
		if(points.Count > 0) {
			currentDestination = points.RandomItem();
			//Game.ActiveGrid.PaintCells(points.ToArray(), Color.blue, 3);
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
	protected virtual IEnumerator ICharge() { return null; }
}
