using Sperlich.Types;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ToyTanks.LevelEditor {
	public class LevelBlock : GameEntity {

		private MeshFilter _meshFilter;
		public MeshFilter MeshFilter {
			get => isLevelPreset || _meshFilter == null ? GetComponent<MeshFilter>() : _meshFilter;
			set => _meshFilter = value;
		}
		private MeshRenderer _meshRenderer;
		public MeshRenderer MeshRender {
			get => isLevelPreset || _meshRenderer == null ? GetComponent<MeshRenderer>() : _meshRenderer;
			set => _meshRenderer = value;
		}
		public LevelEditor.Themes theme;
		public LevelEditor.BlockTypes type;
		public Vector3 customBounds;
		public Vector3 offset;
		public Vector3 Size {
			get {
				Vector3 bounds = MeshRender.bounds.size;
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
		private Int3 _index;
		public Int3 Index {
			get => isLevelPreset ? new Int3(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), Mathf.RoundToInt(transform.position.z)) : _index;
			set => _index = value;
		}
		public List<Int3> allIndexes;
		public bool isLevelPreset;
		Mesh defaultMesh;

		private void Awake() {
			defaultMesh = MeshFilter.sharedMesh;
		}

		// Fix for dynamic Lightmap switching.
		// Reason: Unity static batches these meshes. This results in inaccurate lightmapping UV's
		// when attempting to switch the prebaked lightmaps.
		// Solution: Save the SharedMesh on Awake and Reapply it if this GameObject gets reenabled. This gets rid of the "CombinedMesh".
		private void OnEnable() {
			MeshFilter.sharedMesh = defaultMesh;
		}

		public void SetTheme(LevelEditor.Themes theme) {
			this.theme = theme;

			MeshRender.sharedMaterial = LevelEditor.ThemeAssets.Find(t => t.theme == theme).GetAsset(type).material;
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
