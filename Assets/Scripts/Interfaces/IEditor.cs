using UnityEngine;

namespace ToyTanks.LevelEditor {
	/// <summary>
	/// Interface for ingame LevelEditor. Used to identify objects that can be edited from the editor
	/// </summary>
	public interface IEditor {

		/// <summary>
		/// Restore materials after editor painted
		/// </summary>
		public void RestoreMaterials();
		/// <summary>
		/// Set materials to editor paint
		/// </summary>
		/// <param name="mat"></param>
		public void SetAsPreview();
		/// <summary>
		/// Set materials to destroy editor paint
		/// </summary>
		public void SetAsDestroyPreview();
	}
}
