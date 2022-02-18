using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Sperlich.Types;
using System.Collections.Generic;
using ToyTanks.LevelEditor;
using UnityEngine;
// HDRP Related: using UnityEngine.Rendering.HighDefinition;

[System.Serializable]
public class LevelData {

	public string levelName;
	public ulong levelId;    // 0 - 4069 must be reserved for offical levels
	[JsonConverter(typeof(StringEnumConverter))]
	public GridSizes gridSize;
	[JsonConverter(typeof(StringEnumConverter))]
	public WorldTheme theme;
	public List<BlockData> blocks = new List<BlockData>();
	public List<TankData> tanks = new List<TankData>();
	public List<GroundTileData> groundTiles = new List<GroundTileData>();

	[System.Serializable]
	public class BlockData {
		public Int3 pos;
		public Int3 index;
		public Int3 rotation;
		[JsonConverter(typeof(StringEnumConverter))]
		public BlockType type;
	}

	[System.Serializable]
	public class TankData {
		[JsonConverter(typeof(StringEnumConverter))]
		public TankTypes tankType;
		public Int3 pos;
		public Int3 index;
		public Int3 rotation;
	}
	[System.Serializable]
	public class GroundTileData {
		[JsonConverter(typeof(StringEnumConverter))]
		public GroundTileType groundType;
		public Int2 index;
	}
}
