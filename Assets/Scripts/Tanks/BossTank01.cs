using DG.Tweening;
using Shapes;
using SimpleMan.Extensions;
using static Sperlich.Debug.Draw.Draw;
using Sperlich.FSM;
using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;

public class BossTank01 : BossAI {

	public enum BossBehaviour { Waiting, Charge, Burst }
	public enum AttackBehaviour { None, Bursting }

	[Header("Charge")]
	public byte chargeSpeed = 8;
	public byte waitDuration = 3;
	public byte burstAmount = 4;
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
				Player.GotDestroyed();
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
		bossStates.Push(BossBehaviour.Waiting);
		HideChargeLine();
		GoToNextState(2);
	}

	// State Decision Maker
	protected override void GoToNextState(float delay = 0) {
		if(IsAIEnabled) {
			this.Delay(delay, () => {
				bossStates.Push(BossBehaviour.Waiting);
				while(bossStates == BossBehaviour.Waiting) {
					bossStates.Push(bossStates.GetRandom());
				}
				ProcessState();
				//Debug.Log($"<color=red>Status: {bossStates}</color>");
			});
		}
	}

	protected override void ProcessState() {
		switch(bossStates.State) {
			case BossBehaviour.Charge:
				StartCoroutine(ICharge());
				break;
			case BossBehaviour.Burst:
				StartCoroutine(IBurst());
				break;
		}
	}

	// State Execution

	IEnumerator ICharge() {
		CalculateCharge();
		moveSpeed = chargeSpeed;
		float dotProd = 0;
		canMove = false;

		Debug.Log("<color=red>Charge</color>");
		while(dotProd < 0.98f) {
			Move(chargeDirection);
			Vector3 dirFromAtoB = (Player.Pos - Pos).normalized;
			dotProd = Vector3.Dot(dirFromAtoB, transform.forward);
			yield return null;
		}
		canMove = true;
		yield return new WaitForSeconds(0.25f);
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
		rollerTrigger.TriggerHit.AddListener(action);

		while(bossStates == BossBehaviour.Charge) {
			DisplayChargeLine();
			chargeVibration.PlayFeedbacks();
			chargeSmoke.Play();
			Move(chargeDirection);
			yield return null;
			while(LevelManager.GamePaused) yield return null;   // Pause AI
		}
		canMove = false;
		moveSpeed = normalMoveSpeed;
		rollerTrigger.TriggerHit.RemoveListener(action);
		GoToNextState(waitDuration);
	}

	IEnumerator IBurst() {
		int shot = 0;
		this.RepeatUntil(() => bossStates == BossBehaviour.Burst, () => {
			MoveHead(Player.Pos + Player.MovingDir * 1.5f);
		}, null);
		while(shot < burstAmount) {
			ShootBullet();
			shot++;
			yield return new WaitForSeconds(reloadDuration);
			while(LevelManager.GamePaused) yield return null;   // Pause AI
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
		Physics.Raycast(Pos, chargeDirection, out chargeHit, Mathf.Infinity, obstacleLayers);
		chargeLineL.gameObject.SetActive(true);
		chargeLineR.gameObject.SetActive(true);
		chargeLineM.gameObject.SetActive(true);
		Debug.Log(chargeHit.point);
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
		Physics.Raycast(Pos, chargeDirection, out chargeHit, Mathf.Infinity, obstacleLayers);
		chargeDirection = (chargeHit.point - Pos).normalized;
	}

	public override void GotHitByBullet() {
		base.GotHitByBullet();
		healthBar.gameObject.SetActive(false);
		LevelManager.UI.SetBossBar(healthPoints, 0.25f);
	}

	public override void DrawDebug() {
		if(showDebug) {
			Text(Pos + Vector3.up, bossStates.State.ToString());
		}
	}

	public override void EnableAI() {
		base.EnableAI();
	}

	public override void DisableAI() {
		base.DisableAI();
		StopAllCoroutines();
	}

	void Update() {
		if(IsAIEnabled) {
			rollerTransform.Rotate(-Vector3.forward, rollerRotSpeed * distanceSinceLastFrame);
		}
	}
}