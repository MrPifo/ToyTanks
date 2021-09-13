using UnityEngine;

public abstract class BossAI : TankAI {

	public bool HasBeenInitialized { get; set; }
	public LayerMask obstacleLayers;

	public override void Revive() {
		var health = healthPoints;
		base.Revive();
		healthPoints = health;
	}
}
