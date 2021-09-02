using DG.Tweening;
using Shapes;
using SimpleMan.Extensions;
using static Sperlich.Debug.Draw.Draw;
using Sperlich.FSM;
using System.Collections;
using UnityEngine;
using MoreMountains.Feedbacks;
using UnityEngine.Events;

public class BossTank01 : BossAI {

	public enum BossBehaviour { Waiting, Charge, Burst }
	public enum AttackBehaviour { None, Bursting }

	public float rollerRotSpeed = 1;
	[Header("Charge")]
	public int chargeSpeed = 8;
	public float waitDuration = 3;
	public Line chargeLineL;
	public Line chargeLineR;
	public Line chargeLineM;
	public MMFeedbacks chargeVibration;
	public MMFeedbacks hitChargeFeedback;
	public ParticleSystem chargeSmoke;
	[Header("Burst")]
	public int burstAmount = 4;
	[Header("Info")]
	public HitTrigger rollerTrigger;
	public Transform rollerTransform;
	public FSM<BossBehaviour> bossStates = new FSM<BossBehaviour>();
	public Vector3 chargeDirection;
	public Vector3 chargeDestination;
	RaycastHit chargeHit;
	int normalMoveSpeed;

	protected override void Awake() {
		base.Awake();
		rollerTrigger.PlayerHit.AddListener(() => {
			if(!player.makeInvincible) {
				player.GotDestroyed();
			}
		});
		normalMoveSpeed = moveSpeed;
		chargeDirection = Vector3.zero;
		HideChargeLine();
		GoToNextState(5);
	}

	public override void Initialize() {
		LevelManager.UI.InitBossBar(maxHealthPoints, 3);
	}

	// State Decision Maker
	public override void GoToNextState(float delay = 0) {
		Debug.Log($"<color=red>Status: Waiting</color>");
		this.Delay(delay, () => {
			bossStates.Push(BossBehaviour.Waiting);
			while(bossStates == BossBehaviour.Waiting) {
				bossStates.Push(bossStates.GetRandom());
			}
			BossStates();
			Debug.Log($"<color=red>Status: {bossStates}</color>");
		});
	}

	public void BossStates() {
		if(HasBeenDestroyed) {
			return;
		}
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
		CanMove = false;

		while(dotProd < 0.98f) {
			Move(chargeDirection);
			Vector3 dirFromAtoB = (player.Pos - Pos).normalized;
			dotProd = Vector3.Dot(dirFromAtoB, transform.forward);
			yield return null;
		}
		CanMove = true;
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
		}
		rollerTrigger.TriggerHit.RemoveListener(action);
		GoToNextState(waitDuration);
	}

	IEnumerator IBurst() {
		int shot = 0;
		this.RepeatUntil(() => bossStates == BossBehaviour.Burst, () => {
			MoveHead(player.Pos + player.MovingDir * 1.5f);
		}, null);
		while(shot < burstAmount) {
			ShootBullet();
			shot++;
			yield return new WaitForSeconds(reloadDuration);
		}
		GoToNextState(waitDuration);
	}
	// Helper
	void HideChargeLine() {
		chargeLineL.gameObject.SetActive(false);
		chargeLineR.gameObject.SetActive(false);
		chargeLineM.gameObject.SetActive(false);
	}

	void DisplayChargeLine() {
		Physics.Raycast(Pos, chargeDirection, out chargeHit, Mathf.Infinity, obstacleLayers);
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
		chargeDirection = (player.Pos - Pos).normalized;
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

	void Update() {
		rollerTransform.Rotate(-Vector3.forward, rollerRotSpeed * distanceSinceLastFrame);
		if(HasBeenDestroyed) {
			StopAllCoroutines();
		}
	}
}