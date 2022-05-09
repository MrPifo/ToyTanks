using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sperlich.Types;
using Cysharp.Threading.Tasks;

public class LevelGround : MonoBehaviour {

	//[HorizontalLine(color: EColor.Red)]
	[Header("New")]
	public Material mat;
	public GridSizes gridSize = GridSizes.Size_15x12;
	/// <summary>
	/// Use GameObjects to represent individual ground tiles?
	/// </summary>
	public bool useGameObjects;
	public static LayerMask detectionMask => LayerMaskExtension.Create(GameMasks.Block, GameMasks.Player, GameMasks.Bot, GameMasks.BulletTraverse, GameMasks.Destructable);
	public static List<GroundTile> Tiles;
	private static LevelGround _instance;
	public static LevelGround Instance {
		get {
			if(_instance == null) {
				_instance = FindObjectOfType<LevelGround>();
			}
			return _instance;
		}
	}
	private static MeshRenderer _meshRender;
	public static MeshRenderer Render {
		get {
			if(_meshRender == null) {
				if(Instance.TryGetComponent(out MeshRenderer r) == false) {
					_meshRender = Instance.gameObject.AddComponent<MeshRenderer>();
				} else {
					_meshRender = r;
				}
			}
			return _meshRender;
		}
	}
	private static MeshFilter _meshFilter;
	public static MeshFilter Filter {
		get {
			if(Instance.TryGetComponent(out MeshFilter f) == false) {
				_meshFilter = Instance.gameObject.AddComponent<MeshFilter>();
			} else {
				_meshFilter = f;
			}
			return _meshFilter;
		}
	}
	private static MeshCollider _meshCollider;
	public static MeshCollider MeshCollider {
		get {
			if(_meshCollider == null) {
				_meshCollider = Instance.gameObject.AddComponent<MeshCollider>();
			}
			return _meshCollider;
		}
	}
	private static MeshCollider gapCollider;
	private static GameObject gapColliderGameObject;

	/// <summary>
	/// Use this to for real gameplay purposes. Automacially generates and patches the mesh together.
	/// UseGameObjects is automaticially turned off in this function for better performance.
	/// </summary>
	public static async UniTask GenerateAndPatch(GridSizes size, List<LevelData.GroundTileData> generateFrom) {
		Logger.Log(Channel.Loading, "Generating Ground.");
		Clear();
		Generate(size, false, generateFrom);
		await PatchTiles();
	}

	/// <summary>
	/// Generates the ground tiles. Turn on useGameObjects to spawn individual ground tiles, otherwise the groundtiles get merged into a single mesh.
	/// </summary>
	public static void Generate(GridSizes gridSize, bool useGameObjects, List<LevelData.GroundTileData> generateFrom = null) {
		Clear();

		Instance.useGameObjects = useGameObjects;
		Instance.gridSize = gridSize;
		Tiles = new List<GroundTile>();
		Int3 size = LevelManager.GetGridBoundary(gridSize);
		for(int x = -size.x - 2; x < size.x + 2; x++) {
			for(int z = -size.z - 2; z < size.z + 2; z++) {
				var script = new GroundTile();
				if(useGameObjects) {
					script.GameObject = new GameObject("Ground");
					script.GameObject.transform.SetParent(Instance.transform);
					script.GameObject.tag = "GroundTile";
				}
				script.Apply(x, z);
				Tiles.Add(script);
			}
		}

		var tileList = Tiles;
		for(int i = 0; i < tileList.Count; i++) {
			tileList[i].EvaluateTile();
		}

		if(generateFrom != null) {
			foreach(var tileData in generateFrom) {
				var tile = GetTileAtWorldPos(tileData.index.xyz);
				if(tile != null) {
					tile.ChangeType(tileData.groundType);
				}
			}
		}
		UpdateGapTiles();
	}

	/// <summary>
	/// Merges all tiles into a single mesh and mesh collider to save on performance
	/// </summary>
	public static async UniTask PatchTiles() {
		GameObject primCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
		Mesh baseCubeMesh = primCube.GetComponent<MeshFilter>().sharedMesh;
		CombineInstance[] combine = new CombineInstance[Tiles.Count];
		CombineInstance[] gapCombine = new CombineInstance[Tiles.Where(t => GroundTile.IsAnyGapTile(t.type)).Count()];
		Destroy(primCube);

		if(Instance.useGameObjects) {
			List<MeshFilter> groundObjects = Tiles.Select(t => t.Filter).ToList();

			for(int i = 0; i < combine.Length; i++) {
				combine[i].mesh = groundObjects[i].sharedMesh;
				combine[i].transform = groundObjects[i].transform.localToWorldMatrix;
			}
			foreach(var g in GameObject.FindGameObjectsWithTag("GroundTile")) {
				Destroy(g);
			}
		} else {
			UpdateGapTiles();
			GameObject helper = new GameObject("VerticeWorldPosHelper");	// Gets moved around to convert mesh vertices to world space
			MeshFilter helperFilter = helper.AddComponent<MeshFilter>();
			BoxCollider boxCollider = helper.AddComponent<BoxCollider>();	// Helper for generating extra mesh collider for gap holes
			int gapNum = 0;

			for(int i = 0; i < combine.Length; i++) {
				helper.transform.localScale = Vector3.one;
				helper.transform.position = Tiles[i].Index.xyz;
				helper.transform.rotation = Quaternion.Euler(0, 90 * Tiles[i].orientation, 0);
				combine[i].mesh = Tiles[i].GetMesh;
				combine[i].transform = helper.transform.localToWorldMatrix;

				if(GroundTile.IsAnyGapTile(Tiles[i].type)) {
					helper.transform.localScale = new Vector3(2, 8, 2);
					gapCombine[gapNum].mesh = baseCubeMesh;
					gapCombine[gapNum].transform = helper.transform.localToWorldMatrix;

					gapNum++;
				}
			}

			Destroy(helper);
		}

		Mesh mesh = new Mesh();
		mesh.CombineMeshes(combine);
		mesh.Optimize();
		mesh.OptimizeIndexBuffers();
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
		mesh.UploadMeshData(false);
		Destroy(primCube);

		// Generate extra collider for gap holes
		if(gapCombine.Length > 0) {
			gapColliderGameObject = new GameObject("GapCollider");
			gapCollider = gapColliderGameObject.AddComponent<MeshCollider>();
			UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gapColliderGameObject, Instance.gameObject.scene);

			Mesh gapMesh = new Mesh();
			gapMesh.CombineMeshes(gapCombine);
			gapMesh.RecalculateBounds();
			gapMesh.RecalculateNormals();
			gapMesh.RecalculateTangents();
			gapMesh.UploadMeshData(false);
			gapCollider.sharedMesh = gapMesh;
			gapColliderGameObject.layer = GameMasks.BulletTraverse;

#if UNITY_EDITOR
			// For debug reasons only
			if(false) {
				var rend = gapColliderGameObject.AddComponent<MeshRenderer>();
				var filter = gapColliderGameObject.AddComponent<MeshFilter>();
				rend.sharedMaterial = Instance.mat;
				filter.sharedMesh = gapMesh;
			}
#endif
		}

		await UniTask.WaitForSeconds(1f);
		MeshCollider.sharedMesh = mesh;
		Filter.mesh = mesh;
		Render.sharedMaterial = Instance.mat;
		Debug.Log("Ground has been patched");
	}

	//[Button("Clear")]
	public static void Clear() {
		if(Instance.TryGetComponent(out MeshFilter filter)) {
			Destroy(filter);
		}
		if(Instance.TryGetComponent(out MeshRenderer rend)) {
			Destroy(rend);
		}
		if(Instance.TryGetComponent(out MeshCollider meshCollider)) {
			Destroy(meshCollider);
		}
		if(Instance.TryGetComponent(out MeshCollider gapCollider)) {
			Destroy(gapCollider);
		}
		if(GameObject.Find("GapCollider")) {
			Destroy(GameObject.Find("GapCollider"));
		}
		if(GameObject.Find("VerticeWorldPosHelper")) {
			Destroy(GameObject.Find("VerticeWorldPosHelper"));
		}
		foreach(var g in GameObject.FindGameObjectsWithTag("GroundTile")) {
			Destroy(g);
		}
		foreach(var g in FindObjectsOfType<GroundTileExtra>()) {
			Destroy(g.gameObject);
		}
		Tiles = new List<GroundTile>();
	}

	public static void PatchTileAt(int x, int z) {
		var tile = Tiles.Find(t => t.Index.x == x && t.Index.y == z);
		if(tile != null) {
			tile.EvaluateTile();
		}
	}

	public static Mesh GetMesh(GroundTileType type) {
		Mesh mesh = null;
		var tile = AssetLoader.GetGroundTile(type);
		if(tile != null) {
			mesh = tile.mesh;
		} else {
			Debug.LogError("Failed to load the mesh of the Ground-Tile: " + type);
        }
		return mesh;
	}

	public static void SetTheme(WorldTheme theme) {
		if(Instance.useGameObjects) {
			foreach(var tile in Tiles) {
				if(tile.MeshRender != null) {
					tile.MeshRender.sharedMaterial = AssetLoader.GetTheme(theme).floorMaterial;
				}
				if(tile.ExtraMesh != null) {
					tile.ExtraMesh.SetTheme(theme);
				}
			}
		} else {
			Render.sharedMaterial = AssetLoader.GetTheme(theme).floorMaterial;
		}
		MusicManager.PlayAmbient(theme);
	}

	public static List<GroundTile> FindNeighbours(Int2 origin) {
		List<GroundTile> neighs = new List<GroundTile>();

		if(GetTileAt(origin + new Int2(2, 0), out GroundTile neighRight)) {
			neighs.Add(neighRight);
		}
		if(GetTileAt(origin + new Int2(-2, 0), out GroundTile neighLeft)) {
			neighs.Add(neighLeft);
		}
		if(GetTileAt(origin + new Int2(0, 2), out GroundTile neighUp)) {
			neighs.Add(neighUp);
		}
		if(GetTileAt(origin + new Int2(0, -2), out GroundTile neighDown)) {
			neighs.Add(neighDown);
		}
		return neighs;
	}

	public static void UpdateGapTiles() {
		foreach(var tile in Tiles.Where(t => GroundTile.IsAnyGapTile(t.type))) {
			tile.EvaluateTile();
		}
	}

	public static bool GetTileAt(Int2 index, out GroundTile tile) {
		tile = Tiles.Find(t => t.Index == index);
		return tile != null;
	}

	public static GroundTile GetTileAtWorldPos(Vector3 pos) {
		return Tiles.Find(t => t.Index == new Int2(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z)));
	}
	public static bool GetTileAtWorldPos(Vector3 pos, out GroundTile tile) {
		tile = null;
		if (Tiles == null) {
			return false;
        }
		
		tile = Tiles.Find(t => t.Index == new Int2(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.z)));
		if(tile != null) {
			return true;
		}
		return false;
	}

	public static Vector3[] GetColliderVertexPositions(Transform from, BoxCollider b) {
		Vector3[] vertices = new Vector3[8];
		vertices[0] = from.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, -b.size.z) * 0.5f);
		vertices[1] = from.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, -b.size.z) * 0.5f);
		vertices[2] = from.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, b.size.z) * 0.5f);
		vertices[3] = from.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, b.size.z) * 0.5f);
		vertices[4] = from.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, -b.size.z) * 0.5f);
		vertices[5] = from.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, -b.size.z) * 0.5f);
		vertices[6] = from.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, b.size.z) * 0.5f);
		vertices[7] = from.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, b.size.z) * 0.5f);

		return vertices;
	}
}
