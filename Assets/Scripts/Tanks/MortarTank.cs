using UnityEngine;
using DG.Tweening;
using Sperlich.PrefabManager;
using SimpleMan.Extensions;
using Cysharp.Threading.Tasks;

public class MortarTank : TankAI {

	[Header("Mortar")]
	public GameObject mortar;
	public FloatGrade blastTriggerRange = new FloatGrade(25);
	public FloatGrade blastRadius;
	public FloatGrade flyDuration;
	Vector3 pelletSpawnPos;

	public bool fireFromPlayer => false;
	public bool isPlayerInBlastRadius => Vector3.Distance(Pos, Target.Pos) < blastTriggerRange.Value;
	public Vector3 damageOrigin { get; set; }

	protected override void Awake() {
		base.Awake();
	}

	public override void InitializeTank() {
		base.InitializeTank();
		Attack().Forget();
	}

	async UniTaskVoid Attack() {
		if (IsPlayReady == false) return;
		try {
			stateMachine.Push(TankState.Attack);
			await UniTask.WaitUntil(() => isPlayerInBlastRadius && CanShoot && RequestAttack(5f));
			await CheckPause();
			pelletSpawnPos = this.FindChild("pelletspawn").transform.position;

			// Setup bullet flying path
			Vector3 impactPosition = Target.Pos;
			Vector3 headLookRotation = (impactPosition - Pos).normalized;
			Vector3 inaccuratePos = Vector3.zero;
			if (enableInaccurateAim) {
				inaccuratePos = new Vector3(Random(-inaccurateAim, inaccurateAim), 0, Random(-inaccurateAim, inaccurateAim));
			}
			damageOrigin = impactPosition;

			// Fix head rotation and set impact indicator close to ground
			headLookRotation.y = 0;
			impactPosition.y = 0.05f;

			bool isHeadAligned = false;
			transform.DORotate(Quaternion.LookRotation(headLookRotation).eulerAngles, 0.2f).OnComplete(() => isHeadAligned = true);
			await UniTask.WaitUntil(() => isHeadAligned && IsPlayReady);
			await CheckPause();
			// Animate head
			mortar.transform.localScale = Vector3.one * 1.2f;
			mortar.transform.DOScale(Vector3.one, 0.35f);

			ShootBullet(inaccuratePos);
		} catch(System.Exception e) {
			Logger.LogError(e, "Failed to shoot mortar pellet.");
        }
		Attack().Forget();
	}

	void ShootBullet(Vector3 inaccuratePos = new Vector3()) {
		if (CanShoot) {
			Pellet pellet = PrefabManager.Spawn<Pellet>(PrefabTypes.MortarPellet);
			pellet.SetupPellet(flyDuration, blastRadius, 10);
			pellet.BlastOff(pelletSpawnPos, Target.Pos + inaccuratePos, () => Attack());
			muzzleFlash.Play();
			isReloading = true;
			this.Delay(reloadDuration + Random(0f, randomReloadDuration), () => isReloading = false);
		}
	}
}
