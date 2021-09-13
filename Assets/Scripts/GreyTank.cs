using SimpleMan.Extensions;
using Sperlich.Debug.Draw;
using Sperlich.FSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreyTank : TankAI {

	public enum GreyState { Waiting, Attack, Retreat }
	protected FSM<GreyState> stateMachine = new FSM<GreyState>();

	byte pathUpdateInvervall = 1;
	byte attackPathRefreshIntervall = 4;
	float behaviourAttackTime;

	protected override void Awake() {
		base.Awake();
	}

	protected override void GoToNextState(float delay = 0.0001f) {
		if(IsAIEnabled) {
			this.Delay(delay, () => {
				stateMachine.Push(GreyState.Waiting);
				while(stateMachine == GreyState.Waiting) {
					stateMachine.Push(stateMachine.GetRandom());
				}
				ProcessState();
			});
		}
	}

	public override void InitializeAI() {
		stateMachine.Push(GreyState.Attack);
		ProcessState();
	}

	protected override void ProcessState() {
		if(HasBeenDestroyed == false) {
			switch(stateMachine.State) {
				case GreyState.Attack:
					StartCoroutine(Attack());
					break;
				case GreyState.Retreat:
					StartCoroutine(Retreat());
					break;
			}
		}
	}

	IEnumerator Attack() {
		int bulletsShot = 0;
		int requiredShots = Random(1, 4);
		while(bulletsShot < requiredShots && !HasBeenDestroyed) {
			if(CanShoot && HasSightContactToPlayer && IsAimingAtPlayer) {
				ShootBullet();
				bulletsShot++;
			} else {
				if(HasSightContactToPlayer && IsPlayerInDetectRadius) {
					Aim();
				} else {
					FetchPathToPlayer();
					MoveAlongPath();
					KeepHeadRot();
				}
			}
			yield return null;
		}
		stateMachine.Push(GreyState.Retreat);
		ProcessState();
	}

	IEnumerator Retreat() {
		float time = 0;
		while(time < Random(3, 6) && !HasBeenDestroyed) {
			RefreshRandomPath(Pos, 20, 4);
			MoveAlongPath();
			KeepHeadRot();
			yield return null;
			time += Time.deltaTime;
		}
		stateMachine.Push(GreyState.Attack);
		ProcessState();
	}

	public override void DrawDebug() {
		Draw.Text(Pos + Vector3.up * 2, stateMachine.Text + " : " + healthPoints, 10, Color.black, false);
		Draw.Ring(Pos, Vector3.up, playerLoseRadius, 1f, Color.white, true);
		Draw.Ring(Pos, Vector3.up, playerDetectRadius, 1f, Color.black, true);
		PathMesh.DrawPathLines(currentPath);
	}
}
