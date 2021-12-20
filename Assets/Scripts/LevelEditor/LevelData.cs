using Newtonsoft.Json;
using Sperlich.Types;
using System.Collections.Generic;
using ToyTanks.LevelEditor;
using UnityEngine;
// HDRP Related: using UnityEngine.Rendering.HighDefinition;

[System.Serializable]
public class LevelData {

	public string levelName;
	public ulong levelId;    // 0 - 4069 must be reserved for offical levels
	public bool isNight;
	public GridSizes gridSize;
	public float indirectLightIntensity;
	public float? customCameraFocusIntensity = null;
	public float? customMaxZoomOut = null;
	public LevelEditor.Themes theme;
	public LightData sunLight;
	public LightData spotLight;
	public List<BlockData> blocks = new List<BlockData>();
	public List<TankData> tanks = new List<TankData>();

	public class BlockData {
		public Int3 pos;
		public Int3 index;
		public Int3 rotation;
		public LevelEditor.Themes theme;
		public LevelEditor.BlockTypes type;
	}

	public class TankData {
		public TankTypes tankType;
		public Int3 pos;
		public Int3 index;
		public Int3 rotation;
	}

	public class LightData {
		public Int3 pos;
		public Int3 rotation;
		public int intensity;
		/* HDRP Related: 
		public LightData(HDAdditionalLightData light) {
			if(light != null) {
				pos = new Int3(light.transform.position);
				rotation = new Int3(light.transform.eulerAngles);
				intensity = (int)light.intensity;
			}
		}*/
	}
}
