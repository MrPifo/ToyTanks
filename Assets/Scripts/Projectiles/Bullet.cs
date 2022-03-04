using CarterGames.Assets.AudioManager;
using SimpleMan.Extensions;
using Sperlich.Debug.Draw;
using Sperlich.PrefabManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Bullet : GameEntity, IHittable, IDamageEffector, IResettable, IRecycle {

	public float velocity;
	public int maxBounces;
	public LayerMask reflectLayers;
	public ParticleSystem explodeSmoke;
	public ParticleSystem explodeSmokeFire;
	public ParticleSystem explodePieces;
	public ParticleSystem smokeTrail;
	public ParticleSystem smokeFireTrail;
	public ParticleSystem impactSparks;
	public GameObject fakeShadow;
	public Collider hitBox;
	public Collider rejectHitBox;
	public new MeshRenderer renderer;
	[Header("Individuell")]
	public ParticleSystem BurstParticles;
	public bool showDebug;
	public bool isFromPlayer;
	public bool IsInvincible => false;
	public bool IsFriendlyFireImmune => false;
	GameObject lastHitObject;
	public UnityEvent OnBulletDestroyed { get; private set; }

	public bool fireFromPlayer => isFromPlayer;
	public Vector3 damageOrigin => transform.position;

	public static Vector3 bulletSize = new Vector3(0.25f, 0.25f, 0.25f);
	List<(Vector3 pos, Vector3 normal)> predictedPath;
	Rigidbody rig;
	float lastHitTime;
	public Vector3 Direction { get; set; }
	public PoolData.PoolObject PoolObject { get; set; }

	float baseVelocity;
	float time;
	int bounces = 0;
	bool hitTarget;

	void Awake() {
		rig = GetComponent<Rigidbody>();
		OnBulletDestroyed = new UnityEvent();
		baseVelocity = velocity;
		if(GraphicSettings.PerformanceMode) {
			fakeShadow.gameObject.SetActive(false);
		} else {
			fakeShadow.gameObject.SetActive(true);
		}
	}

	void FixedUpdate() {
		if(Game.IsGameCurrentlyPlaying) {

			Vector3 dir = (transform.position - (transform.position - Direction)).normalized;
			if(dir != Vector3.zero) {
				Quaternion look = Quaternion.LookRotation(dir, transform.up);

				Vector3 pos = Time.fixedDeltaTime * velocity * Direction;
				rig.MovePosition(rig.position + pos);
				rig.rotation = look;
				time += Time.fixedDeltaTime;
				rig.velocity = Vector3.zero;
			}
		}
	}

	[System.Obsolete]
	public void PrecalculatePath() {
		Ray ray = new Ray(transform.position, Direction);
		predictedPath = new List<(Vector3, Vector3)>();

		for(int i = 0; i < maxBounces + 1; i++) {
			Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, Mathf.Infinity, reflectLayers);
			predictedPath.Add((hit.transform.position, hit.normal));
			ray = new Ray(hit.point, Vector3.Reflect(ray.direction, hit.normal));
		}
	}

	public void TakeDamage(IDamageEffector effector, bool instantKill = false) {
		hitTarget = true;
		explodeSmoke.Play();
		explodeSmokeFire.Play();
		explodePieces.Play();
		impactSparks.Play();
		smokeTrail.Stop(false, ParticleSystemStopBehavior.StopEmitting);
		smokeFireTrail.Stop(false, ParticleSystemStopBehavior.StopEmitting);
		fakeShadow.Hide();
		hitBox.enabled = false;
		rejectHitBox.enabled = false;
		renderer.enabled = false;
		

		//new GameObject().AddComponent<DestructionTimer>().Destruct(5, new GameObject[] { explodeSmoke.gameObject, explodeSmokeFire.gameObject, explodePieces.gameObject, smokeTrail.gameObject, smokeFireTrail.gameObject });
		OnBulletDestroyed.Invoke();
		OnBulletDestroyed.RemoveAllListeners();

		this.Delay(2, () => Recycle());
	}

	public Bullet SetupBullet(Vector3 dir, Vector3 startPos, bool firedFromPlayer) {
		transform.position = startPos;
		Direction = dir;
		isFromPlayer = firedFromPlayer;
		AudioPlayer.Play("BulletShot", AudioType.SoundEffect, 0.8f, 1.2f, 1.2f);
		return this;
	}

	void OnCollisionEnter(Collision coll) {
		if(hitTarget == false && time != lastHitTime) {
			if(Physics.SphereCast(transform.position, 0.15f, Direction, out RaycastHit hit, Mathf.Infinity, reflectLayers)) {
				if(hit.transform.gameObject != lastHitObject) {
					bounces++;
					if(coll.transform.TrySearchComponent(out IHittable hittable)) {
						hittable.TakeDamage(this);
						TakeDamage(this);
					} else if(bounces > maxBounces) {
						TakeDamage(this);
					} else if(IsReflective(coll.transform) == false) {
						TakeDamage(this);
					} else {
						Reflect(hit.normal);
						lastHitTime = time;
						lastHitObject = hit.transform.gameObject;
						Debug.DrawRay(rig.position, Direction, Color.cyan, 20);
					}
				}
			}
		}
	}

	void Reflect(Vector3 inNormal) {
		Direction = Vector3.Reflect(Direction, inNormal);
		impactSparks.Play();
		AudioPlayer.Play("BulletReflect", AudioType.SoundEffect, 0.8f, 1.2f, 0.5f);
	}

	public void ResetState() => TakeDamage(this);

	public void Recycle() {
		hitTarget = false;
		lastHitObject = null;
		time = 0;
		lastHitTime = 0;
		bounces = 0;
		Direction = Vector3.zero;
		isFromPlayer = false;
		velocity = baseVelocity;
		hitBox.enabled = true;
		rejectHitBox.enabled = true;
		renderer.enabled = true;
		rig.velocity = Vector3.zero;
		fakeShadow.Show();
		PrefabManager.FreeGameObject(this);
	}

	public static bool IsReflective(Transform t) {
		if(t.transform.CompareTag(ExtraBlocks.Sandbag.ToString()) || t.transform.CompareTag(ExtraBlocks.Tschechigel.ToString())) {
			return false;
		}
		return true;
	}
}
