using Cysharp.Threading.Tasks;
using UnityEngine;

public class GreyTank : TankAI {
	
	[Header("Grey")]
	public float moveRadius;
	public float approachRadius;

	public override void InitializeTank() {
		base.InitializeTank();
		keepMoving = true;
		Patrol().Forget();
	}

	async UniTaskVoid Attack() {
		if (IsPlayReady == false) return;
		await CheckPause();
		stateMachine.Push(TankState.Attack);
		if (RemainingPathPercent < 20 && await RandomPathAsync(Pos, moveRadius, moveRadius / 2f) == false) {
			Patrol().Forget();
			return;
		}

		SetAiming(AimingMode.AimAtPlayerOnSight);
		await CheckPause();
		await UniTask.WaitForSeconds(reloadDuration + randomReloadDuration);
		await CheckPause();

		if (HasSightContactToPlayer && RequestAttack(2f)) {
			ShootBullet();
			Attack().Forget();
			return;
		} else {
			SetAiming(AimingMode.RandomAim);
			while (HasSightContactToPlayer == false && FinalDestInReach == false && Vector3.Distance(Pos, Target.Pos) < approachRadius) {
				await CheckPause();
			}
			Patrol().Forget();
		}
	}

	async UniTaskVoid CatchupToPlayer() {
		if (IsPlayReady == false) return;
		await CheckPause();
		stateMachine.Push(TankState.Chase);
		SetAiming(AimingMode.AimAtPlayerOnSight);

		if(await FindPathToPlayerAsync()) {
			SetMovement(MovementType.MovePath);
			while (Vector3.Distance(Pos, Target.Pos) > approachRadius / 2f) {
				if(await FindPathToPlayerAsync() == false) {
					break;
				}
				await UniTask.WaitForSeconds(1f);
				await CheckPause();
			}
		}
		await CheckPause();
		Patrol().Forget();
    }

	async UniTaskVoid Patrol() {
		if (IsPlayReady == false) return;
		await CheckPause();
		stateMachine.Push(TankState.Patrol);

		if (await RandomPathAsync(Pos, moveRadius, moveRadius / 2f)) {
			SetMovement(MovementType.MovePath);
			SetAiming(AimingMode.KeepRotation);
			while (FinalDestInReach == false) {
				await CheckPause();
				if (HasSightContactToPlayer) {
					Attack().Forget();
					return;
				}
				if(Vector3.Distance(Pos, Target.Pos) > approachRadius) {
					CatchupToPlayer().Forget();
					return;
                }
			}
		}

		Patrol().Forget();
	}
}
