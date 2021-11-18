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
				if(CanShoot) {
					if(IsFacingTarget(Player.transform)) {
						ShootBullet();
					}
				}
			}
			yield return null;
			while(IsPaused) yield return null;   // Pause AI
		}
	}
}
