using SimpleMan.Extensions;
using Sperlich.FSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrownTank : TankAI {

	public override void InitializeTank() {
		base.InitializeTank();
		ProcessState(TankState.Attack);
	}

	protected override IEnumerator IAttack() {
		while(IsPlayReady) {
			if(HasSightContactToPlayer) {
				AimAtPlayer();
				if(CanShoot && IsFacingTarget(Player.transform) && RandomShootChance()) {
					ShootBullet();
					break;
				}
			}
			yield return IPauseTank();
		}
		GoToNextState(TankState.Attack);
	}
}
