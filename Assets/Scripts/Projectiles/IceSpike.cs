using DG.Tweening;
using SimpleMan.Extensions;
using Sperlich.PrefabManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class IceSpike : GameEntity, IDamageEffector, IRecycle, IHittable {

	public float stayDuration;
	public ParticleSystem iceParticles;
	public ParticleSystem destroyParticles;
	public List<Mesh> variants = new List<Mesh>();
	public bool fireFromPlayer => false;
	private new BoxCollider collider;
	private MeshRenderer render;
	private MeshFilter filter;
	public DecalProjector iceGroundDecal;
	public Vector3 damageOrigin => transform.position;
	public PoolData.PoolObject PoolObject { get; set; }

	public bool IsInvincible => false;
	public bool IsFriendlyFireImmune => false;
	protected static LayerMask overlapLayers = LayerMaskExtension.Create(GameMasks.Block, GameMasks.Destructable, GameMasks.BulletTraverse, GameMasks.LevelBoundary);
	protected static LayerMask hitLayers = LayerMaskExtension.Create(GameMasks.Destructable, GameMasks.Player);
	protected static LayerMask destroyOnTouchLayers = LayerMaskExtension.Create(GameMasks.Bot);

	private void Awake() {
		collider = GetComponent<BoxCollider>();
		render = GetComponentInChildren<MeshRenderer>();
		filter = GetComponentInChildren<MeshFilter>();
		render.enabled = false;
		iceGroundDecal.fadeFactor = 0;
	}

	public void SummonSpike(float spawnDelay = 0.02f) {
		StartCoroutine(ISummon());
		IEnumerator ISummon() {
			collider.enabled = false;
			iceGroundDecal.fadeFactor = 0;
			var decalFade = DOTween.To(() => iceGroundDecal.fadeFactor, x => iceGroundDecal.fadeFactor = x, 1, 0.5f);
			yield return new WaitForSeconds(spawnDelay);
			// Check if spike doesnt summon in walls and destroys it if this is the case
			if(Physics.SphereCast(new Ray(transform.position - (Vector3.up * 2), Vector3.up), 1f, out RaycastHit rHit, 20f, overlapLayers) == false) {
				collider.enabled = true;
				render.enabled = true;
				transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
				Vector3 spawnPos = transform.position;
				transform.position -= new Vector3(0, collider.size.y, 0);
				transform.localScale = new Vector3(1f, 0, 1f);
				transform.DOMoveY(spawnPos.y - 0.5f, 0.2f).SetEase(Ease.OutBounce);
				transform.DOScaleY(1f, 0.4f);
				GameCamera.ShakeExplosion2D(1, 0.15f);
				filter.sharedMesh = variants.RandomItem();
				iceParticles.Play();
				AudioPlayer.Play("IceSpikeSpawn", AudioType.SoundEffect, 0.7f, 2f, 0.5f);
				yield return new WaitForSeconds(0.1f);
				// Check if Hit
				foreach(var hit in Physics.OverlapSphere(transform.position, 1f, hitLayers)) {
					if(hit.transform.TrySearchComponent(out IHittable hittable)) {
						hittable.TakeDamage(this, true);
					}
				}
				yield return new WaitUntil(() => Game.IsGamePlaying && Game.GamePaused == false);
				yield return new WaitForSeconds(stayDuration);
				Despawn();
			} else {
				decalFade.Kill();
				if(rHit.transform.TrySearchComponent(out IceSpike spike)) {
					spike.Despawn();
				}
				Recycle();
			}
		}
	}

	public void Despawn() {
		transform.DOMoveY(-collider.size.y - 1, 0.5f).SetEase(Ease.InCubic);
		transform.DOScaleY(0f, 1f).SetEase(Ease.OutCubic);
		DOTween.To(() => iceGroundDecal.fadeFactor, x => iceGroundDecal.fadeFactor = x, 0, 0.5f);
		this.Delay(0.5f, () => Recycle());
	}

	protected IEnumerator IPause() {
		if(Game.GamePaused || Game.IsTerminal) {
			while(Game.GamePaused || Game.IsTerminal)
				yield return null;   // Pause AI
		} else {
			yield return null;
		}
	}

	public void Recycle() {
		collider.enabled = true;
		render.enabled = false;
		PrefabManager.FreeGameObject(this);
	}

	public void TakeDamage(IDamageEffector effector, bool instantKill = false) {
		AudioPlayer.Play("IceSpikeShatter", AudioType.SoundEffect, 0.8f, 1.2f, 1f);
		render.enabled = false;
		destroyParticles.Play();
		collider.enabled = false;

		this.Delay(1f, () => {
			Recycle();
		});
	}

	public void OnCollisionEnter(Collision collision) {
		if(collision.gameObject.IsInLayerMask(destroyOnTouchLayers)) {
			TakeDamage(null, false);
		}
	}
}
