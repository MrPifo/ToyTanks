using SimpleMan.Extensions;
using Sperlich.Debug.Draw;
using Sperlich.FSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YellowTank : TankAI {

	public int burstAmount = 3;
	public float moveDuration = 2;
	public float burstCooldown;

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
		if(HasSightContactToPlayer) {
			while(shots < burstAmount && HasSightContactToPlayer && IsPlayReady) {
				while(IsAimingAtPlayer == false && IsPlayReady) {
					AimAtPlayer();
					yield return null;
				}
				ShootBullet();
				shots++;
				yield return new WaitForSeconds(reloadDuration + randomReloadDuration);
				yield return IPauseTank();
			}

		}
		ProcessState(TankState.Move);
	}

	protected override IEnumerator IMove() {
		float time = 0;
		if(RandomPath(Pos, playerDetectRadius, playerDetectRadius * 0.75f)) {
			MoveMode.Push(MovementType.MoveSmart);
			HeadMode.Push(TankHeadMode.AimAtPlayerOnSight);
			while(IsPlayReady && HasReachedDestination == false) {
				if(time > burstCooldown) {
					if(HasSightContactToPlayer) {
						break;
					}
				}
				yield return IPauseTank();
				time += GetTime;
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
