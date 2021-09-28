using Sperlich.Types;
using UnityEngine;

namespace ToyTanks.LevelEditor {

	[CreateAssetMenu(fileName = "Tank", menuName = "Tanks/Tank", order = 1)]
	public class TankAsset : ScriptableObject {

		public TankTypes tankType;
		public GameObject prefab;
		public Sprite preview;
		public bool usesPathfinding;
		public bool isBoss;
		public Vector3 tankSpawnOffset = new Vector3(0, 0.4f, 0);
		public static Int3 Size => new Int3(4, 4, 4);

	}
}
