using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrownTank : TankAI {

	protected override void Awake() {
		base.Awake();
	}

	public override void Idle() {
		if(IsPlayerInDetectRadius && HasSightContactToPlayer) {
			stateMachine.Push(TankState.Attack);
		}
	}

	public override void Attack() {
		if(!IsPlayerInDetectRadius || !HasSightContactToPlayer) {
			stateMachine.Push(TankState.Idle);
			return;
		}
		Aim();
		if(CanShoot) {
			if(IsAimingOnTarget(player.transform) && IsPlayerMinShootRange || IsAimingOnTarget(player.transform) && !WouldFriendlyFire) {
				ShootBullet();
			}
		}
	}

	public override void Defense() {
		
	}
	public override void Patrol() {
		
	}
}
