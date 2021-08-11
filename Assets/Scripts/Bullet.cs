using CarterGames.Assets.AudioManager;
using SimpleMan.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour {

	public float velocity;
	public int maxBounces;
	public LayerMask affectLayers;
	public ParticleSystem Explosion;
	public ParticleSystem ExplosionFire;
	public ParticleSystem ExplosionPieces;
	public ParticleSystem SmokeTrail;
	public ParticleSystem FireTrail;
	Rigidbody rig;
	SphereCollider coll;
	Vector3 direction;

	int bounces = 0;
	bool denyCollision;

	void Awake() {
		rig = GetComponent<Rigidbody>();
		coll = GetComponent<SphereCollider>();
	}

	void FixedUpdate() {
		Vector3 dir = (transform.position - (transform.position - direction)).normalized;
		Quaternion look = Quaternion.LookRotation(dir, transform.up);

		Vector3 pos = direction * velocity * Time.fixedDeltaTime;
		rig.MovePosition(rig.position + pos);
		rig.MoveRotation(look);
	}

	public void Destroy() {
		velocity = 0;
		Explosion.Play();
		ExplosionFire.Play();
		ExplosionPieces.Play();

		new GameObject().AddComponent<DestructionTimer>().Destruct(5, new GameObject[] { Explosion.gameObject, ExplosionFire.gameObject, ExplosionPieces.gameObject });
		AudioManager.instance.Play("BulletExplode");
		gameObject.SetActive(false);
		Destroy(gameObject);
	}

	public void ReceiveDestroy() {
		Destroy();
	}

	public void SetupBullet(Vector3 dir, Vector3 startPos) {
		transform.position = startPos;
		direction = dir;
		AudioManager.instance.Play("BulletShot");
		this.Delay(15, () => Destroy(gameObject));
	}

	void OnCollisionEnter(Collision coll) {
		coll.collider.enabled = false;
		if(!denyCollision) {
			bounces++;
			if(coll.transform.TryGetComponent(out TankBase tank)) {
				tank.GotHitByBullet();
				Destroy();
			} else if(bounces > maxBounces) {
				Destroy();
			} else if(coll.transform.TryGetComponent(out Bullet bullet) && coll.gameObject != gameObject) {
				bullet.Destroy();
				Destroy();
			} else {
				Reflect(coll.contacts[0].normal);
			}
		}
		coll.collider.enabled = true;
	}

	void Reflect(Vector3 inNormal) {
		direction = Vector3.Reflect(direction, inNormal);
		rig.position += direction * velocity * Time.fixedDeltaTime * 1.5f;
		denyCollision = true;
		AudioManager.instance.Play("BulletReflect", 0.5f, Random.Range(0.8f, 1.2f));
		this.Delay(0.25f, () => denyCollision = false);
	}
}
