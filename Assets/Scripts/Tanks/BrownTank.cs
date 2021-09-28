using SimpleMan.Extensions;
using Sperlich.FSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrownTank : TankAI {

	public enum BrownState { Waiting, Attack }
	protected FSM<BrownState> stateMachine = new FSM<BrownState>();

	public override void InitializeTank() {
		base.InitializeTank();
		GoToNextState();
	}

	IEnumerator Attack() {
		while(IsPlayReady) {
			if(HasSightContactToPlayer) {
				Aim();
				if(CanShoot) {
					if(IsAimingOnTarget(Player.transform) && !WouldFriendlyFire) {
						ShootBullet();
					}
				}
			}
			yield return null;
		}
	}

	protected override void ProcessState() {
		if(IsPlayReady) {
			switch(stateMachine.State) {
				case BrownState.Attack:
					StartCoroutine(Attack());
					break;
			}
		}
	}

	protected override void GoToNextState(float delay = 0.0001f) {
		if(IsPlayReady) {
			this.Delay(delay, () => {
				stateMachine.Push(BrownState.Waiting);
				while(stateMachine == BrownState.Waiting) {
					stateMachine.Push(stateMachine.GetRandom());
				}
				ProcessState();
			});
		}
	}
}
