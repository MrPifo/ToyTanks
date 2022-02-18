using Sperlich.Types;
using UnityEngine;
using System.Linq;
using ToyTanks.LevelEditor;
using System.Collections.Generic;

[System.Serializable]
public class GroundTile : IEditor {

	private Int2 _index;
	public Int2 Index {
		get => GameObject == null ? _index : new Int2(Mathf.RoundToInt(GameObject.transform.position.x), Mathf.RoundToInt(GameObject.transform.position.z));
		set => _index = value;
	}
	public GroundTileType type;
	public BlockType blockAbove;
	/// <summary>
	/// Orientation of the ground tile:
	/// 0 = 0°;
	/// 1 = 90°;
	/// 2 = 180°;
	/// 3 = 270 °
	/// </summary>
	public int orientation;

	public GameObject GameObject { get; set; }
	public GroundTileExtra ExtraMesh { get; set; }
	public MeshFilter Filter { get; private set; }
	public MeshRenderer MeshRender { get; private set; }
	public Mesh GetMesh => LevelGround.GetMesh(type);
	private BoxCollider collider;
	public GroundTileData data => LevelGround.GetTileData(type);

	public void Apply(int x, int z) {
		if(GameObject != null) {
			if(Filter == null) {
				Filter = GameObject.AddComponent<MeshFilter>();
			}
			if(MeshRender == null) {
				MeshRender = GameObject.AddComponent<MeshRenderer>();
			}
			collider = GameObject.AddComponent<BoxCollider>();
			MeshRender.receiveGI = ReceiveGI.Lightmaps;
			GameObject.layer = GameMasks.Ground;
			GameObject.isStatic = true;
			GameObject.transform.position = new Vector3(x * 2, 0, z * 2);
			MeshRender.sharedMaterial = LevelGround.Instance.mat;
		} else {
			Index = new Int2(x * 2, z * 2);
		}
	}

	// Looks at it surroundings and objects and decides which tile type to use
	public void EvaluateTile() {
		var blocks = Object.FindObjectsOfType<LevelBlock>().Where(b => b.isNotEditable == false && b.Index.y == 0).ToList();
		orientation = 0;
		if(collider != null) {
			collider.size = new Vector3(2, 0.01f, 2);
		}

		if(Physics.Raycast(Index.xyz - Vector3.up, Vector3.up, out RaycastHit hit, 50, LevelGround.detectionMask)) {
			if(hit.transform.TryGetComponent(out LevelBlock block) && block.isNotEditable == false) {
				//Debug.DrawLine(Index.xyz - Vector3.up, hit.point, Color.red, 5, false);
				blockAbove = block.type;
			}
		}

		// Automaticially Adjust holes to right mesh
		if(IsAnyGapTile(type)) {
			// Get adjazent tiles and filter them to only gap tiles
			var neighs = LevelGround.Instance.FindNeighbours(Index).Where(t => IsAnyGapTile(t.type));
			type = GroundTileType.Gap_Empty;
			if(collider != null) {
				collider.size = new Vector3(2, 2, 2);
			}

			if(neighs.Count() == 0) {
				type = GroundTileType.Gap_Full;
				orientation = 0;
			}
			// Dead Ends
			if(neighs.Count() == 1) {
				// Tile is dead end down
				if(neighs.Any(t => t.Index == Index + new Int2(0, -2))) {
					type = GroundTileType.Gap_End;
					orientation = 0;
				}

				// Tile is dead end Right
				if(neighs.Any(t => t.Index == Index + new Int2(2, 0))) {
					type = GroundTileType.Gap_End;
					orientation = 3;
				}

				// Tile is dead end Up
				if(neighs.Any(t => t.Index == Index + new Int2(0, 2))) {
					type = GroundTileType.Gap_End;
					orientation = 2;
				}

				// Tile is dead end Left
				if(neighs.Any(t => t.Index == Index + new Int2(-2, 0))) {
					type = GroundTileType.Gap_End;
					orientation = 1;
				}
			}
			// Horizontal & Vertical Walls
			if(neighs.Count() == 2) {
				// Tile is 2Wall if there are 2 neighbours horizontally
				if(neighs.Any(t => t.Index == Index + new Int2(2, 0) && neighs.Any(t => t.Index == Index + new Int2(-2, 0)))) {
					type = GroundTileType.Gap_2Wall;
				}

				// Tile is 2Wall if there are 2 neighbours vertically
				if(neighs.Any(t => t.Index == Index + new Int2(0, 2) && neighs.Any(t => t.Index == Index + new Int2(0, -2)))) {
					type = GroundTileType.Gap_2Wall;
					orientation = 1;
				}
			}
			// T-Walls
			if(neighs.Count() == 3) {
				// Tile is T-Wall facing down
				if(neighs.Any(t => t.Index == Index + new Int2(2, 0)) && neighs.Any(t => t.Index == Index + new Int2(-2, 0)) && neighs.Any(t => t.Index == Index + new Int2(0, -2))) {
					type = GroundTileType.Gap_TWall;
					orientation = 0;
				}

				// Tile is T-Wall facing up
				if(neighs.Any(t => t.Index == Index + new Int2(2, 0)) && neighs.Any(t => t.Index == Index + new Int2(-2, 0)) && neighs.Any(t => t.Index == Index + new Int2(0, 2))) {
					type = GroundTileType.Gap_TWall;
					orientation = 2;
				}

				// Tile is T-Wall facing right
				if(neighs.Any(t => t.Index == Index + new Int2(0, 2)) && neighs.Any(t => t.Index == Index + new Int2(0, -2)) && neighs.Any(t => t.Index == Index + new Int2(2, 0))) {
					type = GroundTileType.Gap_TWall;
					orientation = 3;
				}

				// Tile is T-Wall facing left
				if(neighs.Any(t => t.Index == Index + new Int2(0, 2)) && neighs.Any(t => t.Index == Index + new Int2(0, -2)) && neighs.Any(t => t.Index == Index + new Int2(-2, 0))) {
					type = GroundTileType.Gap_TWall;
					orientation = 1;
				}
			}
			// Corners
			if(neighs.Count() == 2) {
				// Tile is Corner Left -> Down
				if(neighs.Any(t => t.Index == Index + new Int2(-2, 0) && neighs.Any(t => t.Index == Index + new Int2(0, -2)))) {
					type = GroundTileType.Gap_Corner;
					orientation = 0;
				}

				// Tile is Corner Right -> Down
				if(neighs.Any(t => t.Index == Index + new Int2(2, 0) && neighs.Any(t => t.Index == Index + new Int2(0, -2)))) {
					type = GroundTileType.Gap_Corner;
					orientation = 3;
				}

				// Tile is Corner Right -> Up
				if(neighs.Any(t => t.Index == Index + new Int2(2, 0) && neighs.Any(t => t.Index == Index + new Int2(0, 2)))) {
					type = GroundTileType.Gap_Corner;
					orientation = 2;
				}

				// Tile is Corner Left -> Up
				if(neighs.Any(t => t.Index == Index + new Int2(-2, 0) && neighs.Any(t => t.Index == Index + new Int2(0, 2)))) {
					type = GroundTileType.Gap_Corner;
					orientation = 1;
				}
			}
		}
		UpdateMesh();
	}

	public void UpdateMesh() {
		if(GameObject != null) {
			Filter.sharedMesh = LevelGround.GetMesh(type);
			GameObject.name = type.ToString();
			GameObject.layer = GameMasks.Ground;
			GameObject.transform.rotation = Quaternion.Euler(0, orientation * 90, 0);

			if(ExtraMesh != null) {
				Object.DestroyImmediate(ExtraMesh.gameObject);
			}
			if(IsAnyGapTile(type)) {
				GameObject.layer = GameMasks.BulletTraverse;
			}
			try {
				if(data.extraPrefab != null) {
					ExtraMesh = Object.Instantiate(data.extraPrefab.gameObject).GetComponent<GroundTileExtra>();
					UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ExtraMesh.gameObject, GameObject.scene);
					ExtraMesh.transform.SetParent(GameObject.transform);
					ExtraMesh.transform.localPosition = Vector3.zero;
				}
			} catch {
				Debug.LogError("Failed to set extra prefab from " + data);
			}
		} else {
			try {
				if(data.extraPrefab != null) {
					ExtraMesh = Object.Instantiate(data.extraPrefab.gameObject).GetComponent<GroundTileExtra>();
					UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ExtraMesh.gameObject, LevelGround.Instance.gameObject.scene);
					ExtraMesh.transform.SetParent(LevelGround.Instance.transform);
					ExtraMesh.transform.localPosition = Index.xyz;
				}
			} catch {
				Debug.LogError("Failed to set extra prefab from " + data);
			}
		}
	}

	public void Remove() {
		Object.FindObjectOfType<LevelGround>().Tiles.Remove(this);
		if(GameObject != null) {
			Object.DestroyImmediate(GameObject);
		}
	}

	public void ChangeType(GroundTileType type) {
		this.type = type;

		EvaluateTile();
		UpdateMesh();
	}

	public void RestoreMaterials() {
		MeshRender?.material?.SetFloat("_EditorPreview", 0);
	}

	public void SetAsPreview() {
		MeshRender?.material?.SetFloat("_EditorPreview", 1);
		MeshRender?.material?.DisableKeyword("_EDITORDESTROY");
	}

	public void SetAsDestroyPreview() {
		MeshRender?.material?.SetFloat("_EditorPreview", 1);
		MeshRender?.material?.EnableKeyword("_EDITORDESTROY");
	}

	public static bool IsAnyGapTile(GroundTileType type) {
		return type == GroundTileType.Gap_Empty || type == GroundTileType.Gap_Corner || type == GroundTileType.Gap_2Wall || type == GroundTileType.Gap_End || type == GroundTileType.Gap_TWall || type == GroundTileType.Gap_Full;
}
}
