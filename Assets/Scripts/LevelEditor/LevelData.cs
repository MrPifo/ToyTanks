using Newtonsoft.Json;
using Sperlich.Types;
using System.Collections.Generic;
using ToyTanks.LevelEditor;
using UnityEngine;

[System.Serializable]
public class LevelData {

	public string levelName;
	public ulong levelId;    // 0 - 4069 must be reserved for offical levels
	public GridSizes gridSize;
	public LevelEditor.Themes theme;
	public List<BlockData> blocks = new List<BlockData>();
	public List<TankData> tanks = new List<TankData>();

	public class BlockData {
		public Int3 pos;
		public Int3 index;
		public Int3 rotation;
		public LevelEditor.Themes theme;
		public LevelEditor.BlockTypes type;
	}

	public class TankData {
		public TankTypes tankType;
		public Int3 pos;
		public Int3 index;
		public Int3 rotation;
	}
}
