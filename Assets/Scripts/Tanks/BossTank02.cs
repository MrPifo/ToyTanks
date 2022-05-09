using DG.Tweening;
using Sperlich.Debug.Draw;
using Sperlich.FSM;
using Sperlich.PrefabManager;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BossTank02 : BossAI, IHittable, IDamageEffector {

	[Header("Boss 2")]
	public float moveRadius = 25;
	public FloatGrade bigBlastEscapeTime;
	public FloatGrade bigBlastExplodeRadius;
	public FloatGrade spreadBlastRadius;
	public FloatGrade spreadBlastAmount;
	public AnimationCurve pelletFlyingCurve;
	[Header("Impact Explosion Parts")]
	public GameObject pelletPrefab;
	public ParticleSystem blastFireParticles;
	public Transform pelletSpawnPosTransform;
	Vector3 pelletSpawnPos;

	public bool fireFromPlayer => false;
	public Vector3 damageOrigin => pelletPrefab.transform.position;
	public enum BossBehaviour { Waiting, BigBlast, SpreadBlast, QuickMove }
	new FSM<BossBehaviour> TankState = new FSM<BossBehaviour>();

	public override void InitializeTank() {
		base.InitializeTank();
		TankState.FillWeightedStates(new List<(BossBehaviour, float)>() {
			(BossBehaviour.QuickMove, 25f),
			(BossBehaviour.SpreadBlast, 50f),
			(BossBehaviour.BigBlast, 50f),
		});
		CalculateMove().Forget();
	}

	// State Decision Maker
	async UniTaskVoid CalculateMove(int delay = 0) {
		if (IsPlayReady == false) return;
		await CheckPause();
		TankState.AddWeight(BossBehaviour.QuickMove, 40f);
		TankState.AddWeight(BossBehaviour.BigBlast, 20f);
		TankState.AddWeight(BossBehaviour.SpreadBlast, 30f);
		TankState.PushRandomWeighted();
		await UniTask.Delay(delay == 0 ? 100 : delay);
		ProcessState().Forget();
	}

	async UniTaskVoid ProcessState() {
		if (IsPlayReady == false) return;
		await CheckPause();
		pelletSpawnPos = this.FindChild("pelletspawn").transform.position;
		switch (TankState.State) {
			case BossBehaviour.BigBlast:
				BigBlast().Forget();
				break;
			case BossBehaviour.SpreadBlast:
				SpreadBlast().Forget();
				break;
			case BossBehaviour.QuickMove:
				QuickMove().Forget();
				break;
		}
	}

	// State Execution
	public override void TakeDamage(IDamageEffector effector, bool instantKill = false) {
		base.TakeDamage(effector);
		BossUI.BossTakeDamage(this, 1);
	}

	protected override void GotDestroyed() {
		base.GotDestroyed();
		BossUI.RemoveBoss(this);
	}

	async UniTaskVoid BigBlast() {
		if (IsPlayReady == false) return;
		// Setup bullet flying path
		pelletSpawnPos = pelletSpawnPosTransform.position;
		Vector3 impactPosition = Target.Pos;
		Vector3 headLookRotation = (impactPosition - Pos).normalized;

		// Fix head rotation and set impact indicator close to ground
		headLookRotation.y = 0;
		impactPosition.y = 0.05f;

		bool isHeadAligned = false;
		transform.DORotate(Quaternion.LookRotation(headLookRotation).eulerAngles, 0.2f).OnComplete(() => isHeadAligned = true);
		await UniTask.WaitUntil(() => isHeadAligned && IsPlayReady);
		// Animate head
		tankBody.transform.localScale = Vector3.one * 1.2f;
		tankBody.transform.DOScale(Vector3.one, 0.35f);

		Pellet pellet = PrefabManager.Spawn<Pellet>(PrefabTypes.MortarPellet);
		pellet.SetupPellet(bigBlastEscapeTime, bigBlastExplodeRadius, 25);
		pellet.AdjustPellet(pelletFlyingCurve);
		pellet.BlastOff(pelletSpawnPos, Target.Pos, null);
		blastFireParticles.Play();
		await UniTask.Delay(2000);
		QuickMove().Forget();
	}

	async UniTaskVoid SpreadBlast() {
		if (IsPlayReady == false) return;
		for (int i = 0; i < spreadBlastAmount; i++) {
			pelletSpawnPos = pelletSpawnPosTransform.position;
			Vector3 impactPosition = Target.Pos + new Vector3(Random(-spreadBlastRadius, spreadBlastRadius), 0, Random(-spreadBlastRadius, spreadBlastRadius));
			Vector3 headLookRotation = (impactPosition - Pos).normalized;
			bool isHeadAligned = false;
			transform.DORotate(Quaternion.LookRotation(headLookRotation).eulerAngles, 0.1f).OnComplete(() => isHeadAligned = true);
			await UniTask.WaitUntil(() => isHeadAligned && IsPlayReady);
			tankBody.transform.localScale = Vector3.one * 1.3f;
			tankBody.transform.DOScale(Vector3.one, 0.1f);

			Pellet pellet = PrefabManager.Spawn<Pellet>(PrefabTypes.MortarPellet);
			pellet.SetupPellet(1.5f, 3, 3);
			pellet.AdjustPellet(pelletFlyingCurve);
			pellet.BlastOff(pelletSpawnPos, impactPosition);
			blastFireParticles.Play();
			await UniTask.WaitForSeconds(reloadDuration);
			await CheckPause();
		}
		await UniTask.Delay(2000);
		QuickMove().Forget();
	}

	async UniTaskVoid QuickMove() {
		if (IsPlayReady == false) return;
		float time = 0;
		if(await RandomPathAsync(Pos, moveRadius, moveRadius * 0.75f)) {
			SetMovement(MovementType.MovePath);
			SetAiming(AimingMode.KeepRotation);

			while(IsPlayReady) {
				if(NextPathPointInReach || time > 3f) {
					break;
				}
				time += GetTime;
				await CheckPause();
			}
		}
		SetMovement(MovementType.None);
		CalculateMove().Forget();
	}

	protected override void DrawDebug() {
		if(debugMode) {
			Draw.Text(Pos + Vector3.up * 2, TankState.Text, 8, Color.black);
		}
	}
}
