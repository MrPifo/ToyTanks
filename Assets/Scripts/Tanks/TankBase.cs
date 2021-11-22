using UnityEngine;
using CarterGames.Assets.AudioManager;
using SimpleMan.Extensions;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using System.Collections;
using Shapes;
using DG.Tweening;
using System.Collections.Generic;
using ToyTanks.LevelEditor;
using EpPathFinding.cs;
using UnityEngine.Rendering.HighDefinition;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TankReferences))]
public class TankBase : MonoBehaviour, IHittable, IForceShield {

	public TankAsset tankAsset;
	[Header("VALUES")]
	public short healthPoints;
	public byte moveSpeed = 5;
	public float shootStunDuration = 0.2f;
	[Range(0f, 4f)]
	public float reloadDuration = 0.5f;
	[Range(0f, 4f)]
	public float randomReloadDuration = 1f;
	public short bodyRotSpeed = 360;
	public float aimRotSpeed = 600;
	public short destructionVelocity = 400;
	public bool disable2DirectionMovement;
	public bool makeInvincible;     // Ivincibility for Debugging

	bool isInvincible;
	bool isReloading;
	bool isShootStunned;
	bool isLightTurning;
	protected bool canMove;
	float angleDiff;
	float frontLightIntensity;
	float backLightIntensity;
	float lastTurnSign = 1;
	protected float trackSpawnDistance = 0.75f;
	protected float distanceSinceLastFrame;
	int initLayer;
	Vector3 lastTrackPos;
	Vector3 lastPos;
	Vector3 spawnPos;
	Quaternion spawnRot;
	Vector3 moveDir;
	protected Quaternion lastHeadRot;
	Rigidbody rig;
	MMFeedbacks headShotFeedback;
	List<Vector3> destroyRestPoses;
	List<Quaternion> destroyRestRots;
	Material[] bodyMats;
	Material[] headMats;
	List<GridPos> lastOccupied = new List<GridPos>();

	// Propterties
	public bool HasBeenInitialized { get; set; }
	protected bool CanShoot => !isReloading;
	public bool HasBeenDestroyed { get; set; }
	public bool IsInvincible {
		get => isInvincible || makeInvincible;
		set => isInvincible = value;
	}// Game ivincibility
	public bool IsFriendlyFireImmune => tankAsset.hasFriendlyFireShield;
	public Vector3 Pos => tankBody.position;
	public Vector3 MovingDir => moveDir;
	public Sperlich.Types.Int3 PlacedIndex { get; set; }
	public Sperlich.Types.Int3[] OccupiedIndexes { get; set; }
	public Bullet Bullet { get; set; }
	public TankTypes TankType => tankAsset.tankType;
	private TankReferences References { get; set; }
	protected AudioManager Audio {
		get {
			if(LevelManager.audioManager == null) {
				return GameObject.Find("AudioManager").GetComponent<AudioManager>();
			} else {
				return LevelManager.audioManager;
			}
		}
	}
	protected bool IsPaused => LevelManager.GamePaused || Game.IsTerminal;
	public short MaxHealthPoints => tankAsset.health;
	Transform _trackContainer;
	protected Transform TrackContainer {
		// Ensure there is always a TrackContainer
		get {
			if(_trackContainer != null) {
				return _trackContainer;
			} else {
				GameObject container = GameObject.FindGameObjectWithTag("TrackContainer");
				if(container != null) {
					_trackContainer = container.transform;
					return container.transform;
				} else {
					_trackContainer = new GameObject().transform;
					_trackContainer.tag = "TrackContainer";
					_trackContainer.name = "TrackContainer";
					return _trackContainer;
				}
			}
		}
	}
	protected bool IsStatic => tankAsset.isStatic;

	// Tank References Shortcuts
	public Disc shockwaveDisc => References.shockwaveDisc;
	public Rectangle healthBar => References.healthBar;
	public Transform tankBody => References.tankBody;
	public Transform tankHead => References.tankHead;
	public Transform bulletOutput => References.bulletOutput;
	public Transform billboardHolder => References.billboardHolder;
	public Transform lightHolder => References.lightHolder;
	public HDAdditionalLightData frontLight => References.frontLight;
	public HDAdditionalLightData backLight => References.backLight;
	public GameObject blobShadow => References.blobShadow;
	public GameObject tankTrack => References.tankTrack;
	public GameObject destroyFlash => References.destroyFlash;
	public ForceShield shield => References.shield;
	public List<Transform> destroyTransformPieces => References.destroyTransformPieces;
	public ParticleSystem muzzleFlash => References.muzzleFlash;
	public ParticleSystem sparkDestroyEffect => References.sparkDestroyEffect;
	public ParticleSystem smokeDestroyEffect => References.smokeDestroyEffect;
	public ParticleSystem smokeFireDestroyEffect => References.smokeFireDestroyEffect;
	public ParticleSystem damageSmokeBody => References.damageSmokeBody;
	public ParticleSystem damageSmokeHead => References.damageSmokeHead;
	public ParticleSystem mudParticlesFront => References.mudParticlesFront;
	public ParticleSystem mudParticlesBack => References.mudParticlesBack;
	public ParticleSystem muzzleSmoke => References.muzzleSmoke;
	public AnimationCurve turnLightsOnCurve => References.lightsTurnOnAnim;
	public MMFeedbacks hitFlash => References.hitFlash;

	protected virtual void Awake() {
		References = GetComponent<TankReferences>();
		rig = GetComponent<Rigidbody>();
		headMats = tankHead.GetComponent<MeshRenderer>().sharedMaterials;
		bodyMats = tankBody.GetComponent<MeshRenderer>().sharedMaterials;
		healthBar.transform.parent.gameObject.SetActive(false);
		healthPoints = tankAsset.health;
		frontLightIntensity = frontLight.intensity;
		backLightIntensity = backLight.intensity;
		TurnLightsOff();
	}

	public virtual void InitializeTank() {
		Bullet = References.bullet.GetComponent<Bullet>();
		spawnPos = rig.position;
		spawnRot = rig.rotation;
		initLayer = gameObject.layer;
		lastTrackPos = Pos;
		healthBar.Width = 2f;
		canMove = true;
		HasBeenInitialized = true;
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
		if(HasBeenInitialized) {
			distanceSinceLastFrame = Vector3.Distance(transform.position, lastPos);
			billboardHolder.rotation = Quaternion.LookRotation((Pos - Camera.main.transform.position).normalized, Vector3.up);
			lastPos = transform.position;
			lastHeadRot = tankHead.rotation;
			AdjustLightTurn();
		}
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
		Transform track = Instantiate(tankTrack, TrackContainer).transform;
		track.position = new Vector3(rig.position.x, 0.025f, rig.position.z);
		track.rotation = rig.rotation * Quaternion.Euler(90, 0, 0);

		AudioPlayer.Play("TankDrive", AudioType.SoundEffect, 0.95f, 1.05f, 0.5f);
		if(TrackContainer.childCount > 500) {
			Destroy(TrackContainer.GetChild(0).gameObject);
		}
		return track.position;
	}

	public virtual void ShootBullet() {
		if(!isReloading) {
			var bounceDir = -tankHead.right * 2f;
			bounceDir.y = 0;
			
			muzzleFlash.Play();
			muzzleSmoke.Play();
			tankHead.DOScale(tankHead.localScale + Vector3.one / 7f, 0.2f).SetEase(new AnimationCurve(new Keyframe[] { new Keyframe(0, 0, 0.5f, 0.5f), new Keyframe(0.5f, 1, 0.5f, 0.5f), new Keyframe(1, 0, 0.5f, 0.5f) }));
			if(this is PlayerInput) {
				Instantiate(Bullet).SetupBullet(bulletOutput.forward, bulletOutput.position, true);
			} else {
				Instantiate(Bullet).SetupBullet(bulletOutput.forward, bulletOutput.position, false);
			}
			
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

	public virtual void TakeDamage(IDamageEffector effector) {
		if(IsInvincible == false && HasBeenDestroyed == false) {
			if(this is TankAI && effector.fireFromPlayer == false && IsFriendlyFireImmune) {
				ShieldEffect(effector.damageOrigin);
			} else {
				if(healthPoints > 0) {
					healthPoints--;
				}
				if(healthPoints == 0) {
					GotDestroyed();
				}
				hitFlash.PlayFeedbacks();
				int width = MaxHealthPoints == 0 ? 1 : healthPoints;
				healthBar.Width = 2f / MaxHealthPoints * width;
			}
		} else {
			ShieldEffect(effector.damageOrigin);
		}
	}

	public void Kill() {
		if(HasBeenDestroyed == false) {
			GotDestroyed();
		}
	}
	protected virtual void GotDestroyed() {
		HasBeenDestroyed = true;
		foreach(Transform t in destroyTransformPieces) {
			var rig = t.gameObject.AddComponent<Rigidbody>();
			t.gameObject.layer = LayerMask.NameToLayer("DestructionPieces");
			var vec = new Vector3(Random.Range(-destructionVelocity, destructionVelocity), destructionVelocity, Random.Range(-destructionVelocity, destructionVelocity));
			rig.AddForce(vec);
			rig.AddTorque(vec);
			t.SetParent(null);
		}
		blobShadow.SetActive(false);
		healthBar.gameObject.SetActive(false);
		PlayDestroyExplosion();
		TurnLightsOff();

		if(FindObjectOfType<LevelManager>()) {
			if(this is PlayerInput) {
				LevelManager.PlayerDead();
			} else {
				LevelManager.TankDestroyedCheck();
			}
		}
	}

	public void PlayDestroyExplosion() {
		sparkDestroyEffect.Play();
		smokeDestroyEffect.Play();
		damageSmokeBody.Play();
		damageSmokeHead.Play();
		smokeFireDestroyEffect.Play();
		AudioPlayer.Play("TankExplode", AudioType.SoundEffect, 0.8f, 1.2f, 0.5f);

		if(CompareTag("Player")) {
			AudioPlayer.Play("PlayerTankExplode", AudioType.SoundEffect, 0.8f, 1.2f, 0.5f);
		}
		Camera.main.DOOrthoSize(Camera.main.orthographicSize + 1, 0.15f);
		this.Delay(0.15f, () => Camera.main.DOOrthoSize(Camera.main.orthographicSize - 1, 0.15f));
		LevelManager.Feedback?.TankExplode();
		GameCamera.ShakeExplosion2D(12, 0.3f);
		if(this is PlayerInput) {
			LevelManager.Feedback?.PlayerDead();
		}
		shockwaveDisc.gameObject.SetActive(true);
		
		StartCoroutine(IDestroyAnimate());
	}

	public void ShieldEffect(Vector3 impactPos) {
		shield.gameObject.SetActive(true);
		shield.AddHit(impactPos);
		this.Delay(1, () => shield.gameObject.SetActive(false));
	}

	public virtual void Revive() {
		isShootStunned = false;
		isReloading = false;
		HasBeenDestroyed = false;
		canMove = true;
		healthPoints = MaxHealthPoints;
		blobShadow.SetActive(true);
		healthBar.gameObject.SetActive(true);
		healthBar.transform.parent.gameObject.SetActive(false);
		transform.position = spawnPos;
		transform.rotation = spawnRot;

		int c = 0;
		float respawnDuration = 1.5f;
		DisableCollisions();
		TurnLightsOff();
		foreach(Transform t in destroyTransformPieces) {
			t.DOLocalMove(destroyRestPoses[c], respawnDuration).SetEase(Ease.OutCubic);
			t.DOLocalRotate(destroyRestRots[c].eulerAngles, respawnDuration).SetEase(Ease.OutCubic);
			t.gameObject.layer = initLayer;
			t.parent = transform;
			Destroy(t.gameObject.GetComponent<Rigidbody>());
			c++;
		}
		this.Delay(respawnDuration, () => EnableCollisions());
		
		damageSmokeBody.Stop();
		damageSmokeHead.Stop();
	}

	public void TurnLightsOn() {
		StartCoroutine(ITurnOn());
		IEnumerator ITurnOn() {
			float t = 0;
			while(t < 1f) {
				frontLight.SetIntensity(turnLightsOnCurve.Evaluate(t) * frontLightIntensity);
				backLight.SetIntensity(turnLightsOnCurve.Evaluate(t) * backLightIntensity);
				t += Time.deltaTime;
				yield return null;
			}
		}
	}

	public void TurnLightsOff() {
		frontLight.SetIntensity(0);
		backLight.SetIntensity(0);
	}

	public void AdjustLightTurn() {
		float turn = Mathf.Sign(Vector3.Dot(moveDir.normalized, rig.transform.forward));
		if(turn != lastTurnSign && isLightTurning == false) {
			isLightTurning = true;
			TurnLightsOff();
			this.Delay(0.2f, () => {
				TurnLightsOn();
				isLightTurning = false;
			});

			if(lastTurnSign < 0) {
				lightHolder.localRotation = Quaternion.Euler(new Vector3(0, 0, 0));
			} else {
				lightHolder.localRotation = Quaternion.Euler(new Vector3(0, 180, 0));
			}
			lastTurnSign = turn;
		}
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

	public void DisableCollisions() {
		foreach(Transform t in destroyTransformPieces) {
			if(t.TrySearchComponent(out Collider coll)) {
				coll.enabled = false;
			}
		}
	}
	public void EnableCollisions() {
		foreach(Transform t in destroyTransformPieces) {
			if(t.TrySearchComponent(out Collider coll)) {
				coll.enabled = true;
			}
		}
	}

	protected void AdjustOccupiedGridPos() {
		FreeOccupiedGridPos();

		if(tankAsset.isStatic == false) {
			GridPos current = Game.ActiveGrid.Grid.GetGridPos(Pos);
			if(Game.ActiveGrid.SetWalkable(current, false)) {
				lastOccupied.Add(current);
			}
		} else {
			GridPos one = Game.ActiveGrid.Grid.GetGridPos(Pos);
			if(Mathf.RoundToInt(Mathf.Abs(Pos.x)) % 2 != 0) {
				one.x -= 1;
			}
			if(Mathf.RoundToInt(Mathf.Abs(Pos.z)) % 2 != 0) {
				one.y -= 1;
			}
			GridPos second = new GridPos(one.x + 1, one.y);
			GridPos third = new GridPos(one.x, one.y + 1);
			GridPos fourth = new GridPos(one.x + 1, one.y + 1);
			if(Game.ActiveGrid.SetWalkable(one, false)) {
				lastOccupied.Add(one);
			}
			if(Game.ActiveGrid.SetWalkable(second, false)) {
				lastOccupied.Add(second);
			}
			if(Game.ActiveGrid.SetWalkable(third, false)) {
				lastOccupied.Add(third);
			}
			if(Game.ActiveGrid.SetWalkable(fourth, false)) {
				lastOccupied.Add(fourth);
			}
		}
	}
	protected void FreeOccupiedGridPos() {
		foreach(GridPos occ in lastOccupied) {
			Game.ActiveGrid.SetWalkable(occ, true);
		}
		lastOccupied = new List<GridPos>();
	}

	// Only for Editor purposes
	public void RestoreMaterials() {
		Debug.Log(tankBody);
		tankBody.GetComponent<MeshRenderer>().sharedMaterials = bodyMats;
		tankHead.GetComponent<MeshRenderer>().sharedMaterials = headMats;
	}
	public void SwapMaterial(Material mat) {
		tankBody.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { mat, mat };
		tankHead.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { mat, mat };
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