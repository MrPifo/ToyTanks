using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
// HDRP Related: using UnityEngine.Rendering.HighDefinition;
using Sperlich.Types;
using Newtonsoft.Json;
using UnityEngine.Events;
using SimpleMan.Extensions;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif

namespace ToyTanks.LevelEditor {
	public class LevelLightmapper : MonoBehaviour {

		public Mesh defaultPlaneMesh;
		public ReflectionProbe reflectionProbe;
		// HDRP Related: public HDAdditionalLightData hdLightData;
		static LevelLightmapper Instance;
		static LevelData level;
#if UNITY_EDITOR
		static Queue<LevelData> bakeQueue;
		static UnityEvent onBakeFinished = new UnityEvent();
		static Action finishedAction = new Action(OnBakeFinished);
		public bool IsBaking => Lightmapping.isRunning;
#endif

		private void Awake() {
			Instance = this;
		}

#if UNITY_EDITOR
		[DidReloadScripts]
		public static void ScriptReload() {
			Instance = FindObjectOfType<LevelLightmapper>();
		}

		public static void BakeLighting(LevelData data) {
			if(Lightmapping.isRunning == false) {
				Instance = FindObjectOfType<LevelLightmapper>();
				level = data;
				if(Game.LevelExists(level.levelId)) {
					Lightmapping.bakeCompleted += finishedAction;
					Lightmapping.BakeAsync();
				}
			}
		}

		public static void OnBakeFinished() {
			Debug.Log("Baking finished. Moving Files...");
			var lightInfoFolder = Application.dataPath + "/Resources/Lightmaps/Level_" + level.levelId;
			Lightmapping.bakeCompleted -= finishedAction;
			Directory.CreateDirectory(lightInfoFolder);

			var filePaths = Directory.GetFiles(Application.dataPath + "/Scenes/Level");
			foreach(string path in Directory.GetFiles(lightInfoFolder)) {
				File.Delete(path);
			}
			foreach(string path in filePaths) {
				string name = Path.GetFileName(path);
				if(name.Contains("LightingData")) continue;
				string destination = lightInfoFolder + "/" + name;
				File.Copy(path, destination);
			}

			var mapLight = new MapLightData();
			foreach(var mesh in FindObjectsOfType<LevelBlock>()) {
				Vector4 scaleOffset = mesh.MeshRender.lightmapScaleOffset;
				mapLight.lightInfo.Add(new MapLightData.LightInfo() {
					index = mesh.MeshRender.lightmapIndex,
					scaleOffset = new float[4] { scaleOffset.x, scaleOffset.y, scaleOffset.z, scaleOffset.w },
					gridIndex = mesh.Index
				});
			}

			var ground = GameObject.FindGameObjectWithTag("Ground").GetComponent<MeshRenderer>();
			mapLight.groundInfo = new MapLightData.LightInfo() {
				index = ground.lightmapIndex,
				scaleOffset = new float[4] { ground.lightmapScaleOffset.x, ground.lightmapScaleOffset.y, ground.lightmapScaleOffset.z, ground.lightmapScaleOffset.w }
			};
			var lightInfoJsonPath = lightInfoFolder + "/lightInfo.json";
			if(File.Exists(lightInfoJsonPath) == false) {
				File.Create(lightInfoJsonPath).Close();
			}
			File.WriteAllText(lightInfoJsonPath, JsonConvert.SerializeObject(mapLight, Formatting.Indented));
			AssetDatabase.Refresh();
			onBakeFinished.Invoke();
		}

		public static void BakeAllLevels(int from, int to) {
			var textAssets = Resources.LoadAll<TextAsset>($"Levels/");
			onBakeFinished = new UnityEvent();
			onBakeFinished.RemoveAllListeners();
			bakeQueue = new Queue<LevelData>();
			foreach(var asset in textAssets) {
				var data = JsonConvert.DeserializeObject<LevelData>(asset.text);
				if(from == 0 && to == 0) {
					bakeQueue.Enqueue(data);
				} else if(from >= 0 && to > from) {
					if(data.levelId >= (uint)from && data.levelId < (uint)to) {
						bakeQueue.Enqueue(data);
					}
				}
			}

			onBakeFinished.AddListener(() => {
				var nextLevel = bakeQueue.Dequeue();
				FindObjectOfType<LevelEditor>().LoadOfficialLevel(nextLevel.levelId);
				BakeLighting(nextLevel);
				Debug.Log("Remaining levels to be baked: " + bakeQueue.Count);
			});
			Debug.Log("Baking total Levels: " + bakeQueue.Count);
			var nextLevel = bakeQueue.Dequeue();
			FindObjectOfType<LevelEditor>().LoadOfficialLevel(nextLevel.levelId);
			BakeLighting(nextLevel);
		}
#endif

		public static void SwitchLightmaps(ulong levelId) {
			if(Game.LevelExists(levelId)) {
				Debug.Log("Loading Lightmaps from Level: " + levelId);
				Instance = FindObjectOfType<LevelLightmapper>();

				var lightData = new List<LightmapData>();
				var colorMaps = new List<Texture2D>();
				var dirMaps = new List<Texture2D>();
				var shadowMaps = new List<Texture2D>();

				var filePaths = Resources.LoadAll<Texture2D>("Lightmaps/Level_" + levelId);
				foreach(var tex in filePaths) {
					if(tex.name.Contains("_dir")) {
						dirMaps.Add(tex);
					} else if(tex.name.Contains("_light")) {
						colorMaps.Add(tex);
					} else if(tex.name.Contains("_shadowmask")) {
						shadowMaps.Add(tex);
					}
				}

				Debug.Log("Colormaps: " + colorMaps.Count + " DirMaps: " + dirMaps.Count + " Shadowmaps: " + shadowMaps.Count);

				int max = Mathf.Max(colorMaps.Count, dirMaps.Count, shadowMaps.Count);
				for(int i = 0; i < max; i++) {
					lightData.Add(new LightmapData() {
						lightmapColor = colorMaps.Count > i ? colorMaps[i] : null,
						lightmapDir = dirMaps.Count > i ? dirMaps[i] : null,
						shadowMask = shadowMaps.Count > i ? shadowMaps[i] : null,
					});
				}
				
				LightmapSettings.lightmaps = lightData.ToArray();
				Instance.reflectionProbe.customBakedTexture = Resources.Load<Texture>("Lightmaps/Level_" + levelId + "ReflectionProbe-0");

				var textAsset = Resources.Load<TextAsset>($"Lightmaps/Level_{levelId}/lightInfo");
				if(textAsset != null) {
					var mapLight = JsonConvert.DeserializeObject<MapLightData>(textAsset.text);
					foreach(var mesh in FindObjectsOfType<LevelBlock>()) {
						/*if(mapLight.HasInfo(mesh.Index) && mesh.type != BlockType.Hole) {
							var light = mapLight.GetInfo(mesh.Index);
							mesh.MeshRender.lightmapIndex = light.index;
							mesh.MeshRender.lightmapScaleOffset = new Vector4(light.scaleOffset[0], light.scaleOffset[1], light.scaleOffset[2], light.scaleOffset[3]);
							mesh.MeshRender.realtimeLightmapScaleOffset = mesh.MeshRender.lightmapScaleOffset;
							mesh.MeshRender.realtimeLightmapIndex = mesh.MeshRender.lightmapIndex;
							mesh.MeshRender.UpdateGIMaterials();
						}*/
					}

					var ground = GameObject.FindGameObjectWithTag("Ground")?.GetComponent<MeshRenderer>();
					if(ground != null) {
						ground.lightmapIndex = mapLight.groundInfo.index;
						ground.lightmapScaleOffset = new Vector4(mapLight.groundInfo.scaleOffset[0], mapLight.groundInfo.scaleOffset[1], mapLight.groundInfo.scaleOffset[2], mapLight.groundInfo.scaleOffset[3]);
						ground.realtimeLightmapIndex = 0;
						ground.realtimeLightmapScaleOffset = new Vector4(mapLight.groundInfo.scaleOffset[0], mapLight.groundInfo.scaleOffset[1], mapLight.groundInfo.scaleOffset[2], mapLight.groundInfo.scaleOffset[3]);
						//ground.GetComponent<MeshFilter>().sharedMesh = Instance.defaultPlaneMesh;
						//ground.UpdateGIMaterials();
					}
				}
				Instance.Delay(0.5f, () => {
					// HDRP Related: Instance.hdLightData.RequestShadowMapRendering();
				});
			}
		}

		[Serializable]
		public class MapLightData {

			public List<LightInfo> lightInfo = new List<LightInfo>();
			public LightInfo groundInfo;

			[Serializable]
			public class LightInfo {
				public Int3 gridIndex;
				public int index;
				public float[] scaleOffset = new float[4];
			}

			public LightInfo GetInfo(Int3 index) => lightInfo.Find(l => l.gridIndex == index);
			public bool HasInfo(Int3 index) => GetInfo(index) != null;
		}
	}
#if UNITY_EDITOR
	[CustomEditor(typeof(LevelLightmapper))]
	class LevelLightmapperEditor : Editor {

		public string levelId;
		public Vector2Int bakeRange;

		public override void OnInspectorGUI() {
			DrawDefaultInspector();
			var builder = (LevelLightmapper)target;
			if(GUILayout.Button("Bake") && Lightmapping.isRunning == false) {
				LevelLightmapper.BakeLighting(FindObjectOfType<LevelEditor>().LevelData);
			}
			if(GUILayout.Button("Bake All Levels") && Lightmapping.isRunning == false) {
				LevelLightmapper.BakeAllLevels(bakeRange.x, bakeRange.y);
			}
			
			bakeRange = EditorGUILayout.Vector2IntField("Bake Levels (From, To)", bakeRange);
		}
	}
#endif
}