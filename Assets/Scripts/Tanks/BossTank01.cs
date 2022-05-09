using DG.Tweening;
using Shapes;
using Sperlich.FSM;
using UnityEngine;
using MoreMountains.Feedbacks;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using static Sperlich.Debug.Draw.Draw;

public class BossTank01 : BossAI, IHittable, IDamageEffector {

	public enum BossBehaviour { Waiting, Charge, Burst }
	public enum AttackBehaviour { None, Bursting }

	// Boss Moves:
	// Charge: Boss rotates to player and displays a line in front of him.
	// After a short delay he begins to charge along this line until he hits the player or an obstacle.
	//
	// Burst: Boss stands still and shoots a great amount of bullets in short time at the player.
	// The bullets can reflect of walls which makes it more dangerous to dodge them.

	[Header("Boss 1")]
	public FloatGrade chargeSpeed;
	public FloatGrade burstAmount;
	public AudioSource chargeSound;
	private LayerMask chargePathMask = LayerMaskExtension.Create(GameMasks.Destructable, GameMasks.LevelBoundary, GameMasks.Block, GameMasks.BulletTraverse);
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
	float normalMoveSpeed;
	public bool fireFromPlayer => false;
	public Vector3 damageOrigin => Pos;

	protected override void Awake() {
		base.Awake();
		chargePath.SetActive(false);
		rollerTrigger.PlayerHit.AddListener(() => {
			if(!Target.IsInvincible) {
				Target.TakeDamage(this, true);
			}
		});

		normalMoveSpeed = moveSpeed;
		chargeDirection = Vector3.zero;
		HideChargeLine();
	}

	public override void InitializeTank() {
		base.InitializeTank();
		chargeDirection = Vector3.zero;
		moveSpeed.Value = normalMoveSpeed;
		rollerTrigger.TriggerHit.RemoveAllListeners();
		TankState.FillWeightedStates(new List<(BossBehaviour, float)>() {
			(BossBehaviour.Charge, 25f),
			(BossBehaviour.Burst, 50f),
		});
		HideChargeLine();
		GoToNextState(TankState.GetWeightedRandom(), 1500).Forget();
	}

	// State Decision Maker
	async UniTaskVoid GoToNextState(BossBehaviour state, int delay = 0) {
		TankState.Push(state);
		await UniTask.Delay(delay);
		ProcessState().Forget();
	}

	async UniTask CalculateMove(int delay = 0) {
		if (IsPlayReady == false) return;
		await CheckPause();
		if(distToPlayer > 25) {
			TankState.AddWeight(BossBehaviour.Charge, 30f);
		} else {
			TankState.AddWeight(BossBehaviour.Charge, 20f);
		}
		TankState.AddWeight(BossBehaviour.Burst, 10f);
		TankState.PushRandomWeighted();
		await UniTask.Delay(delay);
		if(HasSightContactToPlayer == false) {
			GoToNextState(BossBehaviour.Burst).Forget();
			return;
		}
		ProcessState().Forget();
	}

	async UniTaskVoid ProcessState() {
		if (IsPlayReady == false) return;
		await CheckPause();
		switch (TankState.State) {
			case BossBehaviour.Charge:
				Charge().Forget();
				break;
			case BossBehaviour.Burst:
				Burst().Forget();
				break;
		}
	}

	// State Execution
	async UniTaskVoid Charge() {
		if (IsPlayReady == false) return;
		CalculateCharge();
		float isAlignedToPlayer = 0;
		canMove = false;
		moveSpeed.Value = chargeSpeed;

		SetAiming(AimingMode.KeepRotation);
		float maxTime = 0;
		while(isAlignedToPlayer < 0.98f && IsPlayReady && maxTime < 1f) {
			RotateTank(chargeDirection);
			isAlignedToPlayer = Vector3.Dot((Target.Pos - Pos).normalized, transform.forward);
			maxTime += GetTime;
			await CheckPause();
		}
		await UniTask.Delay(250);

		canMove = true;
		rollerTrigger.TriggerHit.AddListener(action);
		AudioPlayer.Play(JSAM.Sounds.SnowChargeStart, AudioType.SoundEffect, 0.8f, 1.2f, 0.6f);
		chargeSound.Play();
		if(CurrentPhase == BossPhases.Half_Life) {
			SetAiming(AimingMode.AimAtPlayer);
		} else {
			SetAiming(AimingMode.KeepRotation);
		}
		SetMovement(MovementType.Move);
		chargeSound.volume = 0.3f * GraphicSettings.SoundEffectsVolume;
		chargeSound.pitch = 2f;
		bool allowPitchDown = false;
		MuteTrackSound = true;
		DOTween.To(() => chargeSound.pitch, x => chargeSound.pitch = x, 1f, 0.6f).OnComplete(() => allowPitchDown = true);

		while(TankState == BossBehaviour.Charge && IsPlayReady) {
			await CheckPause();
			DisplayChargeLine();
			chargeVibration.PlayFeedbacks();
			chargeSmoke.Play();
			if(CurrentPhase == BossPhases.Half_Life && HasSightContactToPlayer && RandomShootChance()) {
				ShootBullet();
			}
			GameCamera.ShortShake2D(0.02f, 25, 25);
			if(allowPitchDown && chargeSound.pitch > 0.5f) {
				chargeSound.pitch -= Time.deltaTime / 7f;
			}
		}
		

		canMove = false;
		moveSpeed.Value = normalMoveSpeed;
		rollerTrigger.TriggerHit.RemoveListener(action);
		AudioPlayer.Play(JSAM.Sounds.ChargeImpact, AudioType.SoundEffect, 1f, 1f);
		HideChargeLine();
		DOTween.To(() => chargeSound.volume, x => chargeSound.volume = x, 0, 0.3f).SetEase(Ease.OutBounce).OnComplete(() => {
			chargeSound.Stop();
		});
		await CalculateMove(2000);

		void action() {
			HideChargeLine();
			moveSpeed.Value = normalMoveSpeed;
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

	async UniTaskVoid Burst() {
		if (IsPlayReady == false) return;
		int shot = 0;
		float originalMoveSpeed = moveSpeed;
		float newBurstAmount = burstAmount;
		SetAiming(AimingMode.AimAtPlayer);

		if(CurrentPhase == BossPhases.Half_Life) {
			SetMovement(MovementType.MoveSmart);
			await RandomPathAsync(Pos, 15, 6f, false);
			moveSpeed.Value /= 3;
			canMove = true;
			newBurstAmount += 1;
		} 

		while(shot < newBurstAmount && IsPlayReady) {
			ShootBullet();
			shot++;
			await UniTask.WaitUntil(() => CanShoot);
			await CheckPause();
		}
		moveSpeed.Value = originalMoveSpeed;
		rig.velocity = Vector3.zero;
		await CalculateMove(1000);
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
		Physics.SphereCast(Pos, 1f, transform.forward, out chargeHit, Mathf.Infinity, chargePathMask);
		chargeLineL.gameObject.SetActive(true);
		chargeLineR.gameObject.SetActive(true);
		chargeLineM.gameObject.SetActive(true);
		chargeLineL.transform.localPosition = Vector3.zero;
		chargeLineL.Start = chargeLineL.transform.InverseTransformPoint(Pos);
		chargeLineL.End = chargeLineL.transform.InverseTransformPoint(chargeHit.point);
		chargeLineL.transform.localPosition = new Vector3(-0.8f, 0, 0);

		chargeLineR.transform.localPosition = Vector3.zero;
		chargeLineR.Start = chargeLineR.transform.InverseTransformPoint(Pos);
		chargeLineR.End = chargeLineR.transform.InverseTransformPoint(chargeHit.point);
		chargeLineR.transform.localPosition = new Vector3(0.8f, 0, 0);

		chargeLineM.Start = chargeLineM.transform.InverseTransformPoint(Pos);
		chargeLineM.End = chargeLineM.transform.InverseTransformPoint(chargeHit.point);
	}

	void CalculateCharge() {
		chargeDirection = (Target.Pos - Pos).normalized;
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
		if(debugMode) {
			Text(Pos + Vector3.up, TankState.State.ToString());
		}
	}

	protected override void Update() {
		base.Update();
		if(IsAIEnabled) {
			rollerTransform.Rotate(Vector3.right, rollerRotSpeed * distanceSinceLastFrame);
		}
	}
}