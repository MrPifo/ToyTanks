using Cysharp.Threading.Tasks;
using SimpleMan.Extensions;
using Sperlich.Debug.Draw;
using Sperlich.FSM;
using Sperlich.PrefabManager;
using System.Collections.Generic;
using UnityEngine;

public class BossTank03 : BossAI, IHittable, IDamageEffector {

	[Header("Boss 3")]
	public float moveRadius = 25;
	/// <summary>
	/// How fast the spikes will approach the player
	/// </summary>
	public FloatGrade iceSpikeAppearanceSpeed;
	/// <summary>
	/// Higher values mean lesser spikes
	/// </summary>
	public FloatGrade iceSpikeAmount;
	/// <summary>
	/// Maximum distance the boss can target the player
	/// </summary>
	public FloatGrade maxSummonRange;
	[Header("Spray Settings")]
	public float sprayDistance = 8;
	public float sprayAngle = 45;
	public float frostDuration = 4;
	public ParticleSystem spraySmokeParticles;
	public ParticleSystem sprayFloksParticles;
	public enum BossBehaviour { Waiting, IIceSpikeAttack, ISlowWave, IMove }
	new FSM<BossBehaviour> TankState = new FSM<BossBehaviour>();
	public bool fireFromPlayer => false;
	public Vector3 damageOrigin => transform.position;

	public override void InitializeTank() {
		base.InitializeTank();

		TankState.FillWeightedStates(new List<(BossBehaviour, float)>() {
			(BossBehaviour.IIceSpikeAttack, 0f),
			(BossBehaviour.ISlowWave, 50f),
			(BossBehaviour.IMove, 100)
		});

		trackSound = JSAM.Sounds.ToyMotor;
		GoToNextState(TankState.GetWeightedRandom(), 1500).Forget();
	}

	// State Decision Maker
	async UniTask GoToNextState(BossBehaviour state, int delay = 0) {
		if (IsPlayReady == false) return;
		TankState.Push(state);
		await UniTask.Delay(delay);
		ProcessState().Forget();
	}

	async UniTaskVoid CalculateMove(int delay = 0) {
		if (IsPlayReady == false) return;
		TankState.AddWeight(BossBehaviour.IIceSpikeAttack, 10f);
		if(distToPlayer > 16) {
			TankState.AddWeight(BossBehaviour.IIceSpikeAttack, 40f);
		} else {
			TankState.AddWeight(BossBehaviour.ISlowWave, 20f);
		}
		TankState.PushRandomWeighted();
		await UniTask.Delay(delay == 0 ? 25 : delay);
		ProcessState().Forget();
	}

	async UniTaskVoid ProcessState() {
		if (IsPlayReady == false) return;
		await CheckPause();
		switch(TankState.State) {
			case BossBehaviour.IIceSpikeAttack:
				IceSpikeAttack().Forget();
				break;
			case BossBehaviour.ISlowWave:
				SlowWave().Forget();
				break;
			case BossBehaviour.IMove:
				QuickMove().Forget();
				break;
		}
	}

	async UniTaskVoid IceSpikeAttack() {
		if (IsPlayReady == false) return;
		float maxTime = 0;
		TankState.ChangeWeight(BossBehaviour.IIceSpikeAttack, 0);
		SetMovement(MovementType.None);
		SetAiming(AimingMode.AimAtPlayer);

		while(IsPlayReady && maxTime < 1f) {
			maxTime += GetTime;
			await CheckPause();
		}
		bool finished = false;
		disable2DirectionMovement = true;
		this.RepeatUntil(() => finished == false, () => this.Delay(Time.deltaTime, () => RotateTank((Target.Pos - Pos).normalized)), null);
		await UniTask.Delay(250);
		finished = true;
		disable2DirectionMovement = false;

		float lerp = 1f;
		int spikeAmountRange = Mathf.RoundToInt(Vector3.Distance(Pos, Target.Pos) / iceSpikeAmount);
		Vector3 nearestPoint = Target.Pos;
		if(Vector3.Distance(Pos, nearestPoint) > maxSummonRange) {
			Vector3 dir = (Target.Pos - Pos).normalized;
			nearestPoint = Pos + dir * maxSummonRange;
		}
		for(int i = 1; i < spikeAmountRange + 1; i++) {
			Vector3 summonPos = Vector3.Lerp(Pos + transform.forward * 2, nearestPoint, lerp);
			IceSpike spike = PrefabManager.Spawn<IceSpike>(PrefabTypes.IceSpike, null, summonPos);
			spike.SummonSpike((spikeAmountRange - i) * iceSpikeAppearanceSpeed);
			lerp -= 1f / spikeAmountRange;
			await CheckPause();
		}

		TankState.AddWeight(BossBehaviour.IMove, 20);
		CalculateMove(Mathf.RoundToInt(reloadDuration * 1000)).Forget();
	}

	async UniTaskVoid SlowWave() {
		if (IsPlayReady == false) return;
		TankState.ChangeWeight(BossBehaviour.ISlowWave, 10);
		SetAiming(AimingMode.AimAtPlayer);
		float time = 0;
		while(time < 6) {
			if(HasSightContactToPlayer && Vector3.Distance(Pos, Target.Pos) < sprayDistance) {
				break;
			}
			time += GetTime;
			await CheckPause();
		}

		SetMovement(MovementType.None);
		await UniTask.Delay(250);
		GameCamera.ShortShake2D(0.1f, 50, 100);
		var sm = spraySmokeParticles.shape;
		var sf = sprayFloksParticles.shape;
		sm.angle = sprayAngle;
		sf.angle = sprayAngle;
		spraySmokeParticles.Play();
		sprayFloksParticles.Play();
		float frostTime = 0;
		AudioPlayer.Play(JSAM.Sounds.SnowBlow, AudioType.SoundEffect, 1f, 1f);

		while(frostTime < 1.25f) {
			if(Vector3.Distance(Pos, Target.Pos) < sprayDistance * 1.2f && HasSightContactToPlayer) {
				FindObjectOfType<PlayerTank>().FrostEffect(frostDuration);
				break;
			}
			frostTime += GetTime;
			await CheckPause();
		}

		TankState.AddWeight(BossBehaviour.IMove, 10);
		CalculateMove(2000).Forget();
	}

	async UniTaskVoid QuickMove() {
		if (IsPlayReady == false) return;
		TankState.SubtractWeight(BossBehaviour.ISlowWave, 20f);
		SetAiming(AimingMode.AimAtPlayer);

		if(await RandomPathAsync(Pos, moveRadius, moveRadius * 0.5f, true)) {
			SetMovement(MovementType.MovePath);
		} else {
			await RandomPathAsync(Pos, moveRadius, 0f, false);
		}

		float maxTime = 0;
		while(NextPathPointInReach == false && maxTime < 4f) {
			if(RandomShootChance() && distToPlayer > 8) {
				ShootBullet();
			}
			maxTime += GetTime;
			await CheckPause();
		}

		// A little pause for easier player damage
		if(Random(0f, 1f) <= 0.3f) {
			await UniTask.Delay(1000);
		}
		CalculateMove(0).Forget();
	}

	public override void TakeDamage(IDamageEffector effector, bool instantKill = false) {
		base.TakeDamage(effector);
		BossUI.BossTakeDamage(this, 1);
	}

	protected override void DrawDebug() {
		if(debugMode) {
			Draw.Text(Pos + Vector3.up, TankState.State.ToString());
		}
	}
}
