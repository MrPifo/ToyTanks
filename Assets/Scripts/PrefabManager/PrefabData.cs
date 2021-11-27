using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Sperlich.PrefabManager {
	[CreateAssetMenu(fileName = "PrefabData", menuName = "Game/Prefab Data", order = 1)]
	public class PrefabData : ScriptableObject {

		public List<PrefabInfo> prefabs;

		public PrefabInfo GetPrefabInfo(PrefabTypes type) => prefabs.Find(t => t.type == type);

		[System.Serializable]
		public class PrefabInfo {
			public PrefabTypes type;
			public GameObject prefab;
			[Range(0, 1000)]
			public int preloadAmount;
		}
	}
}