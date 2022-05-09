using System.Collections;
using UnityEngine;
using DG.Tweening;
using SimpleMan.Extensions;

public abstract class BossAI : TankAI {

	/// <summary>
	/// Used to determine different boss phases with different attacks
	/// <para>Full Life = Boss has approxiametly over 50-60% health points</para>
	/// <para>Half_Life = Boss has approxiametly under 60-40% health points</para>
	/// <para>Low_Life = Boss has approxiametly under 20-0% health points</para>
	/// </summary>
	public enum BossPhases { Full_Life, Half_Life, Low_Life }
	[Header("BossAI")]
	public BossPhases CurrentPhase;
	public ParticleSystem fallImpactParticle;

	public override void ResetState() {
		var health = healthPoints;
		CurrentPhase = BossPhases.Full_Life;
		base.ResetState();
		healthPoints = health;
	}

	public virtual void BossSpawnAnimate() {
		Vector3 originalPos = transform.position;
		Vector3 fallPos = transform.position + Vector3.up * 40;
		transform.position = fallPos;
		this.Delay(Random(0, 1f), () => {
			transform.DOMove(originalPos, 0.8f).SetEase(Ease.InCubic).OnComplete(() => {
				GameCamera.ShakeExplosion2D(20, 0.3f);
				fallImpactParticle.Play();
				AudioPlayer.Play(JSAM.Sounds.FallImpact, AudioType.SoundEffect, 0.8f, 2);
			});
		});
	}
}
