﻿using Sperlich.PrefabManager;

namespace Sperlich.PrefabManager {
	public interface IRecycle {

		public PoolData.PoolObject PoolObject { get; set; }
		/// <summary>
		/// Must be called if the GameObject is free to use
		/// </summary>
		public void Recycle();

	}
}
