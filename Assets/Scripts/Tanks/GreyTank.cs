using SimpleMan.Extensions;
using Sperlich.Debug.Draw;
using System.Collections;
using UnityEngine;

public class GreyTank : TankAI {

	public override void InitializeTank() {
		base.InitializeTank();
		ProcessState(TankState.Move);
	}

	protected override IEnumerator IMove() {
		MoveMode.Push(MovementType.Chase);
		HeadMode.Push(TankHeadMode.KeepRotation);
		while(IsPlayReady && HasSightContactToPlayer == false) {
			yield return IPauseTank();
		}
		ProcessState(TankState.Attack);
	}

	protected override IEnumerator IAttack() {
		RandomPath(Player.Pos, playerDetectRadius, playerDetectRadius * 0.75f, true);
		MoveMode.Push(MovementType.MovePath);
		HeadMode.Push(TankHeadMode.AimAtPlayerOnSight);
		while(IsPlayReady && HasReachedDestination == false && HasSightContactToPlayer) {
			if(CanShoot && HasSightContactToPlayer && IsAimingAtPlayer && WouldFriendlyFire == false && RandomShootChance()) {
				ShootBullet();
			}
			yield return IPauseTank();
		}
		GoToNextState(TankState.Move);
	}

	protected override void DrawDebug() {
		if(showDebug) {
			base.DrawDebug();
			Draw.Cube(currentDestination, Color.yellow);
			Draw.Cube(nextMoveTarget, Color.green);
		}
	}
}
