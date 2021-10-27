using EpPathFinding.cs;
using Sperlich.Debug.Draw;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using CommandTerminal;
using SimpleMan.Extensions;
using System.Collections;

public class AIGrid : MonoBehaviour {

	public GridSizes gridSize;
	public bool visualize => Game.showGrid;
	public StaticGrid Grid { get; set; }
	JumpPointParam jpParam;
	public const int cellSize = 2;
	static Vector3 debugSize = new Vector3(1.5f, 0.1f, 1.5f);

	[RegisterCommand(Help = "Generates the grid for the AI")]
	static void GenerateGrid(CommandArg[] args) {
		Game.ActiveGrid = new AIGrid();
		//Game.ActiveGrid.GenerateGrid();
	}
	[RegisterCommand(Help = "Shows the AIGrid")]
	static void ShowGrid(CommandArg[] args) {
		Game.showGrid = true;
	}
	[RegisterCommand(Help = "Hides the AIGrid")]
	static void HideGrid(CommandArg[] args) {
		Game.showGrid = false;
	}

	public void GenerateGrid(GridSizes _size, LayerMask mask) {
		gridSize = _size;
		Vector3Int size = LevelManager.GetGridBoundary(_size);
		Grid = new StaticGrid(size.x * cellSize, size.z * cellSize);
		jpParam = new JumpPointParam(Grid, EndNodeUnWalkableTreatment.ALLOW, DiagonalMovement.IfAtLeastOneWalkable, HeuristicMode.MANHATTAN);
		Grid.WorldPos = new Vector3(-size.x * cellSize, 0, -size.z * cellSize);
		Grid.CellSize = cellSize;

		for(int x = 0; x < Grid.width; x++) {
			for(int y = 0; y < Grid.height; y++) {
				Grid.SetWalkableAt(x, y, true);
				List<RaycastHit> hits = new List<RaycastHit>(Physics.SphereCastAll(Grid.GetWorldPos(x, y), 0.8f, Vector3.down, 1, mask.value));
				if(hits.Any(h => h.Layer() == LayerMask.NameToLayer("BulletTraverse"))) {
					Grid.LockAt(x, y);
				}
				if(hits.Any(h => h.Layer() == LayerMask.NameToLayer("Block"))) {
					Grid.LockAt(x, y);
				}
				if(Grid.IsLockedAt(x, y) == false && hits.Any(h => h.Layer() == LayerMask.NameToLayer("Destructable"))) {
					RaycastHit h = hits.Find(h => h.Layer() == LayerMask.NameToLayer("Destructable"));
					Grid.SetReserved(x, y, true, h.transform.gameObject.GetHashCode());
					Destructable dest = hits.Where(h => h.transform.TryGetComponent(out Destructable _)).First().transform.GetComponent<Destructable>();
					dest.occupiedIndexes.Add(new GridPos(x, y));
				}
			}
		}
	}

	void Update() {
		if(visualize && Grid != null) {
			for(int x = 0; x < Grid.width; x++) {
				for(int y = 0; y < Grid.height; y++) {
					if(Grid.IsLockedAt(x, y)) {
						Draw.Cube(Grid.GetWorldPos(x, y), Color.gray, debugSize, true);
					} else if(Grid.IsReserved(x, y)) {
						Draw.Cube(Grid.GetWorldPos(x, y), Color.blue, debugSize);
					} else if(Grid.IsWalkableAt(x, y) == false) {
						Draw.Cube(Grid.GetWorldPos(x, y), Color.red, debugSize, true);
					} else {
						Draw.Cube(Grid.GetWorldPos(x, y), Color.white, debugSize, true);
					}
				}
			}
		}
	}

	public Vector3[] FindPath(Vector3 start, Vector3 end, DiagonalMovement diagonal = DiagonalMovement.Always) {
		if(diagonal != jpParam.DiagonalMovement) {
			jpParam.DiagonalMovement = diagonal;
		}

		bool mirrorSearch = false;
		/*if((start - end).normalized.x >= 0) {
			mirrorSearch = true;
		}*/
		GridPos startGridPos = Grid.GetGridPosSmart(start, mirrorSearch);
		GridPos endGridPos = Grid.GetGridPosSmart(end, mirrorSearch);

		if(startGridPos != null && endGridPos != null) {
			jpParam.Reset(startGridPos, endGridPos);

			var path = JumpPointFinder.FindPath(jpParam);

			Vector3[] poses = new Vector3[path.Count];
			for(int i = 0; i < poses.Length; i++) {
				poses[i] = Grid.GetWorldPos(path[i].x, path[i].y);
			}
			return poses;
		}
		return new Vector3[0];
	}

	public static void DrawPathLines(Vector3[] nodeList) {
		if(nodeList.Length > 1) {
			Vector3 last = nodeList[0];
			Draw.Sphere(nodeList[0], 0.25f, Color.yellow);
			for(int i = 1; i < nodeList.Length; i++) {
				Draw.Sphere(nodeList[i], 0.25f, Color.red);
				Draw.Line(nodeList[i], last, Color.blue);
				last = nodeList[i];
			}
		}
	}

	public Vector3[] GetPointsWithinRadius(Vector3 origin, float radius, float? minRadius = null) {
		GridPos index = Grid.GetGridPos(origin);
		List<Vector3> poses = new List<Vector3>();

		int nodeRadius = Mathf.RoundToInt(radius * cellSize);
		float radiusSqr = radius * radius;
		float minRadiusSqr = 0;
		if(minRadius != null) {
			minRadiusSqr = (float)(minRadius * minRadius);
		}
		for(int x = -nodeRadius; x < nodeRadius; x++) {
			for(int y = -nodeRadius; y < nodeRadius; y++) {
				if(Grid.HasIndex(index.x + x, index.y + y) && Grid.IsWalkableAt(index.x + x, index.y + y)) {
					var indexPos = Grid.GetWorldPos(index.x + x, index.y + y);
					var diff = origin - indexPos;
					float sum = diff.x * diff.x + diff.z * diff.z;
					if(sum < radiusSqr && sum > minRadiusSqr) {
						poses.Add(indexPos);
					}
				}
			}
		}
		return poses.ToArray();
	}

	public void PaintCellAt(Vector3 pos, Color32 color, float duration = 0.05f) => PaintCells(new Vector3[1] { pos }, color, duration);
	public void PaintCells(Vector3[] poses, Color32 color, float duration = 0.05f) {
		StartCoroutine(IRepeat());
		IEnumerator IRepeat() {
			float t = 0;
			Dictionary<GridPos, Color> customCellColors = new Dictionary<GridPos, Color>();
			for(int i = 0; i < poses.Length; i++) {
				GridPos pos = Game.ActiveGrid.Grid.GetGridPos(poses[i]);
				if(customCellColors.ContainsKey(pos) == false) {
					customCellColors.Add(pos, color);
				}
			}
			while(t < duration) {
				foreach(var pos in customCellColors) {
					Draw.Cube(Grid.GetWorldPos(pos.Key), pos.Value, debugSize);
				}
				t += Time.deltaTime;
				yield return null;
			}
		}
}
	public bool SetWalkable(Vector3 pos, bool iWalkable) {
		GridPos gPos = Grid.GetGridPos(pos);
		if(Grid.HasIndex(gPos)) {
			return Grid.SetWalkableAt(gPos.x, gPos.y, iWalkable);
		}
		return false;
	}
	public bool SetWalkable(GridPos pos, bool iWalkable) {
		if(Grid.HasIndex(pos)) {
			return Grid.SetWalkableAt(pos.x, pos.y, iWalkable);
		}
		return false;
	}
	public bool SetReserved(Vector3 pos, bool state, GameObject holder) {
		GridPos gPos = Grid.GetGridPos(pos);
		if(Grid.HasIndex(gPos)) {
			return Grid.SetReserved(gPos.x, gPos.y, state, holder.GetHashCode());
		}
		return false;
	}
	public bool SetReserved(GridPos pos, bool state, GameObject holder) {
		if(Grid.HasIndex(pos)) {
			return Grid.SetReserved(pos.x, pos.y, state, holder.GetHashCode());
		}
		return false;
	}
}
