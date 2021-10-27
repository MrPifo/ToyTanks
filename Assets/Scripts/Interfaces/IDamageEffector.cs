using System;
using UnityEngine;

public interface IDamageEffector {

	public bool fireFromPlayer { get; }
	public Vector3 damageOrigin { get; }

}
