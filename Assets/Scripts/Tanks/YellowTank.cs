using Cysharp.Threading.Tasks;
using UnityEngine;

public class YellowTank : TankAI {

	[Header("Yellow")]
	public float moveRadius;
	public FloatGrade burstAmount;
	public FloatGrade burstCooldown;
	public float idleTime;

	public override void InitializeTank() {
		base.InitializeTank();
		Patrol().Forget();
	}

	async UniTaskVoid Attack() {
		if (IsPlayReady == false) return;
		stateMachine.Push(TankState.Attack);
		if(await FindPathToPlayerAsync() == false) {
			Patrol().Forget();
			return;
        }
		idleTime = 0;
		SetMovement(MovementType.MovePath);
		SetAiming(AimingMode.AimAtPlayer);
		await CheckPause();
		await UniTask.WaitUntil(() => IsAimingAtPlayer);
		int shots = 0;
		keepMoving = true;
		while (shots < burstAmount && HasSightContactToPlayer) {
			await UniTask.WaitUntil(() => CanShoot);
			ShootBullet();
			shots++;
			await CheckPause();
		}
		Patrol().Forget();
	}

	async UniTaskVoid Patrol() {
		if (IsPlayReady == false) return;
		stateMachine.Push(TankState.Patrol);
		SetMovement(MovementType.MovePath);
		SetAiming(AimingMode.KeepRotation);
		if (await RandomPathAsync(Pos, moveRadius, moveRadius / 2f, false, true)) {
			while (FinalDestInReach == false) {
				idleTime += Time.fixedDeltaTime;
				if(HasSightContactToPlayer && idleTime > burstCooldown && RequestAttack(2f)) {
					Attack().Forget();
					return;
                }
				await CheckPause();
			}
		}
		if (HasSightContactToPlayer && idleTime > burstCooldown && RequestAttack(2f)) {
			Attack().Forget();
		} else {
			Patrol().Forget();
		}
	}
}
