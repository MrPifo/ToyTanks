﻿using System.Collections.Generic;
using System;
using UnityEngine;
using Sperlich.Types;

namespace ToyTanks.LevelEditor {
	[CreateAssetMenu(fileName = "LevelTheme", menuName = "Themes/Theme", order = 1)]
	public class ThemeAsset : ScriptableObject {

		public LevelEditor.Themes theme;
		public Material floorMaterial;
		public bool SSR;
		public BlockAsset[] assets;

		[Serializable]
		public class BlockAsset {
			public LevelEditor.BlockTypes block;
			public GameObject prefab;
			public Material material;
			public Sprite preview;
			public Vector3 Size => prefab.GetComponent<LevelBlock>().Size;
		}

		public BlockAsset GetAsset(LevelEditor.BlockTypes type) {
			foreach(var a in assets) {
				if(a.block == type) {
					return a;
				}
			}
			throw new NullReferenceException("Asset: " + type.ToString() + " not found.");
		}
	}
}