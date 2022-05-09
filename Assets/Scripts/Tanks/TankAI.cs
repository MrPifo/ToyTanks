using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sperlich.Debug.Draw;
using Sperlich.FSM;
using Cysharp.Threading.Tasks;

public abstract class TankAI : TankBase, IHittable {

	public enum TankState { Waiting, Patrol, Attack, Chase, Retreat, Charge }
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
		/// Moves the Tank with a given direction {targetMoveDir}
		/// </summary>
		MoveDir,
	}
	public enum AimingMode { None, RotateWithBody, KeepRotation, AimAtPlayer, AimAtPlayerOnSight, RandomAim, AimAtTarget }

	[Tooltip("Disables the tank dual movement system and forces rotation towards moving direction.")]
	public bool disableDualMovement => disable2DirectionMovement;
	[Header("AI")]
	[Tooltip("Disables the system that prevents the tank from colliding and getting stuck with another tank.")]
	public bool disableAntiTankCollide;
	[Tooltip("Disables the tank smoother movement by disabling the helper Transform.")]
	public bool disableGuider;
	[Tooltip("Keeps the tank moving while waiting for path calculation.")]
	protected bool keepMoving;
	public bool enableInaccurateAim;
	public bool debugMode => AIManager.showTankDebugs;
	[Tooltip("Chance to fire a bullet every frame. Range from 0% - 100%")]
	public FloatGrade shotChancePerFrame;
	public FloatGrade inaccurateAim;
	public List<TankBase> collidingTanks;
	private bool isCalculatingPath;
	private float randomShootChanceTimeElapsed;
	private float inaccurateAimFlipTime;
	protected float distToPlayer;
	/// <summary>
	/// Current Target the tank is moving towards to
	/// </summary>
	protected Vector3 targetMovingPoint;
	protected Vector3 finalPathDestination;
	protected Vector3 aimTarget;
	protected Vector3 dodgeVector;
	public Vector3 targetMoveDir;
	protected Vector3 nextMoveTarget => Path.Length <= 1 ? Vector3.zero : Path[0];
	[SerializeField]
	protected Vector3[] Path = new Vector3[0];
	protected Vector3[] UnmodifiedPath = new Vector3[0];
	[SerializeField]
	private MovementType movementMethod = MovementType.None;
	[SerializeField]
	private AimingMode aimingMethod = AimingMode.None;
	protected FSM<TankState> stateMachine = new FSM<TankState>();
	protected AIManager.AttackToken Token;
	const float antiTankDodgePower = 3f;

	#region LayerMasks
	/// <summary>
	/// Default includes: Player, Ground, Destructable, LevelBoundary, Block
	/// </summary>
	protected static LayerMask HitLayers = LayerMaskExtension.Create(GameMasks.Player, GameMasks.Destructable, GameMasks.LevelBoundary, GameMasks.Block);
	/// <summary>
	/// Layermask used for the avoidance system to prevent AI from being stuck
	/// </summary>
	protected static LayerMask AntiTankLayer = LayerMaskExtension.Create(GameMasks.Player, GameMasks.Bot);
	protected static LayerMask BulletLayer = LayerMaskExtension.Create(GameMasks.Bullet);
	public static LayerMask MapLayers = LayerMaskExtension.Create(GameMasks.Block, GameMasks.Destructable, GameMasks.BulletTraverse, GameMasks.LevelBoundary);
	protected static LayerMask PathFilterLayer = LayerMaskExtension.Create(GameMasks.Block, GameMasks.Destructable, GameMasks.BulletTraverse, GameMasks.LevelBoundary);
	#endregion

	#region Properties
	/// <summary>
	/// Neural Network brain component of the tank, if one exists.
	/// </summary>
	protected MLTank Brain { get; set; }
	protected bool IsAIEnabled { get; set; }
	/// <summary>
	/// True if an attack token has been requested and received a available Token.
	/// </summary>
	protected bool AttackPossible => Token != null && Token.inUse;
	protected bool IsAimingAtPlayer => IsFacingTarget(Target.Pos);
	protected bool HasSightContactToPlayer => HasSightContact(Target, HitLayers);
	protected bool FinalDestInReach => RemainingPathPercent < 5 || Vector3.Distance(Pos, finalPathDestination) <= 1.6f;
	protected bool NextPathPointInReach => Vector3.Distance(Pos, targetMovingPoint) < 1.5f;
	protected bool HasPathRemaining => Path != null && Path.Length > 0;
	protected int PathNodeCount => Path == null ? 0 : Path.Length;
	protected float RemainingPathPercent => Mathf.Round(GetPathLengthFromPos(Path, Pos) / GetPathLength(UnmodifiedPath, Pos) * 100);
	protected float RemainingPathDistance => Mathf.Round(GetPathLengthFromPos(Path, Pos) * 100) / 100f;
	protected bool WouldFriendlyFire {
		get {
			var hitList = PredictBulletPath();
			if(hitList.Any(t => t.gameObject.CompareTag("Bot"))) {
				return true;
			}
			return false;
		}
	}
	public Vector3 Velocity => (lastPos - Pos).normalized;
	private TankBase _target;
	public TankBase Target {
		get {
			if(_target == null) {
				_target = FindObjectOfType<TankBase>();
			}
			return _target;
		}
		set => _target = value;
	}
	public System.Threading.CancellationTokenSource CancelAIToken { get; private set; } = new System.Threading.CancellationTokenSource();
	public bool IsPlayReady => !HasBeenDestroyed && HasBeenInitialized && IsAIEnabled;
	public bool IsMLTank => Brain != null;
	public Rigidbody Rig => rig;
	#endregion

	#region Task-Conditions
	protected WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
	protected WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
    #endregion

    #region Updates/Init
    public override void InitializeTank() {
		base.InitializeTank();
		Brain = GetComponent<MLTank>();
		AdjustOccupiedGridPos();
		Target = FindObjectOfType<PlayerTank>();
		AIManager.RegisterAI(this);
	}
	protected virtual void Update() {
		if(IsPlayReady) {
			DrawDebug();
			
			if(debugMode && disableGuider == false) {
				Draw.Sphere(GuiderTransform.position, 0.3f, Color.green);
				Draw.Line(rig.position, GuiderTransform.position, 3, Color.blue);
			}
			if(isUsingWheels) {
				UpdateWheelRotation();
			}
			if (isCalculatingPath == false) {
				AdjustOccupiedGridPos();
			}
		}
	}
	private void FixedUpdate() {
		if (IsPlayReady) {
			Pos = tankBody.position;
			ComputeAI();
			TankAntiCollision();
			lastPos = Pos;
		}
	}
	private void ComputeAI() {
		if(IsPlayReady && Game.IsGamePlaying && IsPaused == false) {
			TankAntiCollision();
			distToPlayer = Vector3.Distance(Pos, Target.Pos);
			rig.velocity = Vector3.zero;
			switch(movementMethod) {
				case MovementType.None:
					break;
				case MovementType.Move:
					Move();
					break;
				case MovementType.MovePath:
					MoveAlongPath();
					break;
				case MovementType.MoveSmart:
					MoveSmart();
					break;
				case MovementType.MoveDir:
					Move(targetMoveDir.Vector2XZ().normalized);
					break;
			}

			switch(aimingMethod) {
				case AimingMode.None:
					break;
				case AimingMode.RotateWithBody:
					break;
				case AimingMode.KeepRotation:
					KeepHeadRot();
					break;
				case AimingMode.AimAtPlayer:
					AimAtPlayer();
					break;
				case AimingMode.AimAtPlayerOnSight:
					if(HasSightContactToPlayer) {
						AimAtPlayer();
					} else {
						KeepHeadRot();
					}
					break;
				case AimingMode.RandomAim:
					AimInaccurate(bulletOutput.position, inaccurateAim, 0.4f);
					break;
				case AimingMode.AimAtTarget:
					if(enableInaccurateAim) {
						AimInaccurate(aimTarget, inaccurateAim, 1f);
					} else {
						MoveHead(aimTarget);
					}
					break;
			}

			if (HasPathRemaining == false && keepMoving) {
				Move(transform.forward);
			}
		} else {
			rig.velocity = Vector3.zero;
		}
	}
	protected virtual void DrawDebug() {
		if (debugMode) {
			if(PathNodeCount > 0) {
				AIGrid.DrawPathLines(Path);
			}
			Draw.Text(Pos + Vector3.up, stateMachine.Text, 6, Color.black);
		}
	}
	public void SetMovement(MovementType method) {
		movementMethod = method;
		if((method == MovementType.MovePath || method == MovementType.MoveSmart) && PathNodeCount < 1) {
			Debug.LogError($"Error: Movement method has been set to {method} with no active path.");
        }
	}
	public void SetAiming(AimingMode method) => aimingMethod = method;
	#endregion

	#region State-Machine
	public virtual void DisableAI() {
		IsAIEnabled = false;
	}
	public virtual void EnableAI() {
		IsAIEnabled = true;
	}
	protected async UniTask CheckPause(PlayerLoopTiming pauseType = PlayerLoopTiming.FixedUpdate) {
		while (IsPaused && IsPlayReady) {
			switch (pauseType) {
				case PlayerLoopTiming.FixedUpdate:
					await UniTask.WaitForFixedUpdate();
					break;
				case PlayerLoopTiming.Update:
					await UniTask.WaitForEndOfFrame(this);
					break;
				default:
					await UniTask.WaitForEndOfFrame(this);
					break;
			}
			CancelAIToken.Token.ThrowIfCancellationRequested();
		}
		if(IsPaused == false || IsPlayReady == false) {
			switch (pauseType) {
				case PlayerLoopTiming.FixedUpdate:
					await UniTask.WaitForFixedUpdate();
					break;
				case PlayerLoopTiming.Update:
					await UniTask.WaitForEndOfFrame(this);
					break;
				default:
					await UniTask.WaitForEndOfFrame(this);
					break;
			}
			CancelAIToken.Token.ThrowIfCancellationRequested();
		}
	}
	#endregion

	public override void TakeDamage(IDamageEffector effector, bool instantKill = false) {
		base.TakeDamage(effector, instantKill);
		if(!IsInvincible && healthPoints <= 0 && CancelAIToken.IsCancellationRequested == false) {
			AIGrid.FreeAllHolderCells(hashcode);
			enabled = false;
			canMove = false;
			CancelAIToken.Cancel();
			LevelManager.Instance?.TankDestroyedCheck(this);
			CancelAIToken.CancelAfter(10000);
		}
	}

	public void OnApplicationQuit() {
		CancelAIToken.Cancel();
	}

	/// <summary>
	/// System for preventing AI from being stuck or better movement.
	/// </summary>
	public virtual void TankAntiCollision() {
		if(IsPlayReady && IsStatic == false && disableAntiTankCollide == false && Time.frameCount % 30 == 0) {
			collidingTanks = new List<TankBase>();
			Collider[] overlaps = new Collider[3];
			int amount = Physics.OverlapSphereNonAlloc(Pos + transform.forward * currentDirFactor * 2f, 0.75f, overlaps, AntiTankLayer);
			//Draw.Sphere(Pos + transform.forward * currentDirFactor * 2f, 1f, Color.red);
			//collidingTanks = overlaps.Select(o => o.attachedRigidbody?.gameObject).Where(r => r.TryGetComponent(out TankBase t) && r.gameObject != gameObject).ToList();
			for(int i = 0; i < amount; i++) {
				if(overlaps[i].attachedRigidbody != null && overlaps[i].attachedRigidbody != rig && overlaps[i].attachedRigidbody.TryGetComponent(out TankBase t) && collidingTanks.Contains(t) == false) {
					if(t is TankAI) {
						var ai = (TankAI)t;
						if(ai.collidingTanks.Contains(this) == false) {
							collidingTanks.Add(t);
                        }
					} else {
						collidingTanks.Add(t);
					}
                }
            }
			if(amount > 0) {
				dodgeVector = Vector3.Lerp(dodgeVector, Quaternion.Euler(0, 75, 0) * (overlaps[0].transform.position - Pos).normalized * antiTankDodgePower * currentDirFactor, Time.fixedDeltaTime * 2);
				//Draw.Line(Pos, Pos + dodgeVector, 5, Color.yellow);
				//Draw.Sphere(overlaps[0].transform.position, 0.5f, Color.blue);
				//Debug.Log(overlaps[0].transform.parent.gameObject);
            } else {
				dodgeVector = Vector3.Lerp(dodgeVector, Vector3.zero, Time.fixedDeltaTime);
            }
			dodgeVector.y = 0;
		}
	}

	#region AI-Controller Methods
	/// <summary>
	/// TankAI movement override
	/// </summary>
	/// <param name="inputDir"></param>
	protected override void Move(Vector2 inputDir) {
		if (IsPlayReady) {
			if (disableGuider == false) {
				GuiderTransform.position = Vector3.Lerp(GuiderTransform.position, rig.position + new Vector3(inputDir.x, 0, inputDir.y) + dodgeVector, GetTime * turnSpeed);
				moveDir = (GuiderTransform.position - rig.position).normalized;
			} else {
				moveDir = new Vector3(inputDir.normalized.x, 0, inputDir.normalized.y) + dodgeVector;
			}

			currentDirFactor = Mathf.Sign(Vector3.Dot(moveDir.normalized, rig.transform.forward));
			if (disable2DirectionMovement) {
				currentDirFactor = 1;
			}
			Vector3 movePos = currentDirFactor * moveSpeed * rig.transform.forward;

			if (!isShootStunned && canMove) {
				rig.velocity = movePos;
				rig.MoveRotation(Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(moveDir * currentDirFactor, Vector3.up), GetTime * turnSpeed));
			}
			TrackTracer();
		}
	}
	/// <summary>
	/// Tank will aim the player. Should be called in Update.
	/// </summary>
	protected virtual void AimAtPlayer() {
		if(enableInaccurateAim) {
			AimInaccurate(Target.Pos, inaccurateAim, 1);
		} else {
			MoveHead(Target.Pos);
		}
	}
	protected virtual void AimInaccurate(Vector3 target, float strength, float aimSpeed) {
		Vector3 randTarget = target;
		if(inaccurateAimFlipTime >= 0) {
			inaccurateAimFlipTime += GetTime * aimSpeed;
			if(inaccurateAimFlipTime > 3) {
				inaccurateAimFlipTime = -3;
			}
		} else {
			inaccurateAimFlipTime += GetTime * 2;
		}
		randTarget += new Vector3(Mathf.Sin(inaccurateAimFlipTime) * strength, 0, Mathf.Cos(inaccurateAimFlipTime) * strength);
		MoveHead(randTarget);
	}
	/// <summary>
	/// Prevents the tank head to rotate with the body.
	/// </summary>
	private void KeepHeadRot() {
		if(enableInaccurateAim) {
			AimInaccurate(bulletOutput.position, inaccurateAim, 0.4f);
		} else {
			tankHead.rotation = lastHeadRot;
		}
	}
	/// <summary>
	/// Moves the tank along the currently active path. Must be called in Update.
	/// If not called in Update, ConsumePath() needs to be called every frame instead.
	/// </summary>
	private void MoveAlongPath() {
		if(PathNodeCount > 0) {
			var dir = GetLookDirection(Path[0]);
			var moveDir = new Vector2(dir.x, dir.z);
			Move(moveDir);
			UsePath();
		}
	}
	private void MoveToPlayer() {
		var moveDir = (Target.Pos - Pos).normalized;
		Move(moveDir);
		targetMovingPoint = Target.Pos;
	}
	private void ChasePlayer() {
		if(HasSightContactToPlayer) {
			MoveToPlayer();
		} else {
			MoveAlongPath();
		}
	}
	private void MoveSmart() {
		if(HasSightContact(targetMovingPoint, MapLayers, 0.5f)) {
			MoveToTarget(targetMovingPoint);
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
	#endregion

	#region AI-Info Getters
	protected bool IsFacingTarget(Vector3 target, float precision = 0f) {
		precision = precision == 0 ? 0.999f : precision;
		Vector3 dirFromAtoB = (tankHead.position - target).normalized;
		float dotProd = -Vector3.Dot(dirFromAtoB, tankHead.forward);
		if(dotProd > precision) {
			return true;
		}
		return false;
	}
	protected bool HasSightContact(TankBase target, LayerMask mask, float scanSize = 0.05f) => target != null ? HasSightContact(target.transform, mask, scanSize) : false;
	protected bool HasSightContact(Transform target, LayerMask mask, float scanSize = 0.05f) {
		try {
			Ray ray = new Ray(Pos, (target.position - Pos).normalized);
			ray.direction = new Vector3(ray.direction.x, 0, ray.direction.z);

			if (Physics.SphereCast(ray, scanSize, out RaycastHit hit, Mathf.Infinity, HitLayers)) {
				if (hit.transform.gameObject == target.gameObject) {
					if (debugMode) {
						Draw.Line(ray.origin, hit.point, Color.green);
					}
					return true;
				}
				if (debugMode) {
					Draw.Line(ray.origin, hit.point, Color.red);
				}
			}
		} catch(System.Exception e) {
			Logger.LogError(e, "Failed to determine sight contact.");
        }
		return false;
	}
	protected bool HasSightContact(Vector3 target, LayerMask mask, float scanSize = 0.05f) {
		Ray ray = new Ray(Pos, (target - Pos).normalized);
		ray.direction = new Vector3(ray.direction.x, 0, ray.direction.z);

		if(Physics.SphereCast(ray, scanSize, Vector3.Distance(ray.origin, target), mask)) {
			return false;
		}
		return true;
	}
	/// <summary>
	/// Checks every second for shooting a random bullet.
	/// </summary>
	/// <returns></returns>
	protected bool RandomShootChance() {
		if(randomShootChanceTimeElapsed > 1) {
			randomShootChanceTimeElapsed = 0;
			if(Random(0f, 100f) < shotChancePerFrame) {
				return true;
			}
		} else {
			randomShootChanceTimeElapsed += GetTime;
		}
		return false;
	}
	/// <summary>
	/// Needs to be called in Update as long as the tank is moving along a path.
	/// This function reduces and updates the current active path without calculating a new path to save performance.
	/// </summary>
	public int Random(int min, int max) => Randomizer.Range(min, max);
	public float Random(float min, float max) => Randomizer.Range(min, max);
	public List<Transform> PredictBulletPath() {
		RaycastHit lastHit = new RaycastHit();
		Ray ray = new Ray(bulletOutput.position, tankHead.forward);
		var hitList = new List<Transform>();
		for (int i = 0; i < 2; i++) {
			if (i > 0) {
				ray = new Ray(lastHit.point, Vector3.Reflect(ray.direction, lastHit.normal));
			}

			if (Physics.BoxCast(ray.origin, Bullet.bulletSize, ray.direction, out lastHit, Quaternion.identity, Mathf.Infinity, HitLayers)) {
				if (debugMode) Draw.Line(ray.origin, lastHit.point, Color.yellow);
				hitList.Add(lastHit.transform);
			}
		}
		return hitList;
	}
	public static Vector3[] GetDirs(int scope, float angleOffset = 0) {
		Vector3[] dirs = new Vector3[scope];
		float angle = 0;
		angleOffset = Mathf.Deg2Rad * angleOffset;

		for (int i = 0; i < scope; i++) {
			float x = Mathf.Sin(angle + angleOffset);
			float y = Mathf.Cos(angle + angleOffset);
			angle += 2 * Mathf.PI / scope;
			dirs[i] = new Vector3(x, 0, y);
		}
		return dirs;
	}
	public Bullet[] GetBulletsOnScreen(GameObject exception, float radius) {
		var colls = Physics.OverlapSphere(Pos, radius, BulletLayer);
		List<Bullet> bullets = new List<Bullet>();
		foreach(var c in colls) {
			if(c.TryGetComponent(out Bullet bullet) && bullet.Owner != exception) {
				bullets.Add(bullet);
			}
		}
		return bullets.ToArray();
	}
	public static Vector3 GetDirectionIndex(int step, int precision) {
		float angle = step > 0 ? 2 * Mathf.PI / precision * step : 0;
		float x = Mathf.Sin(angle);
		float y = Mathf.Cos(angle);
		return new Vector3(x, 0f, y);
	}
	public bool RequestAttack(float borrowTime) {
		if((Token == null || Token.inUse == false) && AIManager.RequestToken(borrowTime, out Token)) {
			return true;
		}
		return false;
	}
	#endregion

	#region Pathfinding
	public async UniTask<bool> RandomPathAsync(Vector3 origin, float radius, float? minRadius = null, bool requireSightContact = false, bool improvePath = false) {
		try {
			isCalculatingPath = true;
			AIGrid.FreeAllHolderCells(hashcode);
			var points = AIGrid.GetPointsWithinRadius(Pos, radius, minRadius).ToList();
			
			points = points.OrderByDescending(v => Vector3.Distance(origin, v)).ToList();
			if (requireSightContact) {
				points = points.Where(p => Physics.Linecast(p, Pos, MapLayers) == false).ToList();
			}
			if (points.Count > 0) {
				targetMovingPoint = points.RandomItem();
				return await FindPathAsync(targetMovingPoint, improvePath);
			}
			isCalculatingPath = false;
		} catch(System.Exception e) {
			Logger.LogError(e, "Failed to calculate random async path.");
        }
		return false;
	}
	protected async UniTask<bool> FindPathAsync(Vector3 target, bool improvePath = false) {
		try {
			keepMoving = true;
			isCalculatingPath = true;
			AIGrid.FreeAllHolderCells(hashcode);
			var currentPath = await AIGrid.FindPathAsync(Pos, target);

			// Filter the Path
			if (improvePath) {
				List<Vector3> visibleNodes = new List<Vector3>();
				for (int i = 0; i < currentPath.Length; i++) {
					if (Physics.SphereCast(Pos, 1f, (currentPath[i] - Pos).normalized, out RaycastHit hit, Vector3.Distance(currentPath[i], Pos), HitLayers) == false) {
						visibleNodes.Add(currentPath[i]);
					}
				}
				if (visibleNodes.Count > 0) {
					visibleNodes.RemoveAt(visibleNodes.Count - 1);
					currentPath = currentPath.ToList().Except(visibleNodes).ToArray();
				}
			}

			// Return results
			keepMoving = false;
			isCalculatingPath = false;
			if (currentPath.Length > 1) {
				finalPathDestination = currentPath[currentPath.Length - 1];
				targetMovingPoint = currentPath[0];
				Path = currentPath;
				UnmodifiedPath = currentPath;
				UsePath(true);
				return true;
			}
		} catch(System.Exception e) {
			Logger.LogError(e, "Failed to calculate async path.");
        }
		return false;
	}
	protected async UniTask<bool> PathExistsAsync(Vector3 from, Vector3 to) {
		try {
			var path = await AIGrid.FindPathAsync(from, to);
			if (path == null || path.Length == 0) {
				return false;
			}
			return true;
		} catch(System.Exception e) {
			Logger.LogError(e, "Failed to check if path is valid.");
        }
		return false;
	}
	protected async UniTask<bool> FleeFromAsync(Vector3 target, float fleeRadius, float? minRadius = null) {
		try {
			var points = AIGrid.GetPointsWithinRadius(Pos, fleeRadius, minRadius);
			List<(Vector3 point, float distance)> distPoints = new List<(Vector3, float)>();

			foreach (Vector3 p in points) {
				var diff = target - p;
				distPoints.Add((p, diff.x * diff.x + diff.z * diff.z));
			}

			distPoints = distPoints.Where(p => Physics.Linecast(p.point, Target.Pos, MapLayers)).OrderByDescending(p => p.distance).ToList();
			if (distPoints.Count > 0) {
				targetMovingPoint = distPoints.RandomItem().point;
				return await FindPathAsync(targetMovingPoint);
			} else {
				return false;
			}
		} catch(System.Exception e) {
			Logger.LogError(e, "Failed to calculate a flee path.");
        }
		return false;
	}
	protected async UniTask<bool> FindPathToPlayerAsync(bool improvePath = false) {
		try {
			if (await FindPathAsync(Target.Pos, improvePath)) {
				return true;
			}
		} catch(System.Exception e) {
			Logger.LogError(e, "Failed to find async path to player.");
        }
		return false;
	}
	protected float GetPathLength(Vector3[] path, Vector3 pos) {
		float pathLength = 0;
		if (path.Length > 1) {
			Vector3 last = path[0];
			for (int i = 0; i < path.Length; i++) {
				pathLength += Mathf.Abs((last - path[i]).sqrMagnitude);
				last = path[i];
			}
		} else if(path.Length == 1) {
			pathLength = (pos - path[0]).sqrMagnitude;
        }
		pathLength = Mathf.Sqrt(pathLength);
		return pathLength;
    }
	public float GetPathLengthFromPos(Vector3[] path, Vector3 pos) {
		float pathLength = 0;
		try {
			if (path != null && path.Length > 1) {
				Vector3 last = path[0];
				pathLength += Mathf.Abs((pos - path[0]).sqrMagnitude);
				for (int i = 0; i < path.Length; i++) {
					pathLength += Mathf.Abs((last - path[i]).sqrMagnitude);
					last = path[i];
				}
			} else if (path != null && path.Length == 1) {
				pathLength = (path[0] - pos).sqrMagnitude;
			} else if (path != null && path.Length == 2) {
				pathLength = (path[0] - path[1]).sqrMagnitude;
				pathLength += (path[1] - pos).sqrMagnitude;
			}
			pathLength = Mathf.Sqrt(pathLength);
		} catch(System.Exception e) {
			Logger.LogError(e, "Failed to calculate total path distance.");
        }
		return pathLength;
	}
	private void UsePath(bool ignoreCheck = false) {
		if(PathNodeCount > 0) {
			try {
				if (Vector3.Distance(Pos, Path[0]) < 1.5f) {
					var list = Path.ToList();
					list.RemoveAt(0);
					Path = list.ToArray();
					if (PathNodeCount > 0) {
						targetMovingPoint = Path[Path.Length - 1];
					}
				}
				if (ignoreCheck == false) {
					CheckPathAvailability();
				}
			} catch(System.Exception e) {
				Logger.LogError(e, "Failed to use path");
            }
		}
	}
	private void CheckPathAvailability() {
		try {
			bool isPathStillValid = true;
			foreach (var p in Path) {
				if (AIGrid.IsPointWalkable(p) == false) {
					isPathStillValid = false;
					break;
				}
			}
			if(HasSightContact(nextMoveTarget, MapLayers, 0.1f) == false) {
				isPathStillValid = false;
            }
			if (PathNodeCount >= 1 && RemainingPathDistance >= 4) {
				if (isPathStillValid == false && isCalculatingPath == false) {
					_ = FindPathAsync(finalPathDestination);
				}
			}
		} catch(System.Exception e) {
			Logger.LogError(e, "Failed to check path availability.");
        }
    }
	#endregion

}
