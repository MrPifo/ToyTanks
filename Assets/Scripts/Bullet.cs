using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour {
	public GameObject bulletMesh;
	public GameObject bulletExplosion;
	public GameObject destructor;
	public MeshRenderer MeshRend;
	public AudioSource BulletReflect;
	public AudioSource BulletShot;
	public AudioSource BulletReload;
	public AudioSource BulletExplode;
	public ParticleSystem Explosion;
	public ParticleSystem ExplosionFire;
	public ParticleSystem ExplosionPieces;
	public ParticleSystem SmokeTrail;
	public ParticleSystem FireTrail;
	public Vector3 direction;
	public bool directionIsSet;
	public Collider BulletCollider;
	public TankBase TankScript;
	public bool ShotByBot;
	public float velocity;
	public int maxBounces;
	public int bouncedTimes;
	public bool CanImpactOnPlayer;
	public bool BounceCooldown;
	public bool destroyed;

	void Awake() {

	}

	void FixedUpdate() {
		Vector3 dir = (transform.position - (transform.position - direction)).normalized;
		Quaternion look = Quaternion.LookRotation(dir, transform.up);
		transform.rotation = look;

		gameObject.transform.position += direction * velocity;
	}

	public void Destroyed() {
		destroyed = true;
		velocity = 0;
		Explosion.Play();
		ExplosionFire.Play();
		BulletExplode.Play();
		ExplosionPieces.Play();
		BulletCollider.enabled = false;
		MeshRend.enabled = false;
		DestructionTimer o = Instantiate(destructor.gameObject).GetComponent<DestructionTimer>();
		o.Destruct(5);
		o.SetChild(Explosion.gameObject);
		o.SetChild(ExplosionFire.gameObject);
		o.SetChild(BulletExplode.gameObject);
		o.SetChild(ExplosionPieces.gameObject);
		Destroy(gameObject);
		Debug.Log("BULLET DESTROYED");
	}

	public void ReceiveDestroy() {
		Destroyed();
	}

	public void SetupBullet(Vector3 dir, Vector3 startPos, int objectId = -1) {
		transform.position = startPos;
		direction = dir;
		directionIsSet = true;
	}

	public void SetDirection(Vector3 dir) {
		direction = dir;
		directionIsSet = true;
	}

	void OnCollisionEnter(Collision collision) {

	}
}
