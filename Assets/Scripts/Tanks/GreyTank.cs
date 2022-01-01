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
				MoveMode.Push(MovementType.None);
				ShootBullet();
				bulletsShot++;
			} else {
				if(HasSightContactToPlayer) {
					HeadMode.Push(TankHeadMode.AimAtPlayer);
				} else {
					HeadMode.Push(TankHeadMode.RotateWithBody);
				}
				MoveMode.Push(MovementType.Chase);
			}
			yield return IPauseTank();
		}
		ProcessState(TankState.Retreat);
	}

	protected override IEnumerator IRetreat() {
		float time = 0;
		FleeFrom(Pos, 30);

		MoveMode.Push(MovementType.MoveSmart);
		HeadMode.Push(TankHeadMode.KeepRotation);
		while(time < Random(3, 6) && IsPlayReady) {
			yield return IPauseTank();
			time += GetTime;
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
