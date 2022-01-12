using UnityEngine;

public interface IHittable {

	public bool IsInvincible { get; }
	public bool IsFriendlyFireImmune { get; }
	public void TakeDamage(IDamageEffector effector, bool instantKill = false);
}
