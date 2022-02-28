using DG.Tweening;
using Shapes;
using SimpleMan.Extensions;
using static Sperlich.Debug.Draw.Draw;
using Sperlich.FSM;
using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;
using System.Collections.Generic;

public class BossTank01 : BossAI, IHittable, IDamageEffector {

	public enum BossBehaviour { Waiting, Charge, Burst }
	public enum AttackBehaviour { None, Bursting }

	// Boss Moves:
	// Charge: Boss rotates to player and displays a line in front of him.
	// After a short delay he begins to charge along this line until he hits the player or an obstacle.
	//
	// Burst: Boss stands still and shoots a great amount of bullets in short time at the player.
	// The bullets can reflect of walls which makes it more dangerous to dodge them.

	[Header("Charge")]
	public byte chargeSpeed = 8;
	public byte waitDuration = 3;
	public byte burstAmount = 4;
	public AudioSource chargeSound;
	private LayerMask chargePathMask = LayerMaskExtension.Create(GameMasks.Ground, GameMasks.Destructable, GameMasks.LevelBoundary, GameMasks.Block, GameMasks.BulletTraverse);
	[Header("References")]
	[SerializeField] Line chargeLineL;
	[SerializeField] Line chargeLineR;
	[SerializeField] Line chargeLineM;
	[SerializeField] Transform rollerTransform;
	[SerializeField] GameObject chargePath;
	[SerializeField] HitTrigger rollerTrigger;
	[SerializeField] MMFeedbacks chargeVibration;
	[SerializeField] MMFeedbacks hitChargeFeedback;
	[SerializeField] ParticleSystem chargeSmoke;

	new FSM<BossBehaviour> TankState = new FSM<BossBehaviour>();
	Vector3 chargeDirection;
	RaycastHit chargeHit;
	byte rollerRotSpeed = 100;
	byte normalMoveSpeed;
	public bool fireFromPlayer => false;
	public Vector3 damageOrigin => Pos;

	protected override void Awake() {
		base.Awake();
		chargePath.SetActive(false);
		trackSpawnDistance = 0.35f;
		rollerTrigger.PlayerHit.AddListener(() => {
			if(!Player.IsInvincible) {
				Player.TakeDamage(this, true);
			}
		});

		normalMoveSpeed = moveSpeed;
		chargeDirection = Vector3.zero;
		HideChargeLine();
	}

	public override void InitializeTank() {
		base.InitializeTank();
		chargeDirection = Vector3.zero;
		moveSpeed = normalMoveSpeed;
		rollerTrigger.TriggerHit.RemoveAllListeners();
		TankState.FillWeightedStates(new List<(BossBehaviour, float)>() {
			(BossBehaviour.Charge, 25f),
			(BossBehaviour.Burst, 50f),
		});
		HideChargeLine();
		GoToNextState(TankState.GetWeightedRandom(), 1.5f);
		healthBar.gameObject.SetActive(false);
	}

	// State Decision Maker
	protected void GoToNextState(BossBehaviour state, float delay = 0) {
		TankState.Push(state);
		this.Delay(delay, () => ProcessState());
	}

	private void CalculateMove(float delay = 0) {
		if(IsAIEnabled) {
			if(distToPlayer > 25) {
				TankState.AddWeight(BossBehaviour.Charge, 30f);
			} else {
				TankState.AddWeight(BossBehaviour.Charge, 20f);
			}
			TankState.AddWeight(BossBehaviour.Burst, 10f);
			TankState.PushRandomWeighted();
			this.Delay(delay == 0 ? Time.deltaTime : delay, () => ProcessState());
		}
	}

	protected void ProcessState() {
		if(IsPlayReady) {
			switch(TankState.State) {
				case BossBehaviour.Charge:
					StartCoroutine(ICharge());
					break;
				case BossBehaviour.Burst:
					StartCoroutine(IBurst());
					break;
			}
		}
	}

	// State Execution

	protected override IEnumerator ICharge() {
		CalculateCharge();
		float isAlignedToPlayer = 0;
		canMove = false;
		moveSpeed = chargeSpeed;

		HeadMode.Push(TankHeadMode.KeepRotation);
		float maxTime = 0;
		while(isAlignedToPlayer < 0.98f && IsPlayReady && maxTime < 1f) {
			RotateTank(chargeDirection);
			isAlignedToPlayer = Vector3.Dot((Player.Pos - Pos).normalized, transform.forward);
			yield return IPauseTank();
			maxTime += GetTime;
		}
		yield return new WaitForSeconds(0.25f);

		canMove = true;
		rollerTrigger.TriggerHit.AddListener(action);
		AudioPlayer.Play("SnowChargeStart", AudioType.SoundEffect, 0.8f, 1.2f, 0.6f);
		chargeSound.Play();
		disableAvoidanceSystem = true;
		if(CurrentPhase == BossPhases.Half_Life) {
			HeadMode.Push(TankHeadMode.AimAtPlayer);
		} else {
			HeadMode.Push(TankHeadMode.KeepRotation);
		}
		MoveMode.Push(MovementType.None);
		chargeSound.volume = 0.3f * GraphicSettings.SoundEffectsVolume;
		chargeSound.pitch = 2f;
		bool allowPitchDown = false;
		MuteTrackSound = true;
		DOTween.To(() => chargeSound.pitch, x => chargeSound.pitch = x, 1f, 0.6f).OnComplete(() => allowPitchDown = true);

		while(TankState == BossBehaviour.Charge && IsPlayReady) {
			yield return IPauseTank();
			DisplayChargeLine();
			chargeVibration.PlayFeedbacks();
			chargeSmoke.Play();
			Move(chargeDirection);
			if(CurrentPhase == BossPhases.Half_Life && HasSightContactToPlayer && RandomShootChance()) {
				ShootBullet();
			}
			GameCamera.ShortShake2D(0.02f, 25, 25);
			if(allowPitchDown && chargeSound.pitch > 0.5f) {
				chargeSound.pitch -= Time.deltaTime / 7f;
			}
		}
		

		canMove = false;
		moveSpeed = normalMoveSpeed;
		disableAvoidanceSystem = false;
		rollerTrigger.TriggerHit.RemoveListener(action);
		AudioPlayer.Play("ChargeImpact", AudioType.SoundEffect, 1f, 1f);
		HideChargeLine();
		CalculateMove(2f);
		DOTween.To(() => chargeSound.volume, x => chargeSound.volume = x, 0, 0.3f).SetEase(Ease.OutBounce).OnComplete(() => {
			chargeSound.Stop();
		});

		void action() {
			HideChargeLine();
			moveSpeed = normalMoveSpeed;
			TankState.Push(BossBehaviour.Waiting);
			float initY = Pos.y;
			transform.DOMoveX(transform.position.x + -chargeDirection.x, 0.35f);
			transform.DOMoveZ(transform.position.z + -chargeDirection.z, 0.35f);
			var seq = DOTween.Sequence();
			seq.Append(transform.DOMoveY(initY + 1f, 0.2f).SetEase(Ease.InCubic));
			seq.Append(transform.DOMoveY(initY, 0.25f).SetEase(Ease.OutBounce));
			seq.Play().onComplete += () => {
				chargeDirection = Vector3.zero;
			};
			hitChargeFeedback.PlayFeedbacks();
		}
	}

	IEnumerator IBurst() {
		int shot = 0;
		this.RepeatUntil(() => TankState == BossBehaviour.Burst && IsPlayReady, () => {
			MoveHead(Player.Pos + Player.MovingDir * 1.5f);
		}, null);
		byte originalMoveSpeed = moveSpeed;
		byte newBurstAmount = burstAmount;
		if(CurrentPhase == BossPhases.Half_Life) {
			MoveMode.Push(MovementType.MoveSmart);
			RandomPath(Pos, playerDetectRadius, 6f, false);
			moveSpeed /= 3;
			canMove = true;
			newBurstAmount = 4;
		} 

		while(shot < newBurstAmount && IsPlayReady) {
			ShootBullet();
			shot++;
			if(CurrentPhase == BossPhases.Half_Life) {
				yield return new WaitForSeconds(0.5f);
			} else {
				yield return new WaitForSeconds(reloadDuration);
			}
			yield return IPauseTank();
		}
		moveSpeed = originalMoveSpeed;
		rig.velocity = Vector3.zero;
		CalculateMove(1f);
	}
	// Helper
	void HideChargeLine() {
		chargeLineL.gameObject.SetActive(false);
		chargeLineR.gameObject.SetActive(false);
		chargeLineM.gameObject.SetActive(false);
		chargePath.transform.SetParent(transform);
		chargePath.SetActive(false);
	}

	void DisplayChargeLine() {
		chargePath.SetActive(true);
		chargePath.transform.SetParent(null);
		Physics.Raycast(Pos, transform.forward, out chargeHit, Mathf.Infinity, chargePathMask);
		chargeLineL.gameObject.SetActive(true);
		chargeLineR.gameObject.SetActive(true);
		chargeLineM.gameObject.SetActive(true);
		chargeLineL.transform.localPosition = Vector3.zero;
		chargeLineL.Start = chargeLineL.transform.InverseTransformPoint(Pos);
		chargeLineL.End = chargeLineL.transform.InverseTransformPoint(chargeHit.point + transform.forward);
		chargeLineL.transform.localPosition = new Vector3(-0.8f, 0, 0);

		chargeLineR.transform.localPosition = Vector3.zero;
		chargeLineR.Start = chargeLineR.transform.InverseTransformPoint(Pos);
		chargeLineR.End = chargeLineR.transform.InverseTransformPoint(chargeHit.point + transform.forward);
		chargeLineR.transform.localPosition = new Vector3(0.8f, 0, 0);

		chargeLineM.Start = chargeLineM.transform.InverseTransformPoint(Pos);
		chargeLineM.End = chargeLineM.transform.InverseTransformPoint(chargeHit.point + transform.forward);
	}

	void CalculateCharge() {
		chargeDirection = (Player.Pos - Pos).normalized;
		chargeDirection.y = 0;
		Physics.Raycast(Pos, chargeDirection, out chargeHit, Mathf.Infinity, chargePathMask);
		chargeDirection = (chargeHit.point - Pos).normalized;
	}

	public override void TakeDamage(IDamageEffector effector, bool instantKill = false) {
		base.TakeDamage(effector);
		BossUI.BossTakeDamage(this, 1);

		if(healthPoints <= MaxHealthPoints / 2) {
			CurrentPhase = BossPhases.Half_Life;
		}
	}

	protected override void GotDestroyed() {
		base.GotDestroyed();
		BossUI.RemoveBoss(this);
	}

	protected override void DrawDebug() {
		base.DrawDebug();
		if(showDebug) {
			Text(Pos + Vector3.up, TankState.State.ToString());
		}
	}

	protected override void Update() {
		base.Update();
		if(IsAIEnabled) {
			rollerTransform.Rotate(-Vector3.right, rollerRotSpeed * distanceSinceLastFrame);
		}
	}
}