using CarterGames.Assets.AudioManager;
using SimpleMan.Extensions;
using Sperlich.Debug.Draw;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Bullet : MonoBehaviour {

	public float velocity;
	public int maxBounces;
	public LayerMask affectLayers;
	public LayerMask reflectLayers;
	public ParticleSystem Explosion;
	public ParticleSystem ExplosionFire;
	public ParticleSystem ExplosionPieces;
	public ParticleSystem SmokeTrail;
	public ParticleSystem FireTrail;
	public bool showDebug;
	public static Vector3 bulletSize = new Vector3(0.1f, 0.1f, 0.1f);
	List<(Vector3 pos, Vector3 normal)> predictedPath;
	Rigidbody rig;
	float lastHitTime;
	Vector3 direction;
	float time;

	int bounces = 0;
	bool hitTarget;

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
		Explosion.Play();
		ExplosionFire.Play();
		ExplosionPieces.Play();
		//LevelManager.Feedback.PlayBulletExplode();

		new GameObject().AddComponent<DestructionTimer>().Destruct(5, new GameObject[] { Explosion.gameObject, ExplosionFire.gameObject, ExplosionPieces.gameObject });
		AudioManager.instance.Play("BulletExplode");
		gameObject.SetActive(false);
		Destroy(gameObject);
	}

	public void SetupBullet(Vector3 dir, Vector3 startPos) {
		transform.position = startPos;
		direction = dir;
		AudioManager.instance.Play("BulletShot");
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
		AudioManager.instance.Play("BulletReflect", 0.5f, Random.Range(0.8f, 1.2f));
	}
}
