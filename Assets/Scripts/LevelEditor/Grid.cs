using Sperlich.Types;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ToyTanks.LevelEditor {
	public class LevelGrid {

		public Dictionary<Int3, bool> Grid { get; set; }
		Dictionary<Int2, List<int>> indexHeights;
		public int Size { get; set; }
		public Int3 Boundaries { get; set; }

		public LevelGrid(Int3 boundaries, int size) {
			Grid = new Dictionary<Int3, bool>();
			indexHeights = new Dictionary<Int2, List<int>>();
			Size = size;
			Boundaries = boundaries;

			for(int x = -boundaries.x; x < boundaries.x; x++) {
				for(int z = -boundaries.z; z < boundaries.z; z++) {
					indexHeights.Add(BaseIndex(ScaleIndex(new Int3(x, 0, z))), new List<int>());
					for(int y = 0; y < boundaries.y; y++) {
						Grid.Add(ScaleIndex(new Int3(x, y, z)), false);
					}
				}
			}
		}

		public Int3 WorldPosToIndex(Vector3 pos) => new Int3(Mathf.Round(pos.x / Size) * Size, Mathf.Round(pos.y / Size) * Size, Mathf.Round(pos.z / Size) * Size);
		public Int3 ScaleIndex(Int3 index) => new Int3(index.x * Size, index.y * Size, index.z * Size);
		public bool AddIndex(List<Int3> index, int height) {
			foreach(var i in index) {
				if(AddIndex(i, height) == false) {
					return false;
				}
			}
			return true;
		}
		public bool AddIndex(Int3 index, int height) {
			if(IsIndexAvailable(index)) {
				Grid[index] = true;
				UpdateHeightIndex(index);
				return true;
			}
			return false;
		}
		public bool IsIndexAvailable(Int3 index) => Grid.ContainsKey(index) && Grid[index] == false;
		public bool AreAllIndexesAvailable(List<Int3> indexes) {
			if(indexes == null) return false;
			return indexes.All(i => IsIndexAvailable(i));
		}
		public void RemoveIndex(Int3 index) {
			if(IsIndexAvailable(index) == false) {
				Grid[index] = false;
				if(indexHeights[BaseIndex(index)].Contains(index.y)) {
					indexHeights[BaseIndex(index)].Remove(index.y);
				}
			}
		}

		// Highest Index Helpers
		
		public bool HasHigherIndex(Int3 index) {
			if(indexHeights[BaseIndex(index)].Contains(index.y + Size)) {
				return true;
			} else {
				return false;
			}
		}
		public bool HasLowerIndex(Int3 index) {
			if(indexHeights[BaseIndex(index)].Count == 0 || indexHeights[BaseIndex(index)].Min() > index.y || indexHeights[BaseIndex(index)].Contains(index.y) == false) {
				return true;
			} else {
				return false;
			}
		}
		public Int3 GetNextHighestIndex(Int3 index) {
			if(indexHeights[BaseIndex(index)].Count > 0 && indexHeights.ContainsKey(BaseIndex(index))) {
				return new Int3(index.x, indexHeights[BaseIndex(index)].Max() + Size, index.z);
			}
			return index;
		}
		public void UpdateHeightIndex(Int3 index) {
			if(indexHeights[BaseIndex(index)].Contains(index.y) == false) {
				indexHeights[BaseIndex(index)].Add(index.y);
			}
		}
		Int2 BaseIndex(Int3 index) => new Int2(index.x, index.z);
	}
}
