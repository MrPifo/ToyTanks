using NesScripts.Controls.PathFind;
using Sperlich.Debug.Draw;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using CommandTerminal;
using System.Collections;
using Cysharp.Threading.Tasks;
using static Sperlich.Debug.Draw.Draw;
using Sperlich.Types;

public class AIGrid : MonoBehaviour {

	public static GridSizes gridSize;
	public static GridExt Grid { get; set; }
	//static JumpPointParam jpParam;
	static Vector3 debugSize = new Vector3(0.5f, 0.5f, 0.5f);

	[RegisterCommand(Help = "Generates the grid for the AI")]
	static void GenerateGrid(CommandArg[] args) {
		AIManager.AIGrid = new AIGrid();
		//Game.ActiveGrid.GenerateGrid();
	}
	[RegisterCommand(Help = "Shows the AIGrid")]
	static void ShowGrid(CommandArg[] args) {
		AIManager.showGrid = true;
	}
	[RegisterCommand(Help = "Hides the AIGrid")]
	static void HideGrid(CommandArg[] args) {
		AIManager.showGrid = false;
	}

	public static void GenerateGrid(GridSizes _size, LayerMask mask) {
		gridSize = _size;
		Vector3Int size = LevelManager.GetGridBoundary(_size);
		Grid = new GridExt(new Vector3(-size.x * 2 - 0.5f, 0, -size.z * 2 - 0.5f), size.x * 4, size.z * 4);

		for(int x = 0; x < Grid.Width; x++) {
			for(int y = 0; y < Grid.Height; y++) {
				List<RaycastHit> hits = new List<RaycastHit>(Physics.SphereCastAll(Grid.GetWorldPos(x, y), 0.8f, Vector3.down, 1, mask.value));
				if(hits.Any(h => h.Layer() == LayerMask.NameToLayer("BulletTraverse"))) {
					Grid.SetAttribute(GridExt.Attributes.Locked, x, y);
				}
				if(hits.Any(h => h.Layer() == LayerMask.NameToLayer("Block"))) {
					Grid.SetAttribute(GridExt.Attributes.Locked, x, y);
				}
					
				if(LevelGround.GetTileAtWorldPos(Grid.GetWorldPos(x, y), out GroundTile tile) && GroundTile.IsAnyGapTile(tile.type)) {
					Grid.SetAttribute(GridExt.Attributes.Locked, x, y);
				}
				if(Grid.IsWalkableAt(x, y) && hits.Any(h => h.Layer() == LayerMask.NameToLayer("Destructable"))) {
					RaycastHit h = hits.Find(h => h.Layer() == LayerMask.NameToLayer("Destructable"));
					Grid.SetAttribute(GridExt.Attributes.Reserved, x, y, h.transform.gameObject.GetHashCode());
					Destructable dest = hits.Where(h => h.transform.TryGetComponent(out Destructable _)).First().transform.GetComponent<Destructable>();
					dest.occupiedIndexes.Add(new Int2(x, y));
				}
			}
		}
	}

	void Update() {
		if(AIManager.showGrid && Grid != null) {
			foreach(var at in Grid.NodeAttributes) {
				switch (at.Key) {
					case GridExt.Attributes.Locked:
						foreach(var i in at.Value) {
							Sphere(Grid.GetWorldPos(i.x, i.y), 0.4f, Color.grey, true);
						}
						break;
					case GridExt.Attributes.Reserved:
						foreach (var i in at.Value) {
							Sphere(Grid.GetWorldPos(i.x, i.y), 0.4f, Color.blue, true);
						}
						break;
					default:
						break;
				}
			}
			foreach(var k in Grid.nodes) {
				if(Grid.HasAttribute(k.gridX, k.gridY) == false) {
					if (Grid.IsWalkableAt(k.gridX, k.gridY)) {
						Sphere(Grid.GetWorldPos(k.gridX, k.gridY), 0.25f, Color.white, true);
					} else {
						Sphere(Grid.GetWorldPos(k.gridX, k.gridY), 0.25f, Color.red, true);
					}
				}
			}
		}
		if(Grid != null) {
			if(Input.GetKeyDown(KeyCode.F8) && AIManager.showGrid == false) {
				AIManager.showGrid = true;
			} else if(Input.GetKeyDown(KeyCode.F8)) {
				AIManager.showGrid = false;
			}
		}
	}

	public static async UniTask<Vector3[]> FindPathAsync(Vector3 start, Vector3 end, bool ignoreWeight = true, Pathfinding.DistanceType distanceType = Pathfinding.DistanceType.Euclidean) {
		await UniTask.SwitchToThreadPool();
		Int2? startGridPos = Grid.GetGridPosSmart(start);
		Int2? endGridPos = Grid.GetGridPosSmart(end);
		
		try {
			if (startGridPos.HasValue && endGridPos.HasValue) {
				var path = Pathfinding.FindPath(Grid, new Point(startGridPos.Value.x, startGridPos.Value.y), new Point(endGridPos.Value.x, endGridPos.Value.y), distanceType, ignoreWeight);

				Vector3[] poses = new Vector3[path.Count];
				for (int i = 0; i < poses.Length; i++) {
					poses[i] = Grid.GetWorldPos(path[i].x, path[i].y);
				}
				await UniTask.SwitchToMainThread();
				return poses;
			}
		} catch (System.Exception e) {
			Logger.LogError(e, "Failed to calculate async path in AI-Grid.");
		} finally {
			await UniTask.SwitchToMainThread();
		}
		return new Vector3[0];
	}

	public static void DrawPathLines(Vector3[] nodeList) {
		if(nodeList.Length > 1) {
			Vector3 last = nodeList[0];
			for(int i = 0; i < nodeList.Length; i++) {
				if(i == 0) {
					Draw.Sphere(nodeList[i], 0.4f, Color.yellow);
				} else if(i == nodeList.Length - 1) {
					Draw.Sphere(nodeList[i], 0.4f, Color.cyan);
				} else {
					Draw.Sphere(nodeList[i], 0.4f, Color.Lerp(Color.yellow, Color.cyan, ExtensionMethods.Remap(i, 0, nodeList.Length, 0f, 1f)));
				}
				Draw.Line(nodeList[i], last, 4f, Color.black);
				last = nodeList[i];
			}
		} else if(nodeList.Length == 1) {
			Draw.Sphere(nodeList[0], 0.4f, Color.cyan);
		}
	}

	public static Vector3[] GetPointsWithinRadius(Float3 origin, float radius, float? minRadius = null) {
		Int2 index = Grid.GetGridPos(origin);
		List<Vector3> poses = new List<Vector3>();

		int nodeRadius = Mathf.RoundToInt(radius);
		float radiusSqr = radius * radius;
		float minRadiusSqr = 0;
		if(minRadius != null) {
			minRadiusSqr = (float)(minRadius * minRadius);
		}
		int its = 0;
		try {
			for (int x = -nodeRadius; x < nodeRadius; x++) {
				for (int y = -nodeRadius; y < nodeRadius; y++) {
					if (Grid.HasIndex(index.x + x, index.y + y) && Grid.IsWalkableAt(index.x + x, index.y + y)) {
						var indexPos = Grid.GetWorldPos(index.x + x, index.y + y);
						Float3 diff = origin - indexPos;
						float sum = diff.x * diff.x + diff.z * diff.z;
						if (sum < radiusSqr && sum > minRadiusSqr) {
							poses.Add(indexPos);
							its++;
						}
					}
				}
			}
		} catch (System.StackOverflowException e) {
			Logger.LogError(e, "GetPointsWithinRadius caused a StackOverflowException. Iterations: " + its);
		}
		return poses.ToArray();
	}

	public static void PaintCellAt(Vector3 pos, Color32 color, float duration = 0.05f) => AIManager.AIGrid.PaintCells(new Vector3[1] { pos }, color, duration);
	public void PaintCells(Vector3[] poses, Color32 color, float duration = 0.05f) {
		StartCoroutine(IRepeat());
		IEnumerator IRepeat() {
			float t = 0;
			Dictionary<Int2, Color> customCellColors = new Dictionary<Int2, Color>();
			for(int i = 0; i < poses.Length; i++) {
				Int2 pos = Grid.GetGridPos(poses[i]);
				if(customCellColors.ContainsKey(pos) == false) {
					customCellColors.Add(pos, color);
				}
			}
			while(t < duration) {
				foreach(var pos in customCellColors) {
					Cube(Grid.GetWorldPos(pos.Key.x, pos.Key.y), pos.Value, debugSize);
				}
				t += Time.deltaTime;
				yield return null;
			}
		}
}
	/// <summary>
	/// This cell is neither Reserved, Locked, or occupied. It is a white walkable cell.
	/// </summary>
	/// <param name="pos"></param>
	/// <returns></returns>
	public static bool IsPointWalkable(Vector3 pos) {
		Int2 index = Grid.GetGridPos(pos);
		return Grid.IsWalkableAt(index.x, index.y);
    }
	/// <summary>
	/// This cell is neither Reserved, Locked, or occupied. It is a white walkable cell.
	/// </summary>
	/// <param name="pos"></param>
	/// <returns></returns>
	public static bool IsPointWalkable(Int2 pos) {
		return Grid.IsWalkableAt(pos.x, pos.y);
	}
	public static void FreeAllHolderCells(GameObject gameObject) => FreeAllHolderCells(gameObject.GetHashCode());
	public static void FreeAllHolderCells(int holder) {
		try {
			var list = Grid.ReservedNodes.Where(t => t.Value == holder).ToList();
			foreach (var at in list) {
				Int2 index = at.Key;
				Grid.RemoveAttribute(GridExt.Attributes.Reserved, index.x, index.y, holder);
			}
		} catch(System.Exception e) {
			Logger.LogError(e, "Failed to free cells.");
        }
	}
}
