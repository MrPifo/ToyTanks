using Sperlich.Types;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ToyTanks.LevelEditor {
	public class LevelBlock : GameEntity {

		public MeshFilter meshFilter;
		public MeshRenderer meshRender;
		public LevelEditor.Themes theme;
		public LevelEditor.BlockTypes type;
		public Vector3 customBounds;
		public Vector3 offset;
		public Vector3 Size {
			get {
				Vector3 bounds = meshRender.bounds.size;
				if(customBounds.x > 0) {
					bounds.x = customBounds.x;
				}
				if(customBounds.y > 0) {
					bounds.y = customBounds.y;
				}
				if(customBounds.z > 0) {
					bounds.z = customBounds.z;
				}
				return bounds;
			}
		}
		public Int3 Index;
		public List<Int3> allIndexes;

		public void SetTheme(LevelEditor.Themes theme) {
			this.theme = theme;

			meshRender.sharedMaterial = LevelEditor.ThemeAssets.Find(t => t.theme == theme).GetAsset(type).material;
		}

		public void SetData(Int3 index, List<Int3> indexes, LevelEditor.BlockTypes type) {
			Index = index;
			allIndexes = indexes;
			this.type = type;
		}

		public void SetPosition(Vector3 pos) {
			transform.position = pos + offset;
		}
	}
}
