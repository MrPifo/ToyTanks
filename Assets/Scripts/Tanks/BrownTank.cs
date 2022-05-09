using Cysharp.Threading.Tasks;
using SimpleMan.Extensions;

public class BrownTank : TankAI {

	public override void InitializeTank() {
		base.InitializeTank();

		// Start tank with reload status
		disableShooting = true;
		this.Delay(reloadDuration, () => disableShooting = false);
		Attack().Forget();
	}

	async UniTaskVoid Attack() {
		while(IsPlayReady) {
			SetAiming(AimingMode.RandomAim);
			if(HasSightContactToPlayer) {
				SetAiming(AimingMode.AimAtPlayerOnSight);
				if(CanShoot && IsFacingTarget(Target.Pos) && RequestAttack(2f)) {
					ShootBullet();
				}
			}
			await CheckPause();
		}
	}
}
