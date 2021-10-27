using CarterGames.Assets.AudioManager;
using SimpleMan.Extensions;
using Sperlich.Debug.Draw;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour, IHittable, IDamageEffector {

	public float velocity;
	public int maxBounces;
	public LayerMask reflectLayers;
	public ParticleSystem explodeSmoke;
	public ParticleSystem explodeSmokeFire;
	public ParticleSystem explodePieces;
	public ParticleSystem smokeTrail;
	public ParticleSystem smokeFireTrail;
	public ParticleSystem impactSparks;
	public bool showDebug;
	public bool isFromPlayer;
	public bool IsInvincible => false;
	public bool IsFriendlyFireImmune => false;
	GameObject lastHitObject;

	public bool fireFromPlayer => isFromPlayer;
	public Vector3 damageOrigin => transform.position;

	public static Vector3 bulletSize = new Vector3(0.25f, 0.25f, 0.25f);
	List<(Vector3 pos, Vector3 normal)> predictedPath;
	Rigidbody rig;
	float lastHitTime;
	Vector3 direction;
	float time;

	int bounces = 0;
	bool hitTarget;
	bool targetIsTank;
	RaycastHit hit;

	void Awake() {
		rig = GetComponent<Rigidbody>();
	}

	void FixedUpdate() {
		if(LevelManager.GamePaused || Game.IsTerminal) return;

		Vector3 dir = (transform.position - (transform.position - direction)).normalized;
		Quaternion look = Quaternion.LookRotation(dir, transform.up);

		Vector3 pos = Time.fixedDeltaTime * velocity * direction;
		rig.MovePosition(rig.position + pos);
		rig.rotation = look;
		time += Time.fixedDeltaTime;
	}

	[System.Obsolete]
	public void PrecalculatePath() {
		Ray ray = new Ray(transform.position, direction);
		predictedPath = new List<(Vector3, Vector3)>();

		for(int i = 0; i < maxBounces + 1; i++) {
			Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, Mathf.Infinity, reflectLayers);
			predictedPath.Add((hit.transform.position, hit.normal));
			ray = new Ray(hit.point, Vector3.Reflect(ray.direction, hit.normal));
		}
	}

	public void TakeDamage(IDamageEffector effector) {
		velocity = 0;
		hitTarget = true;
		explodeSmoke.Play();
		explodeSmokeFire.Play();
		explodePieces.Play();
		impactSparks.Play();
		smokeTrail.Stop(false, ParticleSystemStopBehavior.StopEmitting);
		smokeFireTrail.Stop(false, ParticleSystemStopBehavior.StopEmitting);
		GetComponent<Collider>().enabled = false;

		new GameObject().AddComponent<DestructionTimer>().Destruct(5, new GameObject[] { explodeSmoke.gameObject, explodeSmokeFire.gameObject, explodePieces.gameObject, smokeTrail.gameObject, smokeFireTrail.gameObject });
		Destroy(gameObject);
	}

	public void SetupBullet(Vector3 dir, Vector3 startPos, bool firedFromPlayer) {
		transform.position = startPos;
		direction = dir;
		isFromPlayer = firedFromPlayer;
		AudioPlayer.Play("BulletShot", 0.8f, 1.2f);
	}

	void OnCollisionEnter(Collision coll) {
		if(!hitTarget && time != lastHitTime) {
			if(Physics.SphereCast(transform.position, 0.15f, direction, out RaycastHit hit, Mathf.Infinity, reflectLayers)) {
				if(hit.transform.gameObject != lastHitObject) {
					bounces++;
					if(coll.transform.TrySearchComponent(out IHittable hittable)) {
						hittable.TakeDamage(this);
						TakeDamage(this);
					} else if(bounces > maxBounces) {
						TakeDamage(this);
					} else {
						Reflect(hit.normal);
						lastHitTime = time;
						lastHitObject = hit.transform.gameObject;
						Debug.DrawRay(rig.position, direction, Color.cyan, 20);
					}
				}
			}
		}
	}

	void Reflect(Vector3 inNormal) {
		Vector3 dir = direction;
		direction = Vector3.Reflect(direction, inNormal);
		Debug.Log(inNormal + " : " + dir + " : " + direction);
		impactSparks.Play();
		AudioPlayer.Play("BulletReflect", 0.8f, 1.2f, 0.5f);
	}
}
