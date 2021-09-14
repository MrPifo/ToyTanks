using UnityEngine;
using CarterGames.Assets.AudioManager;
using SimpleMan.Extensions;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using System.Collections;
using Shapes;
using DG.Tweening;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TankReferences))]
public class TankBase : MonoBehaviour {
	[Header("VALUES")]
	public byte moveSpeed = 5;
	public byte healthPoints = 2;
	public float shootStunDuration = 0.2f;
	[Range(0f, 4f)]
	public float reloadDuration = 0.5f;
	[Range(0f, 4f)]
	public float randomReloadDuration = 1f;
	[Range(0, 360f)]
	public short bodyRotSpeed = 360;
	public short destructionVelocity = 400;
	public bool disable2DirectionMovement;
	public bool makeInvincible;     // Ivincibility for Debugging

	bool isInvincible;
	bool isReloading;
	bool isShootStunned;
	protected bool canMove;
	protected byte maxHealthPoints;
	float angleDiff;
	float aimRotSpeed = 600;
	float angleDiffLock = 600;
	protected float trackSpawnDistance = 0.75f;
	protected float distanceSinceLastFrame;
	int initLayer;
	Vector3 lastTrackPos;
	Vector3 lastPos;
	Vector3 spawnPos;
	Vector3 moveDir;
	protected Quaternion lastHeadRot;
	Rigidbody rig;
	Transform trackContainer;
	MMFeedbacks headShotFeedback;
	List<Vector3> destroyRestPoses;
	List<Quaternion> destroyRestRots;

	// Propterties
	protected bool CanShoot => !isReloading;
	public bool HasBeenDestroyed { get; set; }
	public bool IsInvincible {
		get => isInvincible || makeInvincible;
		set => isInvincible = value;
	}// Game ivincibility
	public Vector3 Pos => tankBody.position;
	public Vector3 MovingDir => moveDir;
	public Bullet Bullet { get; set; }
	private TankReferences References { get; set; }
	protected AudioManager Audio => LevelManager.audioManager;
	protected LevelManager LevelManager { get; set; }

	// Tank References Shortcuts
	public Disc shockwaveDisc => References.shockwaveDisc;
	public Rectangle healthBar => References.healthBar;
	public Transform tankBody => References.tankBody;
	public Transform tankHead => References.tankHead;
	public Transform bulletOutput => References.bulletOutput;
	public Transform billboardHolder => References.billboardHolder;
	public GameObject blobShadow => References.blobShadow;
	public GameObject tankTrack => References.tankTrack;
	public GameObject destroyFlash => References.destroyFlash;
	public List<Transform> destroyTransformPieces => References.destroyTransformPieces;
	public ParticleSystem muzzleFlash => References.muzzleFlash;
	public ParticleSystem sparkDestroyEffect => References.sparkDestroyEffect;
	public ParticleSystem smokeDestroyEffect => References.smokeDestroyEffect;
	public ParticleSystem smokeFireDestroyEffect => References.smokeFireDestroyEffect;
	public ParticleSystem damageSmokeBody => References.damageSmokeBody;
	public ParticleSystem damageSmokeHead => References.damageSmokeHead;
	public ParticleSystem mudParticlesFront => References.mudParticlesFront;
	public ParticleSystem mudParticlesBack => References.mudParticlesBack;
	public MMFeedbacks hitFlash => References.hitFlash;

	protected virtual void Awake() {
		References = GetComponent<TankReferences>();
		rig = GetComponent<Rigidbody>();
		Bullet = References.bullet.GetComponent<Bullet>();
		LevelManager = FindObjectOfType<LevelManager>();
		spawnPos = rig.position;
		initLayer = gameObject.layer;
		trackContainer = GameObject.Find("TrackContainer").transform;
		maxHealthPoints = healthPoints;
		lastTrackPos = Pos;
		healthBar.Width = 2f;
		canMove = true;
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
		distanceSinceLastFrame = Vector3.Distance(transform.position, lastPos);
		billboardHolder.rotation = Quaternion.LookRotation((Pos - Camera.main.transform.position).normalized, Vector3.up);
		lastPos = transform.position;
		lastHeadRot = tankHead.rotation;
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
		//bool moveBlocked = Physics.Raycast(rig.position, moveDir, out RaycastHit blockHit, 2, obstacleLayers);
		if(!isShootStunned && canMove) {
			rig.MovePosition(rig.position + movePos);
		}
		TrackTracer(temp);
	}

	public void AdjustRotation(Vector3 moveDir) {
		if(moveDir == Vector3.zero) return;
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

	void TrackTracer(float temp = 0) {
		float distToLastTrack = Vector2.Distance(new Vector2(lastTrackPos.x, lastTrackPos.z), new Vector2(rig.position.x, rig.position.z));
		if(distToLastTrack > trackSpawnDistance) {
			if(temp == 1) {
				mudParticlesBack.Emit(2);
			} else {
				mudParticlesFront.Emit(2);
			}
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
			
			muzzleFlash.Play();
			var wiggleDir = tankHead.rotation * Vector3.forward * 0.25f;
			tankHead.DOLocalMove(tankHead.localPosition + wiggleDir, 0.1f);
			this.Delay(0.1f, () => tankHead.DOLocalMove(tankHead.localPosition - wiggleDir, 0.1f));
			Instantiate(Bullet).SetupBullet(bulletOutput.forward, bulletOutput.position);
			
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
		if(!IsInvincible) {
			if(healthPoints - 1 <= 0) {
				GotDestroyed();
			}
			healthPoints--;
			hitFlash.PlayFeedbacks();
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
		blobShadow.SetActive(false);
		healthBar.gameObject.SetActive(false);
		PlayDestroyExplosion();
		if(this is PlayerInput) {
			LevelManager.PlayerDead();
		} else {
			LevelManager.TankDestroyedCheck();
		}
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
		Camera.main.DOOrthoSize(Camera.main.orthographicSize + 1, 0.15f);
		this.Delay(0.15f, () => Camera.main.DOOrthoSize(Camera.main.orthographicSize - 1, 0.15f));
		LevelManager.Feedback.TankExplode();
		if(this is PlayerInput) {
			LevelManager.Feedback.PlayerDead();
		}
		shockwaveDisc.gameObject.SetActive(true);
		
		StartCoroutine(IDestroyAnimate());
	}

	public virtual void Revive() {
		isShootStunned = false;
		isReloading = false;
		HasBeenDestroyed = false;
		canMove = true;
		healthPoints = maxHealthPoints;
		blobShadow.SetActive(true);
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