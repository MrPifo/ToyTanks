using UnityEngine;
using SimpleMan.Extensions;
using System.Collections;
using Shapes;
using DG.Tweening;
using System.Collections.Generic;
using ToyTanks.LevelEditor;
using EpPathFinding.cs;
// HDRP Related: using UnityEngine.Rendering.HighDefinition;
using Sperlich.PrefabManager;
using UnityEngine.Rendering.Universal;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TankReferences))]
public class TankBase : GameEntity, IHittable, IResettable, IForceShield {

	public enum TimeMode { DeltaTime, FixedUpdate }

	public TankAsset tankAsset;
	[Header("VALUES")]
	public short healthPoints;
	public byte moveSpeed = 5;
	public float turnSpeed = 5;
	public float shootStunDuration = 0.2f;
	[Range(0f, 4f)]
	public float reloadDuration = 0.5f;
	[Range(0f, 4f)]
	public float randomReloadDuration = 1f;
	public short bodyRotSpeed = 360;
	public float aimRotSpeed = 600;
	public short destructionVelocity = 400;
	public bool disable2DirectionMovement;
	public bool disableTracks;
	public bool makeInvincible;     // Ivincibility for Debugging
	public bool isUsingWheels;
	[SerializeField]
	protected TimeMode APITimeMode = TimeMode.FixedUpdate;
	protected float GetTime {
		get {
			switch(APITimeMode) {
				case TimeMode.DeltaTime:
					return Time.deltaTime;
				case TimeMode.FixedUpdate:
					return Time.fixedDeltaTime;
				default:
					return Time.deltaTime;
			}
		}
	}
	bool isInvincible;
	bool isReloading;
	protected bool isShootStunned;
	bool isLightTurning;
	protected bool canMove;
	protected string trackSound = "TankDrive";
	float angleDiff;
	float frontLightIntensity;
	float backLightIntensity;
	float lastTurnSign = 1;
	[Min(0.1f)]
	public float trackSpawnDistance = 0.75f;
	protected float distanceSinceLastFrame;
	int initLayer;
	Vector3 lastTrackPos;
	Vector3 lastPos;
	Vector3 spawnPos;
	Quaternion spawnRot;
	protected Vector3 moveDir;
	Vector3 directionLeaderRestPos;
	public Vector3 evadeDir;
	protected Quaternion lastHeadRot;
	protected Rigidbody rig;
	protected GroundPainter groundPainter;
	List<Vector3> destroyRestPoses;
	List<Quaternion> destroyRestRots;
	Material[] bodyMats;
	Material[] headMats;
	List<GridPos> lastOccupied = new List<GridPos>();
	GameObject healthPointPrefab;

	// Propterties
	
	public bool HasBeenInitialized { get; set; }
	protected bool CanShoot => !isReloading;
	public bool HasBeenDestroyed { get; set; }
	public bool MuteTrackSound { get; set; }
	public bool IsInvincible {
		get => isInvincible || makeInvincible;
		set => isInvincible = value;
	}// Game ivincibility
	public bool IsFriendlyFireImmune => tankAsset.hasFriendlyFireShield;
	public new Vector3 Pos => tankBody.position;
	public Vector3 MovingDir => moveDir;
	public Sperlich.Types.Int3 PlacedIndex { get; set; }
	public Sperlich.Types.Int3[] OccupiedIndexes { get; set; }
	public TankTypes TankType => tankAsset.tankType;
	protected TankReferences References { get; set; }
	protected bool IsPaused => Game.GamePaused || Game.IsTerminal;
	/// <summary>
	/// Makes moving in opposite direction without rotating possible
	/// </summary>
	protected float currentDirFactor;
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
	public GameObject healthBar => References.healthBar;
	public Transform tankBody => References.tankBody;
	public Transform tankHead => References.tankHead;
	public Transform bulletOutput => References.bulletOutput;
	public Transform billboardHolder => References.billboardHolder;
	public Transform DirectionLeader => References.directionLeader;
	public GameObject blobShadow => References.blobShadow;
	public GameObject tankTrack => References.tankTrack;
	public ForceShield shield => References.shield;
	public List<Transform> destroyTransformPieces => References.destroyTransformPieces;
	public List<Transform> Wheels => References.tankWheels;
	public ParticleSystem muzzleFlash => References.muzzleFlash;
	public ParticleSystem sparkDestroyEffect => References.sparkDestroyEffect;
	public ParticleSystem smokeDestroyEffect => References.smokeDestroyEffect;
	public ParticleSystem smokeFireDestroyEffect => References.smokeFireDestroyEffect;
	public ParticleSystem damageSmokeBody => References.damageSmokeBody;
	public ParticleSystem damageSmokeHead => References.damageSmokeHead;
	public AnimationCurve turnLightsOnCurve => References.lightsTurnOnAnim;
	public DecalProjector FakeShadow => References.fakeShadow;
	public PrefabTypes BulletType => References.bullet;

	protected virtual void Awake() {
		References = GetComponent<TankReferences>();
		rig = GetComponent<Rigidbody>();
		headMats = tankHead.GetComponent<MeshRenderer>().sharedMaterials;
		bodyMats = tankBody.GetComponent<MeshRenderer>().sharedMaterials;
		healthPoints = tankAsset.health;
		directionLeaderRestPos = DirectionLeader.position;
		healthPointPrefab = healthBar.transform.GetChild(0).gameObject;
		healthPointPrefab.SetActive(false);
		TurnLightsOff();
	}

	public virtual void InitializeTank() {
		spawnPos = rig.position;
		spawnRot = rig.rotation;
		initLayer = gameObject.layer;
		lastTrackPos = Pos;
		canMove = true;
		HasBeenInitialized = true;
		shockwaveDisc.gameObject.SetActive(false);
		destroyRestPoses = new List<Vector3>();
		destroyRestRots = new List<Quaternion>();
		groundPainter = FindObjectOfType<GroundPainter>();

		foreach(Transform t in destroyTransformPieces) {
			destroyRestPoses.Add(t.localPosition);
			destroyRestRots.Add(t.localRotation);
		}
		healthPointPrefab.SetActive(true);
		for(int i = 0; i < MaxHealthPoints - 1; i++) {
			Instantiate(healthPointPrefab, healthBar.transform);
		}
		healthBar.SetActive(false);
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
	public virtual void Move(Vector2 inputDir) { }

	/// <summary>
	/// Call this to make the wheels rotate to the corresponding movement
	/// </summary>
	public void UpdateWheelRotation() {
		foreach(Transform t in Wheels) {
			t.Rotate(-Vector3.right, 50 * distanceSinceLastFrame);
		}
	}

	public void RotateTank(Vector3 moveDir) {
		if(moveDir == Vector3.zero) return;
		Quaternion rot = Quaternion.LookRotation(moveDir, Vector3.up);
		if(Mathf.Abs(evadeDir.x) > 0.2f && Mathf.Abs(evadeDir.z) > 0.2f) {
			rot = Quaternion.LookRotation(evadeDir, Vector3.up);
		}

		angleDiff = Quaternion.Angle(rig.rotation, rot);
		if(angleDiff > 0) {
			rot = Quaternion.RotateTowards(rig.rotation, rot, bodyRotSpeed * GetTime);
			rig.MoveRotation(rot);
		}
	}

	public void MoveHead(Vector3 target, bool ignoreLerp = false) {
		if((target - Pos).normalized != Vector3.zero) {
			target.y = tankHead.position.y;
			var rot = Quaternion.LookRotation((target - Pos).normalized, Vector3.up);
			if(Quaternion.identity == rot)
				return;
			if(ignoreLerp == false) {
				rot = Quaternion.RotateTowards(tankHead.rotation, rot, GetTime * aimRotSpeed);
			}
			tankHead.rotation = rot;
		}
	}

	public Vector3 GetLookDirection(Vector3 lookTarget) => (new Vector3(lookTarget.x, 0, lookTarget.z) - new Vector3(Pos.x, 0, Pos.z)).normalized;

	protected void TrackTracer(float temp = 0) {
		float distToLastTrack = Vector2.Distance(new Vector2(lastTrackPos.x, lastTrackPos.z), new Vector2(rig.position.x, rig.position.z));
		if(distToLastTrack > trackSpawnDistance && disableTracks == false) {
			// TODO: May be replaced or removed
			if(temp == 1) {
				//mudParticlesBack.Emit(2);
			} else {
				//mudParticlesFront.Emit(2);
			}
			lastTrackPos = SpawnTrack();
		}
	}

	Vector3 SpawnTrack() {
		//groundPainter.PaintTrack(new Vector2(Pos.x, Pos.z), transform.forward);
		if(MuteTrackSound == false) {
			AudioPlayer.Play(trackSound, AudioType.SoundEffect, 0.95f, 1.05f, 0.4f);
		}
		//return rig.position;
		Transform track = PrefabManager.Spawn(PrefabTypes.TankTrack).transform;
		track.position = new Vector3(rig.position.x, 0.025f, rig.position.z);
		track.rotation = rig.rotation * Quaternion.Euler(90, 0, 0);

		
		int despawnTime = 30;
		track.GetComponent<PoolGameObject>().Recycle(despawnTime);
		SpriteRenderer rend = track.GetComponent<SpriteRenderer>();
		rend.DOFade(0.7f, 0.02f);
		this.Delay(despawnTime - 2, () => {
			rend.DOFade(0, 2);
		});
		return track.position;
	}

	public virtual Bullet ShootBullet() {
		if(!isReloading) {
			var bounceDir = -tankHead.right * 2f;
			bounceDir.y = 0;

			Bullet bullet;
			muzzleFlash.Play();
			tankHead.DOScale(tankHead.localScale + Vector3.one / 7f, Mathf.Min(reloadDuration, 0.2f)).SetEase(new AnimationCurve(new Keyframe[] { new Keyframe(0, 0, 0.5f, 0.5f), new Keyframe(0.5f, 1, 0.5f, 0.5f), new Keyframe(1, 0, 0.5f, 0.5f) }));
			if(this is PlayerInput) {
				bullet = PrefabManager.Spawn<Bullet>(BulletType, null, bulletOutput.position).SetupBullet(bulletOutput.forward, bulletOutput.position, true);
			} else {
				bullet = PrefabManager.Spawn<Bullet>(BulletType, null, bulletOutput.position).SetupBullet(bulletOutput.forward, bulletOutput.position, false);
			}
			
			if(reloadDuration > 0) {
				isReloading = true;
				this.Delay(reloadDuration + Random.Range(0f, randomReloadDuration), () => isReloading = false);
			}
			if(shootStunDuration > 0) {
				isShootStunned = true;
				this.Delay(shootStunDuration, () => isShootStunned = false);
			}
			return bullet;
		}
		return null;
	}

	public virtual void TakeDamage(IDamageEffector effector, bool instantKill = false) {
		if(IsInvincible == false && HasBeenDestroyed == false && Game.IsGameRunning) {
			if(this is TankAI && effector.fireFromPlayer == false && IsFriendlyFireImmune) {
				ShieldEffect(effector.damageOrigin);
			} else {
				if(healthPoints > 0) {
					healthPoints--;
					foreach(var t in destroyTransformPieces) {
						foreach(var mat in t.GetComponent<MeshRenderer>().materials) {
							mat.SetFloat("HitEffect", 1);
							DOTween.To(() => mat.GetFloat("HitEffect"), x => mat.SetFloat("HitEffect", x), 1, 0.05f).OnComplete(() => {
								DOTween.To(() => mat.GetFloat("HitEffect"), x => mat.SetFloat("HitEffect", x), 0, 0.05f);
							});
						}
					}
				}
				if(healthPoints == 0 || instantKill) {
					healthPoints = 0;
					GotDestroyed();
				}

				healthBar.transform.GetChild(healthPoints).GetComponent<CanvasGroup>().DOFade(0, 0.2f).SetEase(Ease.OutCubic);
				healthBar.transform.GetChild(healthPoints).DOScale(0f, 0.2f).OnComplete(() => healthBar.transform.GetChild(healthPoints).gameObject.SetActive(false));
				if(this is BossAI == false) {
					healthBar.SetActive(true);
				}
			}
		} else {
			ShieldEffect(effector.damageOrigin);
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
		PlayDestroyExplosion();
		TurnLightsOff();
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
		GameCamera.ShakeExplosion2D(12, 0.3f);
		shockwaveDisc.gameObject.SetActive(true);
		shockwaveDisc.Radius = 0;

		float thickness = shockwaveDisc.Thickness;
		DOTween.To(() => shockwaveDisc.Radius, x => shockwaveDisc.Radius = x, thickness, 1);
		DOTween.To(() => shockwaveDisc.Thickness, x => shockwaveDisc.Thickness = x, 0, 1);
	}

	public void ShieldEffect(Vector3 impactPos) {
		shield.gameObject.SetActive(true);
		shield.AddHit(impactPos);
		this.Delay(1, () => shield.gameObject.SetActive(false));
	}

	public void TurnLightsOn() {
		StartCoroutine(ITurnOn());
		IEnumerator ITurnOn() {
			float t = 0;
			while(t < 1f) {
				t += Time.deltaTime;
				yield return null;
			}
		}
	}

	public void TurnLightsOff() {
		// HDRP Relate: frontLight.SetIntensity(0);
		// HDRP Relate: backLight.SetIntensity(0);
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

			lastTurnSign = turn;
		}
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

	public virtual void ResetState() {
		Debug.Log("<color=blue>RESET: " + name + "</color>");
		TankBase newTank = Instantiate(tankAsset.prefab).GetComponent<TankBase>();
		newTank.transform.position = spawnPos;
		newTank.transform.rotation = spawnRot;
		newTank.OccupiedIndexes = OccupiedIndexes;
		newTank.PlacedIndex = PlacedIndex;
		newTank.transform.parent = transform.parent;
		if(this is not BossAI) {
			newTank.AnimateAssembly();
		}

		if(newTank is PlayerInput) {
			PlayerInput input = (PlayerInput)newTank;
			input.DisableCrossHair();
		}
		CleanUpDestroyedPieces();
		Destroy(gameObject);
	}
	public void CleanUpDestroyedPieces() {
		foreach(Transform piece in destroyTransformPieces) {
			Destroy(piece.gameObject);
		}
	}

	public void AnimateAssembly() {
		canMove = false;
		var parts = destroyTransformPieces.OrderBy(t => t.position.y).ToList();
		var pos = parts.Select(t => t.localPosition).ToList();
		for(int i = 0; i < parts.Count; i++) {
			parts[i].transform.localPosition += new Vector3(0, (i + 1) * 2, 0);
		}
		float timeDelay = Random.Range(1, 8);
		for(int i = 0; i < parts.Count; i++) {
			int p = i;
			this.Delay(0.1f * (Time.deltaTime + i) * timeDelay, () => {
				parts[p].DOLocalMove(pos[p], 0.45f).SetEase(Ease.OutBounce);
				AudioPlayer.Play("TankAssemblyClick", AudioType.SoundEffect, 1f, 1f);
				this.Delay(1f + timeDelay, () => canMove = true);
			});
		}
	}

	// Only for Editor purposes
	public void RestoreMaterials() {
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
			ResetState();
		}
		LevelManager.Instance.UI.playerLives.SetText(Random.Range(0, 5).ToString());
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
		if(GUILayout.Button("Play Destroy Effect")) {
			builder.PlayDestroyExplosion();
		}
		if(GUILayout.Button("Revive")) {
			builder.disableControl = false;
			builder.ResetState();
		}
	}
}
#endif