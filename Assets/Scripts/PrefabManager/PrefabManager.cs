using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SimpleMan.Extensions;
using System;
using UnityEngine.SceneManagement;

namespace Sperlich.PrefabManager {
	public class PrefabManager : Singleton<PrefabManager> {

		private static bool HasBeenInitialized = false;
		public static string DefaultSceneSpawn { get; set; }
		private static List<PoolData> Pools;
		private static PrefabData _data;
		public static PrefabData Data {
			get {
				if(_data == null) {
					_data = Resources.Load<PrefabData>("PrefabData");
					if(_data == null) {
						throw new NullReferenceException("PrefabData could not be found!");
					}
				}
				return _data;
			}
		}

		protected override void Awake() {
			base.Awake();
		}


		/// <summary>
		/// Call this manually intialize the Prefab Pools
		/// </summary>
		public static void Initialize(string defaultScene) {
			DefaultSceneSpawn = defaultScene;
			if (HasBeenInitialized == false) {
				Instance.CreatePools();
				Logger.Log(Channel.System, "Prefabmanager intialized.");
			}
		}

		private void CreatePools() {
			if(HasBeenInitialized == false && Data != null) {
				Pools = new List<PoolData>();
				foreach(PrefabData.PrefabInfo info in Data.prefabs) {
					if(info.loadAsSingleton == false) {
						GameObject p = new GameObject {
							name = info.type.ToString()
						};
						p.transform.SetParent(transform);
						PoolData pool = p.AddComponent<PoolData>();
						pool.Initialize(info);
						Pools.Add(pool);
					}
				}
				HasBeenInitialized = true;
			}
		}

		/// <summary>
		/// Call this to reset and delete all gameobjects that have been spawned with the manager.
		/// </summary>
		public static void ResetPrefabManager() {
			HasBeenInitialized = false;
			if(Pools != null) {
				foreach(PoolData pool in Pools) {
					foreach(PoolData.PoolObject op in pool.pooledObjects) {
						if(Application.isPlaying) {
							Destroy(op.storedObject);
						} else {
							DestroyImmediate(op.storedObject);
						}
					}
				}
				for(int i = 0; i < Pools.Count; i++) {
					if(Pools[i] != null) {
						if(Application.isPlaying) {
							Destroy(Pools[i].gameObject);
						} else {
							DestroyImmediate(Pools[i].gameObject);
						}
					}
				}
			}

			Pools = new List<PoolData>();
			HasBeenInitialized = false;
			Logger.Log(Channel.System, "Prefabmanager has been reset.");
		}

		/// <summary>
		/// Instantiates and returns the GameObject.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parent"></param>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		/// <returns></returns>
		public static GameObject Instantiate(PrefabTypes type, Transform parent, Vector3 position = new Vector3(), Quaternion rotation = new Quaternion()) {
			try {
				GameObject o = Instantiate(GetPrefabData(type).prefab, position, rotation, parent);
				o.SetActive(true);
				return o;
			} catch (Exception e) {
				if(HasBeenInitialized == false) {
					Logger.LogError("PrefabManager has not been initialized!", e);
				} else {
					Logger.LogError("PrefabManager failed to instantiate " + type.ToString(), e);
				}
				throw e;
            }
		}
		/// <summary>
		/// Instantiates and returns the GameObject.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parent"></param>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		/// <returns></returns>
		public static GameObject Instantiate(PrefabTypes type) => Instantiate(type, null);
		/// <summary>
		/// Instantiates and returns the given Component.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parent"></param>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		/// <returns></returns>
		public static T Instantiate<T>(PrefabTypes type) => Instantiate<T>(type, null);
		/// <summary>
		/// Instantiates and returns the given Component.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parent"></param>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		/// <returns></returns>
		public static T Instantiate<T>(PrefabTypes type, Transform parent, Vector3 position = new Vector3(), Quaternion rotation = new Quaternion()) {
			GameObject o = Instantiate(type, parent, position, rotation);
			if(SceneManager.GetSceneByName(DefaultSceneSpawn).IsValid()) {
				SceneManager.MoveGameObjectToScene(o, SceneManager.GetSceneByName(DefaultSceneSpawn));
			}
			return o.GetComponent<T>();
		}

		/// <summary>
		/// Returns the required GameObject without creating a new one. If no stored GameObject is available though, a new one will be generated and stored for reuse.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parent"></param>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		/// <returns></returns>
		public static GameObject Spawn(PrefabTypes type, Transform parent, Vector3 position = new Vector3(), Quaternion rotation = new Quaternion()) {
			try {
				PoolData pool = GetPool(type);
				PoolData.PoolObject poolObject = pool.FetchFreePoolObject();
				if (parent != null) {
					poolObject.storedObject.transform.SetParent(parent);
				}
				poolObject.storedObject.transform.SetPositionAndRotation(position, rotation);
				poolObject.storedObject.SetActive(true);
				return poolObject.storedObject;
			} catch(Exception e) {
				if(HasBeenInitialized == false) {
					Logger.LogError("PrefabManager has not been initialized!", e);
				} else {
					Logger.LogError("PrefabManager failed to spawn " + type.ToString(), e);
				}
				throw e;
			}
        }
		/// <summary>
		/// Returns the required GameObject without creating a new one. If no stored GameObject is available though, a new one will be generated and stored for reuse.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parent"></param>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		/// <returns></returns>
		public static GameObject Spawn(PrefabTypes type) => Spawn(type, null);
		/// <summary>
		/// Returns the GameObject with the required Component without creating a new one. If no stored GameObject is available though, a new one will be generated and stored for reuse.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parent"></param>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		/// <returns></returns>
		public static T Spawn<T>(PrefabTypes type) => Spawn<T>(type, null);
		/// <summary>
		/// Returns the GameObject with the required Component without creating a new one. If no stored GameObject is available though, a new one will be generated and stored for reuse.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="parent"></param>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		/// <returns></returns>
		public static T Spawn<T>(PrefabTypes type, Transform parent, Vector3 position = new Vector3(), Quaternion rotation = new Quaternion()) {
			return Spawn(type, parent, position, rotation).GetComponent<T>();
		}

		/// <summary>
		/// Must be called to free a GameObject so that it can be recycled.
		/// </summary>
		/// <param name="gameobject"></param>
		/// <param name="delay"></param>
		public static void FreeGameObject(IRecycle gameobject, float delay = 0f) {
			try {
				if (delay == 0) {
					PoolData pool = GetPool(gameobject.PoolObject.type);
					pool.FreeGameObject(gameobject.PoolObject);
				} else {
					Instance.Delay(delay, () => {
						PoolData pool = GetPool(gameobject.PoolObject.type);
						pool.FreeGameObject(gameobject.PoolObject);
					});
				}
			} catch(Exception e) {
				Logger.LogError("PrefabManager failed to free the GameObject " + gameobject.PoolObject.type.ToString(), e);
				throw e;
			}
		}

		public static bool ContainsPrefab(string name) {
			foreach(var e in Enum.GetNames(typeof(PrefabTypes))) {
				if(e.ToLower() == name) {
					return true;
				}
			}
			return false;
		}
		public static PrefabData.PrefabInfo GetPrefabData(PrefabTypes prefabType) => Data.GetPrefabInfo(prefabType);
		public static PoolData GetPool(PrefabTypes prefabType) => Pools.Find(p => p.prefabInfo.type == prefabType);
	}
}