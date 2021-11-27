using DG.Tweening;
using Sperlich.PrefabManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pellet : MonoBehaviour, IDamageEffector, IRecycle {

	float flyTime = 2;
	float explodeRadius = 4;
	float explodeDuration = 1f;
	float explodeShakeStrength = 8f;
	[Header("Impact Explosion Parts")]
	public LayerMask hitLayers;
	public AnimationCurve flyCurve;
	public GameObject mesh;
	public GameObject impactExplosionContainer;
	public GameObject explosionCraterPrefab;
	public Transform pelletBlobShadow;
	public Shapes.Sphere impactExplodeSphere;
	public Shapes.Disc impactIndicator;
	public ParticleSystem impactExplodeFire;

	public bool fireFromPlayer => false;
	public Vector3 damageOrigin => transform.position;

	public PoolData.PoolObject PoolObject { get; set; }

	/// <summary>
	/// Configure various pellet parameters.
	/// </summary>
	/// <param name="flytime"></param>
	/// <param name="explodeRadius"></param>
	/// <param name="explodeShakeStrength"></param>
	public void SetupPellet(float flyTime, float explodeRadius, float explodeShakeStrength) {
		this.flyTime = flyTime;
		this.explodeRadius = explodeRadius;
		this.explodeShakeStrength = explodeShakeStrength;
	}

	/// <summary>
	/// Configure specific flying curve and hitmask
	/// </summary>
	/// <param name="flyCurve"></param>
	/// <param name="hitLayers"></param>
	public void AdjustPellet(AnimationCurve flyCurve) {
		this.flyCurve = flyCurve;
	}

	public void AdjustPellet(AnimationCurve flyCurve, LayerMask hitLayers) {
		this.flyCurve = flyCurve;
		this.hitLayers = hitLayers;
	}

	/// <summary>
	/// Shoots the pellet with the configured settings.
	/// </summary>
	/// <param name="spawnPos"></param>
	/// <param name="impactPosition"></param>
	/// <param name="onExplodeCallback"></param>
	public void BlastOff(Vector3 spawnPos, Vector3 impactPosition, System.Action onExplodeCallback = null) {
		StartCoroutine(IShoot());
		IEnumerator IShoot() {
			// Setup bullet flying path
			transform.position = spawnPos;
			Vector3 pelletFlyDirection = (impactPosition - spawnPos).normalized;

			// Fix head rotation and set impact indicator close to ground
			impactPosition.y = 0.05f;

			// Set explosion and indicator to impact position
			impactIndicator.Show();
			impactExplosionContainer.transform.position = impactPosition;
			impactIndicator.transform.position = impactPosition;
			DOTween.To(x => impactIndicator.Radius = x, 0, explodeRadius + 4, 0.5f);
			impactExplodeSphere.Radius = explodeRadius;

			// Animate head
			AudioPlayer.Play("PelletShot", AudioType.SoundEffect, 1f, 0.5f, 1.5f);

			// Move pellet along flying path
			float time = 0;
			while(time < flyTime) {
				Vector3 pelletPos = Vector3.Lerp(spawnPos, impactPosition, time.Remap(0, flyTime, 0, 1f));
				pelletPos.y = flyCurve.Evaluate(time.Remap(0, flyTime, 0, 1f));
				mesh.transform.position = pelletPos;
				mesh.transform.Rotate(pelletFlyDirection, 500 * Time.deltaTime);
				pelletBlobShadow.position = pelletPos;
				time += Time.deltaTime;
				yield return IPause();
			}

			// Check if Hit
			foreach(var hit in Physics.OverlapSphere(impactPosition, explodeRadius, hitLayers)) {
				if(hit.transform.TrySearchComponent(out IHittable hittable)) {
					hittable.TakeDamage(this);
				}
			}

			// Impact
			GameCamera.ShakeExplosion2D(explodeShakeStrength, 0.2f);
			AudioPlayer.Play("SmallExplosion", AudioType.SoundEffect, 0.6f, 0.8f, 1f);
			Color colorLerp = impactExplodeSphere.Color;
			Color targetColor = colorLerp;
			targetColor.a = 0;
			mesh.Hide();
			impactIndicator.Hide();
			DOTween.To(() => colorLerp, a => impactExplodeSphere.Color = a, targetColor, explodeDuration);
			impactExplosionContainer.Show();
			impactExplodeFire.Play();
			PrefabManager.Spawn(PrefabTypes.ExplosionCrater, null, impactPosition + Vector3.up, Quaternion.Euler(90, Random.Range(0, 360), 0));

			// Cleanup
			pelletBlobShadow.Hide();
			impactIndicator.Hide();
			DOTween.To(() => impactExplodeSphere.Radius = 0, x => impactExplodeSphere.Radius = x, explodeRadius, explodeDuration).SetEase(Ease.OutCubic).OnComplete(() => {
				impactExplosionContainer.Hide();
				impactExplodeSphere.Color = colorLerp;
				if(onExplodeCallback != null) {
					onExplodeCallback.Invoke();
				}
				Recycle();
			});
		}
	}

	protected IEnumerator IPause() {
		if(Game.GamePaused || Game.IsTerminal) {
			while(Game.GamePaused || Game.IsTerminal) yield return null;   // Pause AI
		} else {
			yield return null;
		}
	}

	public void Recycle() {
		pelletBlobShadow.Show();
		mesh.Show();
		PrefabManager.FreeGameObject(this);
	}
}
