using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GroundTile", menuName = "GroundTiles/Tile", order = 1)]
public class GroundTileData : ScriptableObject {

	public GroundTileType type;
	public int tileLevel = 0;
	public Mesh mesh;
	public Sprite preview;
	public bool notSelectable;

	[Header("Addtional Mesh")]
	public GameObject extraPrefab;
}
