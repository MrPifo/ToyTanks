using DG.Tweening;
using Sperlich.Debug.Draw;
using System.Collections;
using UnityEngine;

public class SnowPlower : TankAI, IHittable, IDamageEffector {

	[Header("Charge")]
	public byte chargeSpeed = 16;
	public byte minMoveDuration = 3;
	public int snowEmissionRate = 500;
	public float snowChargeSoundSpeed = 0.1f;
	public ParticleSystem snowParticles;
	public ParticleSystem snowParticles2;
	private LayerMask chargePathMask = LayerMaskExtension.Create(GameMasks.Destructable, GameMasks.LevelBoundary, GameMasks.Block, GameMasks.Player, GameMasks.Bot, GameMasks.BulletTraverse);
	private byte normalMoveSpeed;
	public HitTrigger plowTrigger;

	public bool fireFromPlayer => false;

	public Vector3 damageOrigin => plowTrigger.transform.position;

	protected override void Awake() {
		base.Awake();
		AvoidcanceLayers = LayerMaskExtension.Create(GameMasks.Block, GameMasks.Destructable);
	}

	public override void InitializeTank() {
		base.InitializeTank();
		normalMoveSpeed = moveSpeed;
		plowTrigger.triggerLayer = chargePathMask;
		ProcessState(TankState.Move);
	}

	protected override IEnumerator ICharge() {
		float isAlignedToPlayer = 0;
		canMove = false;
		moveSpeed = chargeSpeed;

		HeadMode.Push(TankHeadMode.RotateWithBody);
		float maxTime = 0;
		while(isAlignedToPlayer < 0.99f && IsPlayReady && maxTime < 2) {
			RotateTank((Player.Pos - Pos).normalized);
			isAlignedToPlayer = Vector3.Dot((Player.Pos - Pos).normalized, transform.forward);
			yield return IPauseTank();
			maxTime += GetTime;
		}
		AudioPlayer.Play("SnowChargeStart", AudioType.SoundEffect, 0.8f, 1.2f, 0.4f);
		yield return new WaitForSeconds(0.2f);
		Vector3 chargeDirection = transform.forward;
		plowTrigger.GetComponent<Collider>().enabled = true;
		MuteTrackSound = true;
		disableAvoidanceSystem = true;

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
		float time = 0;

		// Movement is managed manually
		MoveMode.Push(MovementType.Move);
		disableDirectionLeader = true;

		while(isCharging && IsPlayReady) {
			yield return IPauseTank();
			Move(chargeDirection);
			time += GetTime;
			if(time > snowChargeSoundSpeed) {
				AudioPlayer.Play("SnowRowl", AudioType.SoundEffect, 0.8f, 1.2f, 0.5f);
				time = 0;
			}
		}
		AudioPlayer.Play("ChargeImpact", AudioType.SoundEffect, 0.8f, 1.2f, 0.8f);
		snowParticles.Stop();
		snowParticles2.Stop();
		plowTrigger.GetComponent<Collider>().enabled = false;
		MuteTrackSound = false;
		disableAvoidanceSystem = false;
		disableDirectionLeader = false;

		canMove = false;
		moveSpeed = normalMoveSpeed;

		// Bump animation
		float initY = Pos.y;
		transform.DOMoveX(transform.position.x - chargeDirection.x, 0.2f);
		transform.DOMoveZ(transform.position.z - chargeDirection.z, 0.2f);
		var seq = DOTween.Sequence();
		seq.Append(transform.DOMoveY(initY + 0.5f, 0.1f).SetEase(Ease.Linear));
		seq.Append(transform.DOMoveY(initY, 0.1f).SetEase(Ease.Linear));
		seq.Play().onComplete += () => {
			chargeDirection = Vector3.zero;
		};
		GoToNextState(TankState.Move, 1f);
	}

	protected override IEnumerator IMove() {
		float time = 0;
		float soundTime = 0;
		canMove = true;
		var snowEmission = snowParticles.emission;
		var snowEmission2 = snowParticles2.emission;
		snowEmission.rateOverTimeMultiplier = 25f;
		snowEmission2.rateOverTimeMultiplier = 25f;
		snowParticles.Play();
		snowParticles2.Play();

		HeadMode.Push(TankHeadMode.AimAtPlayerOnSight);
		MoveMode.Push(MovementType.MoveSmart);
		if(RandomPath(Pos, playerDetectRadius, playerDetectRadius * 0.75f)) {
			while(IsPlayReady) {
				if(soundTime > snowChargeSoundSpeed) {
					AudioPlayer.Play("SnowRowl", AudioType.SoundEffect, 0.8f, 1.2f, 0.05f);
				}

				if(HasSightContactToPlayer) {
					if(time > minMoveDuration) {
						break;
					}
				}
				if(HasReachedDestination) {
					break;
				}
				yield return IPauseTank();
				time += Time.deltaTime;
				soundTime += Time.deltaTime;
			}
		}
		yield return null;
		snowParticles.Stop();
		snowParticles2.Stop();

		if(HasSightContactToPlayer) {
			ProcessState(TankState.Charge);
		} else {
			ProcessState(TankState.Move);
		}
	}

	Vector3 CalculateChargeDirection() {
		Vector3 chargeDirection = (Player.Pos - Pos).normalized;
		chargeDirection.y = 0;
		Physics.Raycast(Pos, chargeDirection, out RaycastHit chargeHit, Mathf.Infinity, chargePathMask);
		chargeDirection = (chargeHit.point - Pos).normalized;
		return chargeDirection;
	}

	protected override void DrawDebug() {
		if(showDebug) {
			base.DrawDebug();
			Draw.Cube(currentDestination, Color.yellow);
			Draw.Cube(nextMoveTarget, Color.green);
		}
	}
}
