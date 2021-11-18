﻿using UnityEngine;

public abstract class BossAI : TankAI {

	public override void Revive() {
		var health = healthPoints;
		base.Revive();
		healthPoints = health;
	}
}
