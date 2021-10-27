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
				while(IsPaused) yield return null;   // Pause AI
			}

		}
		ProcessState(TankState.Move);
	}

	protected override IEnumerator IMove() {
		if(RandomPath(Pos, playerDetectRadius, playerDetectRadius * 0.75f)) {
			while(IsPlayReady) {
				MoveSmart();
				KeepHeadRot();
				ConsumePath();

				/*if(distToPlayer < 6) {
					// TODO: Make fleeing as standalone state
					FleeFrom(Player.Pos, playerDetectRadius);
				}*/
				if(HasSightContactToPlayer) {
					AimAtPlayer();
				}
				if(HasReachedDestination) {
					break;
				}
				yield return null;
				while(IsPaused) yield return null;   // Pause AI
			}
		}
		yield return null;

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
