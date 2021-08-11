using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrownTank : TankAI {

	protected override void Awake() {
		base.Awake();
	}

	public override void Idle() {
		state = TankState.Attack;
	}

	public override void Move() {
		
	}

	public override void Attack() {
		if(HasSightContact(player.gameObject)) {
			Aim();
			if(CanShoot) {
				if(IsAimingOnTarget(player.transform) && IsPlayerInRange || IsAimingOnTarget(player.transform) && !PotentialFriendlyFire()) {
					ShootBullet();
				}
			}
		}
	}
}
