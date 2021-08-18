using SimpleMan.Extensions;
using Sperlich.Debug.Draw;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreyTank : TankAI {

	public enum GreyTankStates { None, AttackApproach, AttackRetreat }

	public float attackPathRefreshIntervall = 1f;
	public GreyTankStates behaviour;
	float behaviourAttackTime;

	protected override void Awake() {
		base.Awake();
	}

	public override void Idle() {
		stateMachine.Push(TankState.Patrol);
	}

	public override void Attack() {
		if(IsPlayerInShootRadius == false || HasSightContactToPlayer == false) {
			if(behaviour == GreyTankStates.None) {
				if(Time.frameCount % pathUpdateInvervall == 0) {
					FetchPathToPlayer();
				}
			} else {
				if(Time.frameCount % pathUpdateInvervall == 0) {
					FetchPathToPoint(activePatrolPoint);
				}
				behaviourAttackTime += Time.deltaTime;
				if(behaviourAttackTime > attackPathRefreshIntervall) {
					behaviour = GreyTankStates.None;
				}
			}
		}
		if(IsPlayerInShootRadius && HasSightContactToPlayer) {
			if(behaviour == GreyTankStates.None) {
				behaviour = GreyTankStates.AttackApproach;
				behaviourAttackTime = 0;
			}
			if(behaviour == GreyTankStates.AttackRetreat) {
				if(Time.frameCount % pathUpdateInvervall == 0) {
					FetchPathToPoint(activePatrolPoint);
				}
				if(PathNodeCount <= 2) {
					RefreshRandomPath(GetFurthestPointFrom(player.Pos, player.Pos, shootRadius).pos, shootRadius);
					behaviourAttackTime = 0;
				}
			} else {
				if(Time.frameCount % pathUpdateInvervall == 0) {
					FetchPathToPlayer();
				}
				ShootBullet();
			}

			Aim();
			behaviourAttackTime += Time.deltaTime;

			if(behaviourAttackTime > attackPathRefreshIntervall) {
				if(behaviour == GreyTankStates.AttackRetreat) {
					behaviour = GreyTankStates.AttackApproach;
				} else {
					behaviour = GreyTankStates.AttackRetreat;
				}
				RefreshRandomPath(GetFurthestPointFrom(player.Pos, player.Pos, shootRadius).pos, shootRadius);
				behaviourAttackTime = 0;
			}
		}
		MoveAlongPath();

		if(IsPlayerOutsideLoseRadius) {
			stateMachine.Push(TankState.Patrol);
		}
	}

	public override void Defense() {
		
	}

	public override void Patrol() {
		if(HasActivePatrolPoint == false || PathNodeCount <= 2) {
			activePatrolPoint = GetRandomPointOnMap(Pos, playerLoseRadius);
		}
		if(Time.frameCount % pathUpdateInvervall == 0) {
			FetchPathToPoint(activePatrolPoint);
		}
		MoveAlongPath();
		if(IsPlayerInDetectRadius) {
			stateMachine.Push(TankState.Attack);
		}
	}

	public override void DrawDebug() {
		Draw.Text(Pos + Vector3.up * 2, stateMachine.Text + " : " + healthPoints, 10, Color.black, false);
		Draw.Ring(Pos, Vector3.up, playerLoseRadius, 1f, Color.white, true);
		Draw.Ring(Pos, Vector3.up, playerDetectRadius, 1f, Color.black, true);
		Draw.Ring(Pos, Vector3.up, Mathf.Min(shootRadius, distToPlayer), 1f, Color.red, true);
		pathMesh.DrawPathLines(currentPath);
	}
}
