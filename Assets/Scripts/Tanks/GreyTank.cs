using SimpleMan.Extensions;
using Sperlich.Debug.Draw;
using System.Collections;
using UnityEngine;

public class GreyTank : TankAI {

	public override void InitializeTank() {
		base.InitializeTank();
		StopAllCoroutines();
		ProcessState(TankState.Attack);
	}

	protected override IEnumerator IAttack() {
		int bulletsShot = 0;
		int requiredShots = Random(2, 4);
		while(bulletsShot < requiredShots && IsPlayReady) {
			if(CanShoot && HasSightContactToPlayer && IsAimingAtPlayer && WouldFriendlyFire == false) {
				ShootBullet();
				bulletsShot++;
			} else {
				if(HasSightContactToPlayer) {
					AimAtPlayer();
				} else {
					KeepHeadRot();
				}
				ChasePlayer();
			}
			yield return null;
			while(IsPaused) yield return null;   // Pause AI
		}
		ProcessState(TankState.Retreat);
	}

	protected override IEnumerator IRetreat() {
		float time = 0;
		FleeFrom(Pos, 30);
		while(time < Random(3, 6) && IsPlayReady) {
			MoveAlongPath();
			KeepHeadRot();
			ConsumePath();
			yield return null;
			while(IsPaused) yield return null;   // Pause AI
			time += Time.deltaTime;
		}

		ProcessState(TankState.Attack);
	}

	protected override void DrawDebug() {
		if(showDebug) {
			base.DrawDebug();
			Draw.Cube(currentDestination, Color.yellow);
			Draw.Cube(nextMoveTarget, Color.green);
		}
	}
}
