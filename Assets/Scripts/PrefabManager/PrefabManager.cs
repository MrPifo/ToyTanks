using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SimpleMan.Extensions;
using System;

namespace Sperlich.PrefabManager {
	public class PrefabManager : Singleton<PrefabManager> {

		[SerializeField] PrefabData prefabData;
		List<PoolData> pools;
		bool hasBeenInitialized = false;

		protected override void Awake() {
			base.Awake();
			Initialize();
		}


		/// <summary>
		/// Call this manually intialize the Prefab Pools
		/// </summary>
		public static void Initialize() {
			if (Instance.hasBeenInitialized == false) {
				Instance.CreatePools();
				Logger.Log(Channel.System, "Prefabmanager intialized.");
			}
		}

		private void CreatePools() {
			if(hasBeenInitialized == false && prefabData != null) {
				pools = new List<PoolData>();
				foreach(PrefabData.PrefabInfo info in prefabData.prefabs) {
					GameObject p = new GameObject {
						name = info.type.ToString()
					};
					p.transform.SetParent(transform);
					PoolData pool = p.AddComponent<PoolData>();
					pool.Initialize(info);
					pools.Add(pool);
				}
				hasBeenInitialized = true;
			}
		}

		/// <summary>
		/// Call this to reset and delete all gameobjects that have been spawned with the manager.
		/// </summary>
		public static void ResetPrefabManager() {
			Instance.hasBeenInitialized = false;
			if(Instance.pools != null) {
				foreach(PoolData pool in Instance.pools) {
					foreach(PoolData.PoolObject op in pool.pooledObjects) {
						Destroy(op.storedObject);
					}
				}
				for(int i = 0; i < Instance.pools.Count; i++) {
					Destroy(Instance.pools[i].gameObject);
				}
			}
			Instance.pools = new List<PoolData>();
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
				Logger.LogError(Channel.Loading, "PrefabManager failed to instantiate " + type.ToString(), e);
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
			return Instantiate(type, parent, position, rotation).GetComponent<T>();
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
				Logger.LogError(Channel.Loading, "PrefabManager failed to spawn " + type.ToString(), e);
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
				Logger.LogError(Channel.Loading, "PrefabManager failed to free the GameObject " + gameobject.PoolObject.type.ToString(), e);
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
		public static PrefabData.PrefabInfo GetPrefabData(PrefabTypes prefabType) => Instance.prefabData.GetPrefabInfo(prefabType);
		public static PoolData GetPool(PrefabTypes prefabType) => Instance.pools.Find(p => p.prefabInfo.type == prefabType);
	}
}