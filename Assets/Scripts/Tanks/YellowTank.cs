using SimpleMan.Extensions;
using Sperlich.Debug.Draw;
using Sperlich.FSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YellowTank : TankAI {

	public int burstAmount = 3;
	public float moveDuration = 2;

	private new void LateUpdate() {
		base.LateUpdate();
		DrawDebug();
	}

	public override void InitializeTank() {
		base.InitializeTank();
		ProcessState(TankState.Move);
	}

	protected override IEnumerator IAttack() {
		int shots = 0;
		int amount = Random(2, burstAmount);
		if(HasSightContactToPlayer) {
			while(shots < amount && HasSightContactToPlayer && IsPlayReady) {
				while(IsAimingAtPlayer == false && IsPlayReady) {
					AimAtPlayer();
					yield return null;
				}
				ShootBullet();
				shots++;
				yield return new WaitForSeconds(reloadDuration);
				yield return IPauseTank();
			}

		}
		ProcessState(TankState.Move);
	}

	protected override IEnumerator IMove() {
		if(RandomPath(Pos, playerDetectRadius, playerDetectRadius * 0.75f)) {
			MoveMode.Push(MovementType.MoveSmart);
			HeadMode.Push(TankHeadMode.AimAtPlayerOnSight);
			while(IsPlayReady) {
				if(HasReachedDestination) {
					break;
				}
				yield return IPauseTank();
			}
		}
		yield return IPauseTank();

		if(HasSightContactToPlayer) {
			ProcessState(TankState.Attack);
		} else {
			ProcessState(TankState.Move);
		}
	}

	protected override void DrawDebug() {
		if(showDebug) {
			base.DrawDebug();
			Draw.Cube(currentDestination, Color.yellow);
			Draw.Cube(nextMoveTarget, Color.green);
		}
	}
}
