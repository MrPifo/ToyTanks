using CarterGames.Assets.AudioManager;
using SimpleMan.Extensions;
using Sperlich.Debug.Draw;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour {

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
	public static Vector3 bulletSize = new Vector3(0.25f, 0.25f, 0.25f);
	List<(Vector3 pos, Vector3 normal)> predictedPath;
	Rigidbody rig;
	float lastHitTime;
	Vector3 direction;
	float time;

	int bounces = 0;
	bool hitTarget;
	bool targetIsTank;

	void Awake() {
		rig = GetComponent<Rigidbody>();
	}

	void FixedUpdate() {
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

	public void Destroy() {
		velocity = 0;
		hitTarget = true;
		explodeSmoke.Play();
		explodeSmokeFire.Play();
		explodePieces.Play();
		impactSparks.Play();
		smokeTrail.Stop(false, ParticleSystemStopBehavior.StopEmitting);
		smokeFireTrail.Stop(false, ParticleSystemStopBehavior.StopEmitting);

		new GameObject().AddComponent<DestructionTimer>().Destruct(5, new GameObject[] { explodeSmoke.gameObject, explodeSmokeFire.gameObject, explodePieces.gameObject, smokeTrail.gameObject, smokeFireTrail.gameObject });
		Destroy(gameObject);
	}

	public void SetupBullet(Vector3 dir, Vector3 startPos) {
		transform.position = startPos;
		direction = dir;
		AudioManager.instance.Play("BulletShot", 0.25f, Random.Range(0.9f, 1.1f));
		this.Delay(15, () => Destroy(gameObject));
	}

	void OnCollisionEnter(Collision coll) {
		if(!hitTarget && time != lastHitTime) {
			if(Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, Mathf.Infinity, reflectLayers)) {
				bounces++;
				if(coll.transform.TryGetComponent(out TankBase tank)) {
					tank.GotHitByBullet();
					Destroy();
				} else if(bounces > maxBounces) {
					Destroy();
				} else if(coll.transform.TryGetComponent(out Bullet bullet) && coll.transform.gameObject != gameObject) {
					bullet.Destroy();
					Destroy();
				} else {
					Reflect(hit.normal);
					lastHitTime = time;
					Debug.DrawRay(rig.position, direction, Color.cyan, 20);
				}
			}
		}
	}

	void Reflect(Vector3 inNormal) {
		direction = Vector3.Reflect(direction, inNormal);
		impactSparks.Play();
		AudioManager.instance.Play("BulletReflect", 0.5f, Random.Range(0.8f, 1.2f));
	}
}
