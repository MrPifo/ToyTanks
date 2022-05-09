using Sperlich.Types;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ToyTanks.LevelEditor {
	public class LevelFloraBlock : LevelBlock, IEditor {

		public FloraBlocks vegetationType;

		public void SetData(Int3 index, List<Int3> indexes, FloraBlocks type) {
			Index = index;
			allIndexes = indexes;
			vegetationType = type;
		}
	}
}
