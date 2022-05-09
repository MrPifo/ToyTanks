using SimpleMan.Extensions;
using Sperlich.PrefabManager;
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
	protected GameObject lastHitObject;
	public GameObject Owner { get; protected set; }
	public UnityEvent OnBulletDestroyed { get; private set; }
	public float Pitch { get; set; } = 1f;

	public bool fireFromPlayer => isFromPlayer;
	public Vector3 damageOrigin => transform.position;

	public static Vector3 bulletSize = new Vector3(0.25f, 0.25f, 0.25f);
	List<(Vector3 pos, Vector3 normal)> predictedPath;
	Rigidbody rig;
	protected float lastHitTime;
	public Vector3 Direction { get; set; }
	public Vector3 CurrentDirection { get; set; }
	public PoolData.PoolObject PoolObject { get; set; }
	public bool IsHittable { get; set; } = true;

	protected float baseVelocity;
	protected float time;
	protected int bounces = 0;
	protected bool hitTarget;

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
			CurrentDirection = (transform.position - (transform.position - Direction)).normalized;
			fakeShadow.transform.rotation = Quaternion.Euler(90, 0, 0);
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
		if (IsHittable || instantKill) {
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
	}

	public virtual Bullet SetupBullet(GameObject owner, Vector3 dir, Vector3 startPos, bool firedFromPlayer, float customPitch = 1f) {
		IsHittable = true;
		Owner = owner;
		transform.position = startPos;
		Direction = dir;
		isFromPlayer = firedFromPlayer;
		float pitch = customPitch == 1f ? 1.2f : customPitch;
		AudioPlayer.Play(JSAM.Sounds.BulletShot, AudioType.SoundEffect, 0.8f * pitch, 1.2f * pitch, 1f);
		return this;
	}

	protected virtual void OnCollisionEnter(Collision coll) {
		if(hitTarget == false && time != lastHitTime) {
			if(Physics.SphereCast(transform.position, 0.15f, Direction, out RaycastHit hit, Mathf.Infinity, reflectLayers)) {
				if(hit.transform.gameObject != lastHitObject) {
					bounces++;
					if(coll.transform.TrySearchComponent(out IHittable hittable) && hittable.IsHittable == false) {
						Physics.IgnoreCollision(hitBox, coll.collider, true);
						return;
					}
					if(hittable != null && IsBulletBlocker(coll.collider.transform) == false && IsBulletReflector(coll.collider.transform) == false) {
						hittable.TakeDamage(this);
						TakeDamage(this);
					} else if(bounces > maxBounces) {
						TakeDamage(this);
					} else if(IsBulletBlocker(coll.collider.transform) && IsBulletReflector(coll.collider.transform) == false) {
						AudioPlayer.Play(JSAM.Sounds.BulletRicochet, AudioType.SoundEffect, 0.8f * Pitch, 1.2f * Pitch, 1.5f);
						TakeDamage(this);
					} else {
						Reflect(hit.normal);
						lastHitTime = time;
						lastHitObject = hit.transform.gameObject;
					}
				}
			}
		}
	}

	protected void Reflect(Vector3 inNormal) {
		Direction = Vector3.Reflect(Direction, inNormal);
		impactSparks.Play();
		AudioPlayer.Play(JSAM.Sounds.BulletReflect, AudioType.SoundEffect, 0.8f, 1.2f, 0.5f);
	}

	public void ResetState() => TakeDamage(this);
	public void Recycle() {
		hitTarget = false;
		lastHitObject = null;
		time = 0;
		lastHitTime = 0;
		bounces = 0;
		Pitch = 1f;
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
	public static bool IsBulletBlocker(Transform t) {
		if(t.CompareTag("BulletBlocker")) {
			return true;
        }
		return false;
    }
	public static bool IsBulletReflector(Transform t) {
		if (t.CompareTag("BulletReflector")) {
			return true;
		}
		return false;
	}
}
