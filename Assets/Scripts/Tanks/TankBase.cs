using UnityEngine;
using SimpleMan.Extensions;
using Shapes;
using DG.Tweening;
using System.Collections.Generic;
using ToyTanks.LevelEditor;
using Sperlich.PrefabManager;
using UnityEngine.Rendering.Universal;
using System.Linq;
using Sperlich.Types;
using NesScripts.Controls.PathFind;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TankReferences))]
public class TankBase : GameEntity, IHittable, IResettable, IEditor {

	//public enum TimeMode { Update, FixedUpdate }

	#region Modifiers
	[Header("Modifiers")]
	public TankTypes tankType;
	public FloatGrade moveSpeed = new FloatGrade(5);
	public FloatGrade shootStunDuration = new FloatGrade(0.2f);
	public FloatGrade reloadDuration = new FloatGrade(0.5f);
	public FloatGrade randomReloadDuration = new FloatGrade(1f);
	public float turnSpeed = 5;
	public short bodyRotSpeed = 360;
	public float aimRotSpeed = 600;
	public bool disable2DirectionMovement;
	public bool disableTracks;
	public bool makeInvincible;     // Ivincibility for Debugging
	public bool isUsingWheels;
	//public TimeMode APITimeMode = TimeMode.FixedUpdate;
	#endregion

	#region Privates
	bool isInvincible;
	float angleDiff;
	Vector3 lastTrackPos;
	Vector3 spawnPos;
	Quaternion spawnRot;
	List<Vector3> destroyRestPoses;
	List<Quaternion> destroyRestRots;
	#endregion

	#region Protected
	protected int hashcode;
	protected float distanceSinceLastFrame;
	protected float GetTime {
		get {
			/*switch(APITimeMode) {
				case TimeMode.Update:
					return Time.deltaTime;
				case TimeMode.FixedUpdate:
					return Time.fixedDeltaTime;
				default:
					return Time.deltaTime;
			}*/
			return Time.fixedDeltaTime;
		}
	}
	protected short healthPoints;
	protected bool isShootStunned;
	protected bool canMove;
	protected bool isReloading;
	protected bool disableShooting;
	protected JSAM.Sounds trackSound = JSAM.Sounds.TankDrive;
	protected Vector3 moveDir;
	protected Vector3 lastPos;
	protected Quaternion lastHeadRot;
	protected Rigidbody rig;
	protected GroundPainter groundPainter;
	#endregion

	#region Constants
	public const short destructionVelocity = 5;
	public const float trackSpawnDistance = 0.75f;
	#endregion

	#region Properties
	public bool IsHittable { get; set; } = true;
	public bool HasBeenInitialized { get; set; }
	public bool CanShoot => isReloading == false && disableShooting == false;
	public bool HasBeenDestroyed { get; set; }
	public bool MuteTrackSound { get; set; }
	public bool IsInvincible {
		get => isInvincible || makeInvincible;
		set => isInvincible = value;
	}// Game ivincibility
	public bool IsFriendlyFireImmune => tankAsset.hasFriendlyFireShield;
	public new Vector3 Pos { get; set; }
	public Vector3 MovingDir => moveDir;
	public TankBase TempNewTank { get; private set; }
	public Int3 PlacedIndex { get; set; }
	public Int3[] OccupiedIndexes { get; set; }
	public TankTypes TankType => tankAsset.tankType;
	public TankAsset tankAsset => AssetLoader.GetTank(tankType);
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
	#endregion

	#region Shortcuts
	public Transform tankHead { get => References.tankHead; set => References.tankHead = value; }
	public Transform bulletOutput { get => References.bulletOutput; set => References.bulletOutput = value; }
	protected List<Transform> destroyTransformPieces => References.destroyTransformPieces;
	protected Transform tankBody { get => References.tankBody; set => References.tankBody = value; }
	protected Transform GuiderTransform => References.directionLeader;
	protected ParticleSystem muzzleFlash { get => References.muzzleFlash; set => References.muzzleFlash = value; }
	protected DecalProjector FakeShadow => References.fakeShadow;
	private GameObject blobShadow => References.blobShadow;
	private GameObject tankTrack => References.tankTrack;
	private ForceShield shield => References.shield;
	private Disc shockwaveDisc => References.shockwaveDisc;
	private List<Transform> Wheels => References.tankWheels;
	private ParticleSystem sparkDestroyEffect => References.sparkDestroyEffect;
	private ParticleSystem smokeDestroyEffect => References.smokeDestroyEffect;
	private ParticleSystem smokeFireDestroyEffect => References.smokeFireDestroyEffect;
	private ParticleSystem damageSmokeBody => References.damageSmokeBody;
	private ParticleSystem damageSmokeHead => References.damageSmokeHead;
	private PrefabTypes BulletType => References.bullet;
	#endregion

	#region Init
	protected virtual void Awake() {
		References = GetComponent<TankReferences>();
		rig = GetComponent<Rigidbody>();
		hashcode = gameObject.GetHashCode();
	}
	protected virtual void LateUpdate() {
		if(HasBeenInitialized) {
			distanceSinceLastFrame = Vector3.Distance(transform.position, lastPos);
			lastPos = transform.position;
		}
		Pos = transform.position;
	}
	public virtual void InitializeTank() {
		spawnPos = rig.position;
		spawnRot = rig.rotation;
		lastTrackPos = Pos;
		lastHeadRot = tankHead.rotation;
		canMove = true;
		HasBeenInitialized = true;
		Pos = transform.position;
		healthPoints = tankAsset.health;
		shockwaveDisc.gameObject.SetActive(false);
		destroyRestPoses = new List<Vector3>();
		destroyRestRots = new List<Quaternion>();
		groundPainter = FindObjectOfType<GroundPainter>();

		foreach(Transform t in destroyTransformPieces) {
			destroyRestPoses.Add(t.localPosition);
			destroyRestRots.Add(t.localRotation);
		}
		if(GraphicSettings.PerformanceMode) {
			FakeShadow.enabled = false;
		} else {
			FakeShadow.enabled = true;
		}
	}
	public void SetDifficulty(CampaignV1.Difficulty diff) {
		var type = GetType();
		foreach(var v in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)) {
			if(v.FieldType == typeof(FloatGrade)) {
				var field = (FloatGrade)v.GetValue(this);
				field.SetDifficulty(diff);
				v.SetValue(this, field);
			}
		}
	}
	#endregion

	#region Moving Parts
	protected void Move() => Move(new Vector3(transform.forward.x, 0, transform.forward.z));
	protected void Move(Vector3 moveDir) => Move(new Vector2(moveDir.x, moveDir.z));
	protected virtual void Move(Vector2 inputDir) { }
	protected void RotateTank(Vector3 moveDir) {
		if(moveDir == Vector3.zero) return;
		Quaternion rot = Quaternion.LookRotation(moveDir, Vector3.up);
		/*if(Mathf.Abs(evadeDir.x) > 0.2f && Mathf.Abs(evadeDir.z) > 0.2f) {
			rot = Quaternion.LookRotation(evadeDir, Vector3.up);
		}*/

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
			lastHeadRot = rot;
		}
	}
	/// <summary>
	/// Call this to make the wheels rotate to the corresponding movement
	/// </summary>
	protected void UpdateWheelRotation() {
		foreach(Transform t in Wheels) {
			t.Rotate(-Vector3.right, 50 * distanceSinceLastFrame);
		}
	}
	#endregion

	protected Vector3 GetLookDirection(Vector3 lookTarget) => (new Vector3(lookTarget.x, 0, lookTarget.z) - new Vector3(Pos.x, 0, Pos.z)).normalized;
	protected void TrackTracer() {
		float distToLastTrack = Vector2.Distance(new Vector2(lastTrackPos.x, lastTrackPos.z), new Vector2(rig.position.x, rig.position.z));
		if(distToLastTrack > trackSpawnDistance && disableTracks == false) {
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
			lastTrackPos = track.position;
		}
	}
	protected virtual Bullet ShootBullet() => ShootBullet(1f);
	protected virtual Bullet ShootBullet(float customPitch = 1f) {
		if(CanShoot) {
			Bullet bullet = null;
			try {
				var bounceDir = -tankHead.right * 2f;
				bounceDir.y = 0;
				
				muzzleFlash.Play();
				tankHead.DOScale(tankHead.localScale + Vector3.one / 7f, Mathf.Min(reloadDuration, 0.2f)).SetEase(new AnimationCurve(new Keyframe[] { new Keyframe(0, 0, 0.5f, 0.5f), new Keyframe(0.5f, 1, 0.5f, 0.5f), new Keyframe(1, 0, 0.5f, 0.5f) }));
				if (this is PlayerTank) {
					bullet = PrefabManager.Spawn<Bullet>(BulletType, null, bulletOutput.position).SetupBullet(gameObject, bulletOutput.forward, bulletOutput.position, true, customPitch);
				} else {
					bullet = PrefabManager.Spawn<Bullet>(BulletType, null, bulletOutput.position).SetupBullet(gameObject, bulletOutput.forward, bulletOutput.position, false, customPitch);
				}

				if (reloadDuration > 0) {
					isReloading = true;
					this.Delay(reloadDuration + Random.Range(0f, randomReloadDuration), () => isReloading = false);
				}
				if (shootStunDuration > 0 && isShootStunned == false) {
					isShootStunned = true;
					this.Delay(shootStunDuration, () => isShootStunned = false);
				}
			} catch (System.Exception e) {
				Logger.LogError(e, "Failed to set up Bullet.");
			}
			return bullet;
		}
		return null;
	}

	#region Effects
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
			}
		} else {
			ShieldEffect(effector.damageOrigin);
		}
	}
	protected virtual void GotDestroyed() {
		HasBeenDestroyed = true;
		foreach(Transform t in destroyTransformPieces) {
			var rig = t.gameObject.AddComponent<Rigidbody>();
			rig.mass = 0.01f;
			rig.drag = 0.5f;
			if(rig.TryGetComponent(out Collider c) && c.name.ToLower() != "bouncefriction") {
				rig.GetComponent<Collider>().material = AssetLoader.DefaultFriction;
			}
			t.gameObject.layer = LayerMask.NameToLayer("DestructionPieces");
			var vec = new Vector3(Random.Range(-destructionVelocity, destructionVelocity), destructionVelocity, Random.Range(-destructionVelocity, destructionVelocity));
			rig.AddForce(vec);
			rig.AddTorque(vec);
			t.SetParent(null);
		}
		blobShadow.SetActive(false);
		PlayExplosion();
	}
	private void PlayExplosion() {
		sparkDestroyEffect.Play();
		smokeDestroyEffect.Play();
		damageSmokeBody.Play();
		damageSmokeHead.Play();
		smokeFireDestroyEffect.Play();
		AudioPlayer.Play(JSAM.Sounds.TankExplode, AudioType.SoundEffect, 0.8f, 1.2f, 0.5f);

		if(CompareTag("Player")) {
			AudioPlayer.Play(JSAM.Sounds.PlayerTankExplode, AudioType.SoundEffect, 0.8f, 1.2f, 0.5f);
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
	protected void ShieldEffect(Vector3 impactPos) {
		shield.gameObject.SetActive(true);
		shield.AddHit(impactPos);
		this.Delay(1, () => shield.gameObject.SetActive(false));
	}
	private void AnimateAssembly() {
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
				AudioPlayer.Play(JSAM.Sounds.TankAssemblyClick, AudioType.SoundEffect, 1f, 1f);
				this.Delay(1f + timeDelay, () => canMove = true);
			});
		}
	}
	#endregion

	protected void AdjustOccupiedGridPos() {
		try {
			AIGrid.FreeAllHolderCells(hashcode);
			float size = 1f;
			for (float x = -size; x < size; x += 0.2f) {
				for (float y = -size; y < size; y += 0.2f) {
					Int2 current = AIGrid.Grid.GetGridPos(Pos + new Vector3(x, 0, y));
					if(AIGrid.IsPointWalkable(current)) {
						AIGrid.Grid.SetAttribute(GridExt.Attributes.Reserved, current.x, current.y, gameObject.GetHashCode());
					}
				}
			}
		} catch(System.Exception e) {
			Logger.LogError(e, "Failed to determine walkable tiles.");
        }
	}

	public virtual void ResetState() {
		Debug.Log("<color=blue>RESET: " + name + "</color>");
		TempNewTank = Instantiate(tankAsset.prefab).GetComponent<TankBase>();
		TempNewTank.transform.position = spawnPos;
		TempNewTank.transform.rotation = spawnRot;
		TempNewTank.OccupiedIndexes = OccupiedIndexes;
		TempNewTank.PlacedIndex = PlacedIndex;
		TempNewTank.transform.parent = transform.parent;
		if(this is not BossAI) {
			TempNewTank.AnimateAssembly();
		}
		if(this is PlayerTank) {
			(TempNewTank as PlayerTank).equippedAbility = (this as PlayerTank).equippedAbility;
		}

		CleanUpDestroyedPieces();
		Destroy(gameObject);
	}
	public void CleanUpDestroyedPieces() {
		foreach(Transform piece in destroyTransformPieces) {
			Destroy(piece.gameObject);
		}
	}

	#region Level-Editor
	public void RestoreMaterials() {
		try {
			LevelEditor.RestoreMaterials(tankHead.GetComponent<MeshRenderer>().materials);
			LevelEditor.RestoreMaterials(tankBody.GetComponent<MeshRenderer>().materials);
		} catch {
			Debug.LogWarning("References to materials lost. Cannot mostly be ignored.");
		}
	}
	public void SetAsPreview() {
		try {
			LevelEditor.SetMaterialsAsPreview(tankHead.GetComponent<MeshRenderer>().materials);
			LevelEditor.SetMaterialsAsPreview(tankBody.GetComponent<MeshRenderer>().materials);
		} catch {
			Debug.LogWarning("References to materials lost. Cannot mostly be ignored.");
		}
	}
	public void SetAsDestroyPreview() {
		try {
			LevelEditor.SetMaterialsAsDestroyPreview(tankHead.GetComponent<MeshRenderer>().materials);
			LevelEditor.SetMaterialsAsDestroyPreview(tankBody.GetComponent<MeshRenderer>().materials);
		} catch {
			Debug.LogWarning("References to materials lost. Cannot mostly be ignored.");
		}
	}
	#endregion
}