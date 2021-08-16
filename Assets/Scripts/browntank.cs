using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrownTank : TankAI {

	protected override void Awake() {
		base.Awake();
	}

	public override void Idle() {
		stateMachine.Push(TankState.Attack);
	}

	public override void Attack() {
		if(HasSightContact(player)) {
			Aim();
			if(CanShoot) {
				if(IsAimingOnTarget(player.transform) && IsPlayerMinShootRange || IsAimingOnTarget(player.transform) && !PotentialFriendlyFire()) {
					ShootBullet();
				}
			}
		}
	}

	public override void Defense() {
		
	}
	public override void Patrol() {
		
	}
}
