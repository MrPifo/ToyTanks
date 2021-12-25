using DG.Tweening;
using Shapes;
using SimpleMan.Extensions;
using static Sperlich.Debug.Draw.Draw;
using Sperlich.FSM;
using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;

public class BossTank01 : BossAI, IHittable {

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
	private LayerMask chargePathMask = LayerMaskExtension.Create(GameMasks.Ground, GameMasks.Destructable, GameMasks.LevelBoundary, GameMasks.Block);
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

	FSM<BossBehaviour> bossStates = new FSM<BossBehaviour>();
	Vector3 chargeDirection;
	RaycastHit chargeHit;
	byte rollerRotSpeed = 100;
	byte normalMoveSpeed;

	protected override void Awake() {
		base.Awake();
		chargePath.SetActive(false);
		trackSpawnDistance = 0.35f;
		rollerTrigger.PlayerHit.AddListener(() => {
			if(!Player.IsInvincible) {
				Player.Kill();
			}
		});

		normalMoveSpeed = moveSpeed;
		chargeDirection = Vector3.zero;
		HideChargeLine();
	}

	public override void InitializeTank() {
		base.InitializeTank();
		BossUI.RegisterBoss(this);
		chargeDirection = Vector3.zero;
		moveSpeed = normalMoveSpeed;
		rollerTrigger.TriggerHit.RemoveAllListeners();
		bossStates.Push(BossBehaviour.Waiting);
		HideChargeLine();
		GoToNextState(2);
		healthBar.gameObject.SetActive(false);
	}

	// State Decision Maker
	protected override void GoToNextState(float delay = 0) {
		if(IsPlayReady) {
			this.Delay(delay, () => {
				bossStates.Push(BossBehaviour.Waiting);
				while(bossStates == BossBehaviour.Waiting) {
					bossStates.Push(bossStates.GetRandom());
				}
				ProcessState();
			});
		}
	}

	protected void ProcessState() {
		if(IsPlayReady) {
			switch(bossStates.State) {
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

		while(isAlignedToPlayer < 0.98f && IsPlayReady) {
			RotateTank(chargeDirection);
			KeepHeadRot();
			isAlignedToPlayer = Vector3.Dot((Player.Pos - Pos).normalized, transform.forward);
			yield return IPauseTank();
		}
		yield return new WaitForSeconds(0.25f);

		canMove = true;
		rollerTrigger.TriggerHit.AddListener(action);
		disableAvoidanceSystem = true;

		while(bossStates == BossBehaviour.Charge && IsPlayReady) {
			yield return IPauseTank();
			DisplayChargeLine();
			chargeVibration.PlayFeedbacks();
			chargeSmoke.Play();
			Move(chargeDirection);
			GameCamera.ShortShake2D(0.02f, 25, 25);
		}

		canMove = false;
		moveSpeed = normalMoveSpeed;
		disableAvoidanceSystem = false;
		rollerTrigger.TriggerHit.RemoveListener(action);
		HideChargeLine();
		GoToNextState(waitDuration);

		void action() {
			HideChargeLine();
			moveSpeed = normalMoveSpeed;
			bossStates.Push(BossBehaviour.Waiting);
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
		this.RepeatUntil(() => bossStates == BossBehaviour.Burst && IsPlayReady, () => {
			MoveHead(Player.Pos + Player.MovingDir * 1.5f);
		}, null);
		while(shot < burstAmount && IsPlayReady) {
			ShootBullet();
			shot++;
			yield return new WaitForSeconds(reloadDuration);
			yield return IPauseTank();
		}
		GoToNextState(waitDuration);
	}
	// Helper
	void HideChargeLine() {
		chargeLineL.gameObject.SetActive(false);
		chargeLineR.gameObject.SetActive(false);
		chargeLineM.gameObject.SetActive(false);
		chargePath.SetActive(false);
	}

	void DisplayChargeLine() {
		chargePath.SetActive(true);
		Physics.Raycast(Pos, chargeDirection, out chargeHit, Mathf.Infinity, chargePathMask);
		chargeLineL.gameObject.SetActive(true);
		chargeLineR.gameObject.SetActive(true);
		chargeLineM.gameObject.SetActive(true);
		chargeLineL.transform.localPosition = Vector3.zero;
		chargeLineL.Start = chargeLineL.transform.InverseTransformPoint(Pos);
		chargeLineL.End = chargeLineL.transform.InverseTransformPoint(chargeHit.point + chargeDirection);
		chargeLineL.transform.localPosition = new Vector3(-0.8f, 0, 0);

		chargeLineR.transform.localPosition = Vector3.zero;
		chargeLineR.Start = chargeLineR.transform.InverseTransformPoint(Pos);
		chargeLineR.End = chargeLineR.transform.InverseTransformPoint(chargeHit.point + chargeDirection);
		chargeLineR.transform.localPosition = new Vector3(0.8f, 0, 0);

		chargeLineM.Start = chargeLineM.transform.InverseTransformPoint(Pos);
		chargeLineM.End = chargeLineM.transform.InverseTransformPoint(chargeHit.point + chargeDirection);
	}

	void CalculateCharge() {
		chargeDirection = (Player.Pos - Pos).normalized;
		chargeDirection.y = 0;
		Physics.Raycast(Pos, chargeDirection, out chargeHit, Mathf.Infinity, chargePathMask);
		chargeDirection = (chargeHit.point - Pos).normalized;
	}

	public new void TakeDamage(IDamageEffector effector) {
		base.TakeDamage(effector);
		BossUI.BossTakeDamage(this, 1);
	}

	protected override void GotDestroyed() {
		base.GotDestroyed();
		BossUI.RemoveBoss(this);
	}

	protected override void DrawDebug() {
		base.DrawDebug();
		if(showDebug) {
			Text(Pos + Vector3.up, bossStates.State.ToString());
		}
	}

	protected override void Update() {
		base.Update();
		if(IsAIEnabled) {
			rollerTransform.Rotate(-Vector3.right, rollerRotSpeed * distanceSinceLastFrame);
		}
	}
}