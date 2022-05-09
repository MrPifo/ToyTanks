using Cysharp.Threading.Tasks;
using Sperlich.Types;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using static Sperlich.Debug.Draw.Draw;

[RequireComponent(typeof(DecisionRequester))]
[RequireComponent(typeof(TankAI))]
public class MLTank : Agent {

    protected TankAI tankAI;
	protected DecisionRequester requester;
	protected BehaviorParameters nnParams;

	public int movePrecision = 32;
	public int colliderSensors = 8;
	public int chunkSize = 5;
	public float bulletDetectionRadius = 20;
	public float wallTouchDistance = 1.8f;
	public float wallTouchTimeKill = 3f;
	public float angleToGoalRewardTreshold = 40;
	public float sensorMaxDistance = 30;
	public float chunkDecayTime = 30;
	public readonly Vector2Int exploreChunkArea = new Vector2Int(6, 5);

	[Header("Neural Configs")]
	public float timeUntilGoalStuck = 10;
	public bool enableNNActions;

    [Header("Neural Input")]
    public int nnMoveDir;
    public bool nnMoveStop;

	#region Observations
	public Vector3 velocity => tankAI.Velocity;
	public Vector3 EnemyDir => (tankAI.Target.Pos - tankPosition).normalized;
	public Vector3 tankPosition => tankAI.Pos;
	[Header("Observations")]
	public Vector3 goalPosition;
	public float angleToGoal;
	public float wallTouchTime;
	public float lookAtGoalAngle;
	public float minDistance;
	public bool directSightContactToGoal;
	public const int maxDetectBullet = 4;
	float furthestChunkDistance;
	float[] sensorDistances;
	RayInfo[] nearbyBullets;
	HashSet<Chunk> visitedChunks = new HashSet<Chunk>();
	Dictionary<Int2, Chunk> exploredChunks = new Dictionary<Int2, Chunk>();
	Dictionary<Int2, (bool available, bool validIndex)> neighChunks;
	#endregion

	[Header("Information")]
	public float distanceToGoal;
	public float timeWithoutAnyGoalProgress;

	private bool debug => tankAI.debugMode;
	public bool IsStuckCantReachGoal => timeWithoutAnyGoalProgress > timeUntilGoalStuck;
	public static LayerMask MapLayers = LayerMaskExtension.Create(GameMasks.Block, GameMasks.Destructable, GameMasks.BulletTraverse, GameMasks.LevelBoundary);

    void Awake() {
		tankAI = GetComponent<TankAI>();
		nnParams = GetComponent<BehaviorParameters>();
		requester = GetComponent<DecisionRequester>();
		Disable();
	}

    public new void Initialize() {
		tankAI.SetMovement(TankAI.MovementType.MoveDir);
		Enable();
	}

    void FixedUpdate() {
		if (enableNNActions) {
			tankAI.targetMoveDir = TankAI.GetDirectionIndex(nnMoveDir, movePrecision);

			CollectInformation();
		}
	}

	void Update() {
		if(enableNNActions && debug) {
			PaintChunks();
			DisplayDebugs();
		}
	}

	void CollectInformation() {
		ReachGoal();
		SensorWalls();
		DetectBullets();
		CheckChunks();
	}
	void ReachGoal() {
		distanceToGoal = Vector3.Distance(tankPosition, goalPosition);  // Inference not required

		if (Physics.Linecast(tankPosition, goalPosition, MapLayers) == false) {
			float distance = Vector3.Distance(tankPosition, goalPosition);  // Can be optimized without Sqrt()
			directSightContactToGoal = true;

			// Only Reward the Agent if close to goal and looking at at goal
			if (distance < minDistance && Vector3.Angle(velocity, (goalPosition - tankPosition).normalized) < angleToGoalRewardTreshold) {
				minDistance = distance;
				timeWithoutAnyGoalProgress = 0;
			}
		} else {
			timeWithoutAnyGoalProgress += Time.fixedDeltaTime;
			directSightContactToGoal = false;
		}
	}
	void SensorWalls() {
		Vector3[] dirs = TankAI.GetDirs(colliderSensors);  // can be saved as a one time
		sensorDistances = new float[colliderSensors];

		for (int i = 0; i < dirs.Length; i++) {
			if (Physics.Raycast(tankPosition, dirs[i], out RaycastHit hit, Mathf.Infinity, MapLayers)) {
				sensorDistances[i] = Vector3.Distance(tankPosition, hit.point);
			} else {
				sensorDistances[i] = -1;
			}
		}
		if (sensorDistances.Any(d => d < wallTouchDistance)) {
			wallTouchTime += Time.fixedDeltaTime;
		} else {
			wallTouchTime = Mathf.Lerp(wallTouchTime, 0, Time.fixedDeltaTime / 2f);
		}
		wallTouchTime = Mathf.Round(wallTouchTime * 100f) / 100f;
	}
	void DetectBullets() {
		var bullets = tankAI.GetBulletsOnScreen(gameObject, bulletDetectionRadius);
		nearbyBullets = new RayInfo[maxDetectBullet];

		for (int i = 0; i < maxDetectBullet; i++) {
			nearbyBullets[i] = new RayInfo(Vector3.zero, Vector3.zero, -1);
			if (i < bullets.Length) {
				nearbyBullets[i] = new RayInfo(bullets[i].CurrentDirection, bullets[i].Pos, Vector3.Distance(bullets[i].Pos, tankPosition));
			}
		}
	}
	void CheckChunks() {
		Int2 currentIndex = Chunk.WorldToChunkPos(Vector3.zero, tankPosition, chunkSize);
		// Marks the current chunk as visited
		if (exploredChunks.ContainsKey(currentIndex) && exploredChunks[currentIndex].explored == false) {
			exploredChunks[currentIndex].explored = true;
			exploredChunks[currentIndex].time = chunkDecayTime;
			// Add to visited list
			if (visitedChunks.Contains(exploredChunks[currentIndex]) == false) {
				visitedChunks.Add(exploredChunks[currentIndex]);
			}
		}

		// Check if the chunks time has run out
		HashSet<Chunk> remove = new HashSet<Chunk>();
		foreach (var c in visitedChunks) {
			c.time -= Time.fixedDeltaTime;
			// Reset chunks explored status
			if (c.time < 0) {
				c.time = 0;
				c.explored = false;
				remove.Add(c);
			}
		}
		// Remove from visited list
		foreach (var c in remove)
			visitedChunks.Remove(c);

		// Fetch and check if adjacent neighbours are reachable
		neighChunks = new Dictionary<Int2, (bool available, bool validIndex)>();
		foreach (var c in GetNeighbours(exploredChunks, currentIndex)) {
			if (c.chunkSize == 0) {
				neighChunks.Add(c.index, (false, false));
				continue;
			}
			if (Physics.Linecast(tankPosition, c.Position, MapLayers)) {
				neighChunks.Add(c.index, (false, true));
			} else {
				neighChunks.Add(c.index, (true, true));
			}
		}
	}
	public void ResetParameters() {
		nearbyBullets = Enumerable.Repeat(new RayInfo(Vector3.zero, Vector3.zero, -1), maxDetectBullet).ToArray();
		sensorDistances = new float[colliderSensors];
		minDistance = 1000;
		lookAtGoalAngle = 0;
		wallTouchTime = 0;
    }
	void PaintChunks() {
		foreach (KeyValuePair<Int2, Chunk> ch in exploredChunks) {
			if (ch.Value is null) continue;
			if (ch.Value.WorldToChunkPos(tankPosition, chunkSize) == ch.Value.index) {
				Cube(ch.Value.Position, new Color(0.2f, 0.2f, 1f, 0.8f), new Vector3(ch.Value.chunkSize - 0.25f, 0.05f, ch.Value.chunkSize - 0.25f), true);
			} else {
				Cube(ch.Value.Position, ch.Value.explored ? (new Color(0.8f, 0f, 0f, Normalize(ch.Value.time, 0f, chunkDecayTime))) : new Color(0, 0f, 0f, 0.8f), new Vector3(ch.Value.chunkSize - 0.25f, 0.05f, ch.Value.chunkSize - 0.25f), true);
			}
		}
		if (neighChunks != null && neighChunks.Count > 0) {
			foreach (KeyValuePair<Int2, (bool available, bool validIndex)> ch in neighChunks) {
				if (ch.Value.validIndex) {
					Line(tankPosition, exploredChunks[ch.Key].Position, 0.85f, ch.Value.available ? Color.yellow : Color.red);
				}
			}
		}
	}
	void DisplayDebugs() {
		for (int i = 0; nearbyBullets != null && i < nearbyBullets.Length; i++) {
			if (nearbyBullets[i] != null && nearbyBullets[i].distance != -1) {
				Line(tankPosition, tankPosition + (nearbyBullets[i].pos - tankPosition).normalized * nearbyBullets[i].distance, Color.red);
				Sphere(nearbyBullets[i].pos, 0.4f, Color.Lerp(Color.red, Color.yellow, nearbyBullets[i].distance.Remap(2f, 30f, 0f, 1f)));
			}
		}
		Line(tankPosition, goalPosition, 4, Color.green);
	}
	/// <summary>
	/// Returns if the agent is stuck. If true this is reset to false.
	/// </summary>
	/// <returns></returns>
	public bool IsStuck() {
		if (IsStuckCantReachGoal) {
			timeWithoutAnyGoalProgress = 0;
		}
		return IsStuckCantReachGoal;
	}

	public override void OnActionReceived(ActionBuffers actions) {
		if (enableNNActions) {
			nnMoveDir = actions.DiscreteActions[0];
		}
	}

	public override void CollectObservations(VectorSensor sensor) {
		if (enableNNActions) {
			// Normalized Velocity of the tank (Direction)
			sensor.AddObservation(velocity.Vector2XZ());

			// Direction to Goal, if not in sight = zero (Direction)
			sensor.AddObservation(directSightContactToGoal ? (goalPosition - tankPosition).normalized.Vector2XZ() : Vector2.zero);

			// Normalized info how long a wall has been touched (Float Time)
			sensor.AddObservation(Mathf.Round(Normalize(wallTouchTime, 0f, wallTouchTimeKill) * 100f) / 100f);
			// Environment Raycasts (Distance)
			for (int i = 0; i < colliderSensors; i++) {
				sensor.AddObservation(Normalize(sensorDistances[i], wallTouchDistance, sensorMaxDistance));    // Can be Sqrt() to improve perf.
			}

			// Info of bullets on screen (Velocity, Distance)
			for (int i = 0; i < nearbyBullets.Length; i++) {
				sensor.AddObservation(nearbyBullets[i].velocity.Vector2XZ());
				sensor.AddObservation(Normalize(nearbyBullets[i].distance, 0, bulletDetectionRadius));
			}

			// Info of all moveable chunks
			foreach (KeyValuePair<Int2, Chunk> ch in exploredChunks) {
				// Get direction to chunk
				Vector2 dir = (ch.Value.Position - tankPosition).Vector2XZ();
				// Normalize direction against furthest chunk
				dir = new Vector2(Normalize(dir.x, 0f, furthestChunkDistance), Normalize(dir.y, 0f, furthestChunkDistance));
				sensor.AddObservation(ch.Value.explored);
				sensor.AddObservation(dir);
			}

			// Info of all 8 adjacenting chunks
			foreach (var ch in neighChunks) {
				if (ch.Value.validIndex) {
					// Get direction to chunk
					Vector2 dir = (exploredChunks[ch.Key].Position - tankPosition).Vector2XZ();
					// Normalize direction against furthest chunk
					dir = new Vector2(Normalize(dir.x, 0f, furthestChunkDistance), Normalize(dir.y, 0f, furthestChunkDistance));
					sensor.AddObservation(ch.Value.available);
					sensor.AddObservation(dir);
				} else {
					// Add empty Chunk if outside of grid
					sensor.AddObservation(false);
					sensor.AddObservation(new Vector2(0, 0));
				}
			}
		}
	}

	public async UniTask SetRandomGoalAsync(Vector3 origin, float radius, float? minRadius = null, bool requireSightContact = false) {
		await UniTask.SwitchToThreadPool();
		var points = AIGrid.GetPointsWithinRadius(tankPosition, radius, minRadius).OrderByDescending(v => Vector3.Distance(origin, v)).ToList();

		if (requireSightContact) {
			await UniTask.SwitchToMainThread();
			points = points.Where(p => Physics.Linecast(p, tankPosition, TankAI.MapLayers) == false).ToList();
		}

		goalPosition = points.RandomItem();
	}

	public void ResetChunks() {
		visitedChunks = new HashSet<Chunk>();
		FillChunks(out exploredChunks, Vector3.zero, exploreChunkArea, chunkSize);
		furthestChunkDistance = FurthestChunk(exploredChunks.Select(c => c.Value).ToArray());
	}
	public void Enable() {
		requester.enabled = true;
		enableNNActions = true;
		
	}
	public void Disable() {
		requester.enabled = false;
		enableNNActions = false;
	}
	public float Normalize(float value, float minValue, float maxValue) {
		value = Mathf.Clamp(value, minValue, maxValue);
		return (value - minValue) / (maxValue - minValue);
	}
	public Vector3 Normalize(Vector3 vec, float min, float max) => new Vector3(Normalize(vec.x, min, max), Normalize(vec.y, min, max), Normalize(vec.z, min, max));
	public Vector2 Normalize(Vector2 vec, float min, float max) => new Vector2(Normalize(vec.x, min, max), Normalize(vec.y, min, max));
	public static void FillChunks(out Dictionary<Int2, Chunk> chunks, Vector3 worldSpaceOrigin, Vector2Int size, int chunkSize) {
		chunks = new Dictionary<Int2, Chunk>();

		for (int x = -size.x; x < size.x; x++) {
			for (int y = -size.y; y < size.y; y++) {
				chunks.Add(new Int2(x, y), new Chunk(worldSpaceOrigin, new Int2(x, y), chunkSize));
			}
		}
	}
	public static float FurthestChunk(Chunk[] chunks) {
		float distance = 1;
		foreach (var c in chunks) {
			float dist = Vector3.Distance(c.origin, c.Position);
			if (dist > distance) {
				distance = dist;
			}
		}
		return distance;
	}
	public static Chunk[] GetNeighbours(Dictionary<Int2, Chunk> chunks, Int2 originIndex) {
		Chunk[] nChunks = new Chunk[8];
		int count = 0;

		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				Int2 index = originIndex + new Int2(x, y);
				if (x == 0 && y == 0) continue;
				if (chunks.ContainsKey(index)) {
					nChunks[count] = chunks[index];
				} else {
					nChunks[count] = new Chunk(Vector3.zero, index, 0);
				}
				count++;
			}
		}
		return nChunks;
	}

	public class RayInfo {
		public float distance;
		public Vector3 pos;
		public Vector3 velocity;

		public RayInfo(Vector3 vel, Vector3 pos, float distance) {
			this.distance = distance;
			this.pos = pos;
			velocity = vel;
		}
		public RayInfo(Vector2 vel, Vector2 pos, float distance) {
			this.distance = distance;
			this.pos = new Vector3(pos.x, 0, pos.y);
			velocity = new Vector3(vel.x, 0, vel.y);
		}
	}
	public class Chunk {

		public float time;
		public bool explored = false;
		public readonly int chunkSize;
		public readonly Vector3 origin;
		public readonly Int2 index;
		public readonly Vector3 Position;

		public Chunk(Vector3 worldSpaceOrigin, Int2 index, int chunkSize) {
			origin = worldSpaceOrigin;
			this.index = index;
			this.chunkSize = chunkSize;
			Position = origin + new Vector3(index.x * chunkSize, 0, index.y * chunkSize);
		}

		public Int2 WorldToChunkPos(Vector3 pos, int chunkSize) {
			return new Int2(Mathf.FloorToInt((pos.x - origin.x + chunkSize / 2f) / chunkSize), Mathf.FloorToInt((pos.z - origin.z + chunkSize / 2f) / chunkSize));
		}
		public static Int2 WorldToChunkPos(Vector3 gridOrigin, Vector3 pos, int chunkSize) {
			return new Int2(Mathf.FloorToInt((pos.x - gridOrigin.x + chunkSize / 2f) / chunkSize), Mathf.FloorToInt((pos.z - gridOrigin.z + chunkSize / 2f) / chunkSize));
		}
	}
}
