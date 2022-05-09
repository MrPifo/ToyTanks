using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class SnowPlower : TankAI, IHittable, IDamageEffector {

	[Header("Charge")]
	public float moveRadius = 15;
	public FloatGrade chargeSpeed;
	public FloatGrade chargeDuration;
	public FloatGrade minMoveDuration;
	public int snowEmissionRate = 500;
	public float snowChargeSoundSpeed = 0.1f;
	public ParticleSystem snowParticles;
	public ParticleSystem snowParticles2;
	private LayerMask chargePathMask = LayerMaskExtension.Create(GameMasks.Destructable, GameMasks.LevelBoundary, GameMasks.Block, GameMasks.Player, GameMasks.Bot, GameMasks.BulletTraverse);
	private LayerMask chargePredictMask = LayerMaskExtension.Create(GameMasks.Destructable, GameMasks.LevelBoundary, GameMasks.Block, GameMasks.BulletTraverse);
	private float normalMoveSpeed;
	public HitTrigger plowTrigger;

	public bool fireFromPlayer => false;

	public Vector3 damageOrigin => plowTrigger.transform.position;

	protected override void Awake() {
		base.Awake();
		AntiTankLayer = LayerMaskExtension.Create(GameMasks.Block, GameMasks.Destructable);
	}

	public override void InitializeTank() {
		base.InitializeTank();
		normalMoveSpeed = moveSpeed;
		plowTrigger.triggerLayer = chargePathMask;
		Patrol();
	}

	async UniTaskVoid Charge() {
		//if (IsPlayReady == false) return;
		float isAlignedToPlayer = 0;
		canMove = false;
		moveSpeed.Value = chargeSpeed;

		SetAiming(AimingMode.RotateWithBody);
		float maxTime = 0;
		while(isAlignedToPlayer < 0.99f && IsPlayReady && maxTime < 2) {
			RotateTank((Target.Pos - Pos).normalized);
			isAlignedToPlayer = Vector3.Dot((Target.Pos - Pos).normalized, transform.forward);
			maxTime += GetTime;
			await CheckPause();
		}
		AudioPlayer.Play(JSAM.Sounds.SnowChargeStart, AudioType.SoundEffect, 0.8f, 1.2f, 0.4f);
		await UniTask.Delay(200);
		Vector3 chargeDirection = transform.forward;
		plowTrigger.GetComponent<Collider>().enabled = true;
		MuteTrackSound = true;

		canMove = true;
		bool isCharging = true;
		plowTrigger.TriggerHit.RemoveAllListeners();
		plowTrigger.TriggerHit.AddListener(() => {
			if(plowTrigger.CurrentCollider.transform.TrySearchComponent(out IHittable hittable) && hittable.IsInvincible == false && hittable.IsFriendlyFireImmune == false) {
				hittable.TakeDamage(this);
			} else {
				isCharging = false;
			}
		});

		var snowEmission = snowParticles.emission;
		var snowEmission2 = snowParticles2.emission;
		snowEmission.rateOverTimeMultiplier = 500f;
		snowEmission2.rateOverTimeMultiplier = 100;
		snowParticles.Play();
		snowParticles2.Play();
		float soundTime = 0;
		float chargeTime = 0;
		bool timeInterrupted = false;

		// Movement is managed manually
		SetMovement(MovementType.Move);

		while(isCharging && IsPlayReady) {
			await CheckPause();
			Move(chargeDirection);
			soundTime += GetTime;
			chargeTime += GetTime;
			if(soundTime > snowChargeSoundSpeed) {
				AudioPlayer.Play(JSAM.Sounds.SnowRowl, AudioType.SoundEffect, 0.8f, 1.2f, 0.5f);
				soundTime = 0;
			}
			if(chargeTime > chargeDuration) {
				isCharging = false;
				timeInterrupted = true;
				break;
            }
		}
		snowParticles.Stop();
		snowParticles2.Stop();
		plowTrigger.GetComponent<Collider>().enabled = false;
		MuteTrackSound = false;
		moveSpeed.Value = normalMoveSpeed;

		// Bump animation
		if (timeInterrupted == false) {
			AudioPlayer.Play(JSAM.Sounds.ChargeImpact, AudioType.SoundEffect, 0.8f, 1.2f, 0.8f);
			canMove = false;
			float initY = Pos.y;
			transform.DOMoveX(transform.position.x - chargeDirection.x, 0.2f);
			transform.DOMoveZ(transform.position.z - chargeDirection.z, 0.2f);
			var seq = DOTween.Sequence();
			seq.Append(transform.DOMoveY(initY + 0.5f, 0.1f).SetEase(Ease.Linear));
			seq.Append(transform.DOMoveY(initY, 0.1f).SetEase(Ease.Linear));
			seq.Play().onComplete += () => {
				chargeDirection = Vector3.zero;
			};
		}
		Patrol().Forget();
	}

	async UniTask Patrol() {
		//if (IsPlayReady == false) return;
		float time = 0;
		float soundTime = 0;
		canMove = true;
		var snowEmission = snowParticles.emission;
		var snowEmission2 = snowParticles2.emission;
		snowEmission.rateOverTimeMultiplier = 25f;
		snowEmission2.rateOverTimeMultiplier = 25f;
		snowParticles.Play();
		snowParticles2.Play();

		SetAiming(AimingMode.AimAtPlayerOnSight);
		if(await RandomPathAsync(Pos, moveRadius, moveRadius * 0.75f)) {
			SetMovement(MovementType.MovePath);
			while (IsPlayReady && FinalDestInReach == false) {
				if(soundTime > snowChargeSoundSpeed) {
					AudioPlayer.Play(JSAM.Sounds.SnowRowl, AudioType.SoundEffect, 0.8f, 1.2f, 0.05f);
				}

				if(HasSightContactToPlayer) {
					if(time > minMoveDuration) {
						break;
					}
				}
				time += Time.deltaTime;
				soundTime += Time.deltaTime;
				await CheckPause();
			}
		}
		await CheckPause();
		snowParticles.Stop();
		snowParticles2.Stop();

		if(HasSightContact(Target.Pos, chargePredictMask, 0.5f) && RequestAttack(3f)) {
			Charge().Forget();
		} else {
			Patrol().Forget();
		}
	}
}
