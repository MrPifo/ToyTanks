using System;


public abstract class BossAI : TankAI {

	public abstract void Initialize();
	public abstract void GoToNextState(float delay = 0);
}
