using System.Collections.Generic;
using System;
using UnityEngine;
using Sperlich.Types;

namespace ToyTanks.LevelEditor {
	[CreateAssetMenu(fileName = "LevelTheme", menuName = "Themes/Theme", order = 1)]
	public class ThemeAsset : ScriptableObject {

		public WorldTheme theme;
		public Material floorMaterial;
		public bool SSR;
		public bool isDark;
		public BlockAsset[] assets;

		[Serializable]
		public class BlockAsset {
			public BlockType block;
			public GameObject prefab;
			public Material material;
			public Sprite preview;
			public Vector3 Size => prefab.GetComponent<LevelBlock>().Size;

			public bool isDynamic {
				get {
					switch(block) {
						case BlockType.BoxDestructable:
							return true;
						default:
							return false;
					}
				}
			}
		}

		public BlockAsset GetAsset(BlockType type) {
			foreach(var a in assets) {
				if(a.block == type) {
					return a;
				}
			}
			throw new NullReferenceException("Asset: " + type.ToString() + " not found.");
		}
	}
}
