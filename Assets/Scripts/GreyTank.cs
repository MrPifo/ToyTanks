using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreyTank : TankAI {

	public float stopDistanceToPlayer = 15f;

	protected override void Awake() {
		base.Awake();
	}

	public override void Move() {

	}

	public override void Idle() {
		if(IsPlayerInRange) {
			state = TankState.Attack;
		}
	}

	public override void Attack() {
		
	}
}
