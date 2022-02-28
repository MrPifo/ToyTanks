using Sperlich.Types;
using System;
using System.Collections.Generic;

namespace ToyTanks.LevelEditor {
	public class LevelExtraBlock : LevelBlock, IEditor {

		public ExtraBlocks extraType;

		public void SetData(Int3 index, List<Int3> indexes, ExtraBlocks type) {
			Index = index;
			allIndexes = indexes;
			this.extraType = type;
		}

	}
}
