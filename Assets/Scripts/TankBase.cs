﻿using UnityEngine;
using UnityEngine.VFX;
using CarterGames.Assets.AudioManager;
using SimpleMan.Extensions;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using System.Collections;
using static Sperlich.Debug.Draw.Draw;
using Shapes;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MMFeedbacks))]
public class TankBase : MonoBehaviour {
	[Header("VALUES")]
	public int moveSpeed;
	public int healthPoints;
	public float bodyRotSpeed;
	public float aimRotSpeed;
	public float angleDiffLock;
	public float shootStunDuration;
	[Range(0f, 4f)]
	public float reloadDuration = 1f;
	[Range(0f, 4f)]
	public float randomReloadDuration = 1f;
	public float trackSpawnDistance;
	public float destructionVelocity;
	public Bullet bullet;
	public LayerMask hitLayers;
	public LayerMask obstacleLayers;
	public bool disable2DirectionMovement;
	public bool makeInvincible;
	[Header("REFERENCES")]
	public Transform tankBody;
	public Transform tankHead;
	public Transform bulletOutput;
	public GameObject tankTrack;
	public ParticleSystem muzzleFlash;
	public Transform billboardHolder;
	public Rectangle healthBar;
	public List<Transform> destroyTransformPieces;
	[Header("Explosion Effects")]
	public GameObject destroyFlash;
	public ParticleSystem sparkDestroyEffect;
	public ParticleSystem smokeDestroyEffect;
	public ParticleSystem smokeFireDestroyEffect;
	public ParticleSystem damageSmokeBody;
	public ParticleSystem damageSmokeHead;
	public Disc shockwaveDisc;
	Vector3 moveDir;
	public Vector3 Pos => tankBody.position;
	public Vector3 MovingDir => moveDir;
	public bool CanShoot => !isReloading;
	public bool HasBeenDestroyed { get; set; }
	Rigidbody rig;
	MMFeedbacks feedback;
	MMWiggle headWiggle;
	Transform trackContainer;
	AudioManager Audio => LevelManager.audioManager;
	int muzzleFlashDelta;
	float angleDiff;
	Vector3 lastTrackPos;
	Vector3 spawnPos;
	int initLayer;
	float engineVolume;
	protected int maxHealthPoints;
	bool isReloading;
	bool isShootStunned;
	List<Vector3> destroyRestPoses;
	List<Quaternion> destroyRestRots;

	protected virtual void Awake() {
		rig = GetComponent<Rigidbody>();
		feedback = GetComponent<MMFeedbacks>();
		headWiggle = tankHead.GetComponent<MMWiggle>();
		spawnPos = rig.position;
		initLayer = gameObject.layer;
		trackContainer = GameObject.Find("TrackContainer").transform;
		maxHealthPoints = healthPoints;
		lastTrackPos = Pos;
		healthBar.Width = 2f;
		healthBar.transform.parent.gameObject.SetActive(false);
		shockwaveDisc.gameObject.SetActive(false);
		destroyRestPoses = new List<Vector3>();
		destroyRestRots = new List<Quaternion>();

		foreach(Transform t in destroyTransformPieces) {
			destroyRestPoses.Add(t.localPosition);
			destroyRestRots.Add(t.localRotation);
		}
	}

	protected virtual void LateUpdate() {
		billboardHolder.rotation = Quaternion.LookRotation((Pos - Camera.main.transform.position).normalized, Vector3.up);
	}

	public void Move() => Move(new Vector3(transform.forward.x, 0, transform.forward.z));
	public void Move(Vector3 moveDir) => Move(new Vector2(moveDir.x, moveDir.z));
	public void Move(Vector2 inputDir) {
		moveDir = new Vector3(inputDir.x, 0, inputDir.y);
		rig.velocity = Vector3.zero;
		float temp = Mathf.Sign(Vector3.Dot(moveDir.normalized, rig.transform.forward));
		if(disable2DirectionMovement) {
			temp = 1;
		}
		AdjustRotation(moveDir * temp);

		var movePos = temp * moveSpeed * Time.deltaTime * rig.transform.forward;
		bool moveBlocked = Physics.Raycast(rig.position, moveDir, out RaycastHit blockHit, 2, obstacleLayers);
		if(!moveBlocked && !isShootStunned) {
			rig.MovePosition(rig.position + movePos);
		}
		TrackTracer();
	}

	public void AdjustRotation(Vector3 moveDir) {
		var rot = Quaternion.LookRotation(moveDir, Vector3.up);
		angleDiff = Quaternion.Angle(rig.rotation, rot);
		if(angleDiff > 0) {
			rot = Quaternion.RotateTowards(rig.rotation, rot, bodyRotSpeed * Time.deltaTime);
			rig.MoveRotation(rot);
		}
	}

	public void MoveHead(Vector3 target) {
		target.y = tankHead.position.y;
		var rot = Quaternion.LookRotation((target - Pos).normalized, Vector3.up);
		rot = Quaternion.RotateTowards(tankHead.rotation, rot, Time.deltaTime * aimRotSpeed);
		tankHead.rotation = rot;
	}

	public Vector3 GetLookDirection(Vector3 lookTarget) => (new Vector3(lookTarget.x, 0, lookTarget.z) - new Vector3(Pos.x, 0, Pos.z)).normalized;

	void TrackTracer() {
		float distToLastTrack = Vector2.Distance(new Vector2(lastTrackPos.x, lastTrackPos.z), new Vector2(rig.position.x, rig.position.z));
		if(distToLastTrack > trackSpawnDistance) {
			lastTrackPos = SpawnTrack();
		}
	}

	Vector3 SpawnTrack() {
		Transform track = Instantiate(tankTrack, trackContainer).transform;
		track.position = new Vector3(rig.position.x, 0.025f, rig.position.z);
		track.rotation = rig.rotation * Quaternion.Euler(90, 0, 0);

		Audio.Play("TankDrive", 0.5f, Random.Range(1f, 1.1f));
		return track.position;
	}

	public void ShootBullet() {
		if(!isReloading) {
			var bounceDir = -tankHead.right * 2f;
			bounceDir.y = 0;
			headWiggle.PositionWiggleProperties.AmplitudeMin = bounceDir;
			headWiggle.PositionWiggleProperties.AmplitudeMax = bounceDir;
			muzzleFlash.Play();
			feedback.PlayFeedbacks();
			Instantiate(bullet).SetupBullet(bulletOutput.forward, bulletOutput.position);
			
			if(reloadDuration > 0) {
				isReloading = true;
				this.Delay(reloadDuration + Random.Range(0f, randomReloadDuration), () => isReloading = false);
			}
			if(shootStunDuration > 0) {
				isShootStunned = true;
				this.Delay(shootStunDuration, () => isShootStunned = false);
			}
		}
	}

	public virtual void GotHitByBullet() {
		if(!LevelManager.playerDeadGameOver && !makeInvincible) {
			if(healthPoints - 1 <= 0) {
				GotDestroyed();
			}
			healthPoints--;
			int width = maxHealthPoints == 0 ? 1 : healthPoints;
			healthBar.Width = 2f / maxHealthPoints * width;
		}
	}

	public void GotDestroyed() {
		healthPoints--;
		HasBeenDestroyed = true;
		foreach(Transform t in destroyTransformPieces) {
			t.parent = null;
			var rig = t.gameObject.AddComponent<Rigidbody>();
			t.gameObject.layer = LayerMask.NameToLayer("DestructionPieces");
			var vec = new Vector3(Random.Range(-destructionVelocity, destructionVelocity), destructionVelocity, Random.Range(-destructionVelocity, destructionVelocity));
			rig.AddForce(vec);
			rig.AddTorque(vec);
		}
		healthBar.gameObject.SetActive(false);
		FindObjectOfType<LevelManager>().TankDestroyedCheck();
		PlayDestroyExplosion();
	}

	public void PlayDestroyExplosion() {
		sparkDestroyEffect.Play();
		smokeDestroyEffect.Play();
		damageSmokeBody.Play();
		damageSmokeHead.Play();
		smokeFireDestroyEffect.Play();
		Audio.Play("TankExplode", 0.5f, Random.Range(0.9f, 1.1f));
		if(CompareTag("Player")) {
			Audio.Play("PlayerTankExplode", 0.5f, Random.Range(0.9f, 1.1f));
		}
		LevelManager.Feedback.TankExplode();
		if(this is PlayerInput) {
			LevelManager.Feedback.PlayerDead();
		}
		shockwaveDisc.gameObject.SetActive(true);
		
		StartCoroutine(IDestroyAnimate());
	}

	public virtual void Revive() {
		LevelManager.playerDeadGameOver = false;
		healthPoints = maxHealthPoints;
		HasBeenDestroyed = false;
		healthBar.gameObject.SetActive(true);
		healthBar.transform.parent.gameObject.SetActive(false);
		transform.position = spawnPos;

		int c = 0;
		foreach(Transform t in destroyTransformPieces) {
			t.position = Vector3.zero;
			t.rotation = Quaternion.identity;
			t.parent = transform;
			t.localPosition = destroyRestPoses[c];
			t.localRotation = destroyRestRots[c];
			t.gameObject.layer = initLayer;
			Destroy(t.gameObject.GetComponent<Rigidbody>());
			c++;
		}
		damageSmokeBody.Stop();
		damageSmokeHead.Stop();
	}

	IEnumerator IDestroyAnimate() {
		float time = 0f;
		float thickness = shockwaveDisc.Thickness;
		destroyFlash.SetActive(true);
		var rend = destroyFlash.GetComponent<MeshRenderer>();
		var flashMat = new Material(rend.sharedMaterial);
		rend.sharedMaterial = flashMat;
		flashMat.SetColor("_BaseColor", Color.white);
		while(time < 1f) {
			shockwaveDisc.Radius = MMTween.Tween(time, 0, 1f, 0, 10, MMTween.MMTweenCurve.EaseOutCubic);
			shockwaveDisc.Thickness = MMTween.Tween(time, 0, 1f, thickness, 0, MMTween.MMTweenCurve.EaseOutCubic);
			float flashScale = MMTween.Tween(time, 0f, 0.3f, 0f, 1f, MMTween.MMTweenCurve.EaseOutCubic);
			float flashAlpha = MMTween.Tween(time, 0f, 0.3f, 1f, 0f, MMTween.MMTweenCurve.EaseOutCubic);
			flashMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, flashAlpha));
			if(time >= 0.3f) {
				destroyFlash.SetActive(false);
			} else {
				destroyFlash.transform.localScale = new Vector3(flashScale, flashScale, flashScale);
			}
			time += Time.deltaTime;
			yield return null;
		}
		shockwaveDisc.Thickness = thickness;
		shockwaveDisc.Radius = 0f;
		shockwaveDisc.gameObject.SetActive(false);
	}

#if UNITY_EDITOR
	public void DebugDestroy() {
		if(HasBeenDestroyed) {
			Revive();
		}
		LevelManager.UI.playerLives.SetText(Random.Range(0, 5).ToString());
		LevelManager.Feedback.PlayLives();
		this.Delay(0.1f, () => GotDestroyed());
	}
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(PlayerInput))]
public class TankBaseDebugEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		PlayerInput builder = (PlayerInput)target;
		if(GUILayout.Button("Destroy")) {
			builder.DebugDestroy();
		}
		if(GUILayout.Button("Revive")) {
			builder.disableControl = false;
			builder.Revive();
		}
	}
}
#endif