using SimpleMan.Extensions;
using Sperlich.FSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrownTank : TankAI {

	public enum BrownState { Waiting, Attack }
	protected FSM<BrownState> stateMachine = new FSM<BrownState>();

	protected override void Awake() {
		base.Awake();
	}

	public override void InitializeAI() {
		GoToNextState();
	}

	IEnumerator Attack() {
		while(!HasBeenDestroyed) {
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
		if(HasBeenDestroyed == false) {
			switch(stateMachine.State) {
				case BrownState.Attack:
					StartCoroutine(Attack());
					break;
			}
		}
	}

	protected override void GoToNextState(float delay = 0.0001f) {
		if(IsAIEnabled) {
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
