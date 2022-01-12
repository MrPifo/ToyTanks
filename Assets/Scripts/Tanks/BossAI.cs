using System.Collections;
using UnityEngine;
using DG.Tweening;
using SimpleMan.Extensions;

public abstract class BossAI : TankAI {

	public ParticleSystem fallImpactParticle;

	public override void ResetState() {
		var health = healthPoints;
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
				AudioPlayer.Play("FallImpact", AudioType.SoundEffect, 0.8f, 2);
			});
		});
	}
}
