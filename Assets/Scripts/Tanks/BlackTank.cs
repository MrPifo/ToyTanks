using Cysharp.Threading.Tasks;
using UnityEngine;

public class BlackTank : TankAI {

    [Header("Black Tank")]
    public float patrolRadius;
    bool isStuck;

    public override void InitializeTank() {
        base.InitializeTank();
        Brain.Initialize();
        SetAiming(AimingMode.AimAtPlayerOnSight);
        Brain.enabled = true;
        Patrol().Forget();
    }

    async UniTaskVoid Patrol() {
        if (IsPlayReady == false) return;
        Brain.ResetParameters();
        Brain.ResetChunks();
        await Brain.SetRandomGoalAsync(Pos, patrolRadius, 5, !isStuck);
        isStuck = false;

        while (Brain.distanceToGoal > 2 && Brain.IsStuckCantReachGoal == false && IsPlayReady) {
            if (RandomShootChance() && HasSightContactToPlayer && RequestAttack(1f)) {
                ShootBullet();
            }
            await CheckPause();
        }
        isStuck = Brain.IsStuck();
        await CheckPause();
        Patrol().Forget();
    }

    protected override void GotDestroyed() {
        base.GotDestroyed();
        Brain.Disable();
        Brain.enabled = false;
    }
}
