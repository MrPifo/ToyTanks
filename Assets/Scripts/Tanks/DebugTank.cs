using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public class DebugTank : TankAI {


	public override void InitializeTank() {
		base.InitializeTank();
		Patrol().Forget();
	}

	async UniTaskVoid Attack() {
		stateMachine.Push(TankState.Attack);
		if(RemainingPathPercent < 25 && await RandomPathAsync(Pos, 30, 16, true) == false) {
			Patrol().Forget();
			return;
        }
		enableInaccurateAim = false;
		SetMovement(MovementType.MovePath);
		SetAiming(AimingMode.AimAtPlayerOnSight);
		await CheckPause();
		await UniTask.WaitForSeconds(reloadDuration + randomReloadDuration);
		await CheckPause();

		if (HasSightContactToPlayer && RequestAttack(3)) {
			ShootBullet();
			Attack().Forget();
			return;
		} else {
			SetAiming(AimingMode.RandomAim);
			enableInaccurateAim = true;
			while (HasSightContactToPlayer == false && FinalDestInReach == false) {
				await CheckPause();
			}
			Patrol().Forget();
		}
    }

	async UniTaskVoid Patrol() {
		stateMachine.Push(TankState.Patrol);
		if(await RandomPathAsync(Pos, 40, 20, true)) {
			SetMovement(MovementType.MovePath);
			SetAiming(AimingMode.AimAtPlayerOnSight);
			enableInaccurateAim = true;
			while (FinalDestInReach == false) {
				if (HasSightContactToPlayer) {
					Attack().Forget();
					return;
				}
				await CheckPause();
			}
		}

		Patrol().Forget();
	}
}