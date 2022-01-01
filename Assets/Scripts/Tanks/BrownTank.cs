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
		bool bulletShot = false;
		while(IsPlayReady) {
			if(HasSightContactToPlayer) {
				AimAtPlayer();
				if(CanShoot) {
					if(IsFacingTarget(Player.transform) && bulletShot == false) {
						bulletShot = true;
						this.Delay(Random(reloadDuration, reloadDuration * 2), () => {
							ShootBullet();
							bulletShot = false;
						});
					}
				}
			}
			yield return null;
			while(IsPaused) yield return null;   // Pause AI
		}
	}
}
