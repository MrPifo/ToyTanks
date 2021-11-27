using SimpleMan.Extensions;
using Sperlich.PrefabManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is a generic Script you can attach to a pooled GameObject
/// </summary>
public class PoolGameObject : MonoBehaviour, IRecycle {
	public PoolData.PoolObject PoolObject { get; set; }

	public void Recycle() {
		PrefabManager.FreeGameObject(this);
	}

	public void Recycle(float delay) {
		this.Delay(delay, () => Recycle());
	}
}
