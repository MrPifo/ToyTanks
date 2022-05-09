using Sperlich.Types;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ToyTanks.LevelEditor {
	public class LevelBlock : GameEntity, IEditor {

		[SerializeField]
		private MeshFilter _meshFilter;
		public MeshFilter MeshFilter {
			get => transform.CompareTag("LevelPreset") || _meshFilter == null ? GetComponent<MeshFilter>() : _meshFilter;
			set => _meshFilter = value;
		}
		[SerializeField]
		private MeshRenderer _meshRenderer;
		[SerializeField]
		private List<MeshRenderer> extraRenders = new List<MeshRenderer>();
		public MeshRenderer MeshRender {
			get => transform.CompareTag("LevelPreset") || _meshRenderer == null ? gameObject.transform.SearchComponent<MeshRenderer>() : _meshRenderer;
			set => _meshRenderer = value;
		}
		public BlockType type;
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
			get => transform.CompareTag("LevelPreset") ? new Int3(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), Mathf.RoundToInt(transform.position.z)) : _index;
			set => _index = value;
		}
		public List<Int3> allIndexes;
		public bool IsPreset => transform.CompareTag("LevelPreset");
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

		public void SetTheme(WorldTheme theme) {
			MeshRender.sharedMaterial = AssetLoader.GetBlockAsset(theme, type).material;
		}

		public virtual void SetData(Int3 index, List<Int3> indexes, BlockType type) {
			Index = index;
			allIndexes = indexes;
			this.type = type;
		}

		public void SetPosition(Vector3 pos) {
			transform.position = pos + offset;
		}

		public virtual void RestoreMaterials() {
			LevelEditor.RestoreMaterials(MeshRender.materials);
			if(extraRenders.Count > 0) {
				foreach(var r in extraRenders) {
					LevelEditor.RestoreMaterials(r.materials);
				}
            }
		}

		public virtual void SetAsPreview() {
			LevelEditor.SetMaterialsAsPreview(MeshRender.materials);
			if (extraRenders.Count > 0) {
				foreach (var r in extraRenders) {
					LevelEditor.SetMaterialsAsPreview(r.materials);
				}
			}
		}

		public virtual void SetAsDestroyPreview() {
			LevelEditor.SetMaterialsAsDestroyPreview(MeshRender.materials);
			if (extraRenders.Count > 0) {
				foreach (var r in extraRenders) {
					LevelEditor.SetMaterialsAsDestroyPreview(r.materials);
				}
			}
		}
	}
}
