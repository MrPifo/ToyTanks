using DG.Tweening;
using Shapes;
using SimpleMan.Extensions;
using static Sperlich.Debug.Draw.Draw;
using Sperlich.FSM;
using System.Collections;
using UnityEngine;

public class BossTank01 : TankAI {

	[Header("Charge")]
	public int chargeSpeed = 8;
	public float waitDuration = 3;
	public Line chargeLineL;
	public Line chargeLineR;
	public Line chargeLineM;
	[Header("Burst")]
	public int burstAmount = 4;
	[Header("Info")]
	public bool isWaiting;
	public HitTrigger rollerTrigger;
	public enum BossBehaviour { Waiting, Charge, Burst}
	public enum AttackBehaviour { None, Bursting }
	public FSM<BossBehaviour> bossStates = new FSM<BossBehaviour>();
	public FSM<AttackBehaviour> attackBehaviours = new FSM<AttackBehaviour>();
	public Vector3 chargeDirection;
	public Vector3 chargeDestination;
	RaycastHit chargeHit;
	int normalMoveSpeed;
	bool renewChargeAfterTurn;
	bool disableChargeMovement;

	protected override void Awake() {
		base.Awake();
		rollerTrigger.PlayerHit.AddListener(() => {
			if(!player.makeInvincible) {
				player.GotDestroyed();
			}
		});
		normalMoveSpeed = moveSpeed;
		renewChargeAfterTurn = false;
		disableChargeMovement = false;
		chargeDirection = Vector3.zero;
		HideChargeLine();
	}

	public override void Attack() {
		
	}

	public override void Defense() {
		
	}

	public override void Idle() {
		stateMachine.Push(TankState.BossStates);
		bossStates.Push(BossBehaviour.Waiting);
	}

	public override void Patrol() {
		
	}

	public void Waiting() {
		if(!isWaiting) {
			isWaiting = true;
			this.Delay(waitDuration, () => {
				isWaiting = false;
				DecideNextMove();
			});
		}
	}

	public void DecideNextMove() {
		bossStates.Push(bossStates.GetRandom());
		if(bossStates.IsState(BossBehaviour.Waiting)) {
			DecideNextMove();
		}
		//bossStates.Push(BossBehaviour.Charge);
	}

	public void Charge() {
		if(chargeDirection == Vector3.zero) {
			CalculateCharge();
			moveSpeed = chargeSpeed;
			this.Delay(1f, () => rollerTrigger.TriggerHit.AddListener(ChargeHit));
		} else {
			if(IsFacing(chargeHit.point, 0.98f) && !renewChargeAfterTurn) {
				renewChargeAfterTurn = true;
				disableChargeMovement = true;
				this.Delay(0.25f, () => {
					disableChargeMovement = false;
					CalculateCharge();
					DisplayChargeLine();
				});
				return;
			}
			if(!disableChargeMovement) {
				if(renewChargeAfterTurn) {
					DisplayChargeLine();
				}
				Move(chargeDirection);
			}
		}
	}
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

	public void ChargeHit() {
		if(bossStates.State == BossBehaviour.Charge) {
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
				renewChargeAfterTurn = false;
				disableChargeMovement = false;
			};
			rollerTrigger.TriggerHit.RemoveListener(ChargeHit);
		}
	}

	public void Burst() {
		MoveHead(player.Pos + player.MovingDir * 1.5f);
		if(!attackBehaviours.IsState(AttackBehaviour.Bursting)) {
			StartCoroutine(IBurst());
		}
	}
	IEnumerator IBurst() {
		int shot = 0;
		attackBehaviours.Push(AttackBehaviour.Bursting);
		while(shot < burstAmount) {
			ShootBullet();
			shot++;
			yield return new WaitForSeconds(reloadDuration);
		}
		bossStates.Push(BossBehaviour.Waiting);
		attackBehaviours.Push(AttackBehaviour.None);
	}

	public override void BossStates() {
		Text(Pos + Vector3.up, bossStates.State.ToString());
		switch(bossStates.State) {
			case BossBehaviour.Waiting:
				Waiting();
				break;
			case BossBehaviour.Charge:
				Charge();
				break;
			case BossBehaviour.Burst:
				Burst();
				break;
		}
	}
}
