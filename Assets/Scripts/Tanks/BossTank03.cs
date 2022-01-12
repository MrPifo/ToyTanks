using DataStructures.RandomSelector;
using SimpleMan.Extensions;
using Sperlich.Debug.Draw;
using Sperlich.FSM;
using Sperlich.PrefabManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossTank03 : BossAI, IHittable, IDamageEffector {

	[Header("Spike Settings")]
	/// <summary>
	/// How fast the spikes will approach the player
	/// </summary>
	public float iceSpikeAppearanceSpeed = 2;
	/// <summary>
	/// Higher values mean lesser spikes
	/// </summary>
	public float iceSpikeAmount = 1f;
	/// <summary>
	/// Maximum distance the boss can target the player
	/// </summary>
	public float maxSummonRange;
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

		trackSound = "ToyMotor";
		healthBar.gameObject.SetActive(false);
		GoToNextState(TankState.GetWeightedRandom(), 1.5f);
	}

	// State Decision Maker
	protected void GoToNextState(BossBehaviour state, float delay = 0f) {
		TankState.Push(state);
		this.Delay(delay, () => ProcessState());
	}
	private void CalculateMove(float delay = 0) {
		if(IsAIEnabled) {
			if(distToPlayer > 16) {
				TankState.AddWeight(BossBehaviour.IIceSpikeAttack, 20f);
			} else {
				TankState.AddWeight(BossBehaviour.ISlowWave, 20f);
			}
			TankState.PushRandomWeighted();
			this.Delay(delay == 0 ? Time.deltaTime : delay, () => ProcessState());
		}
	}

	protected void ProcessState() {
		if(IsPlayReady) {
			switch(TankState.State) {
				case BossBehaviour.IIceSpikeAttack:
					StartCoroutine(IIceSpikeAttack());
					break;
				case BossBehaviour.ISlowWave:
					StartCoroutine(ISlowWave());
					break;
				case BossBehaviour.IMove:
					StartCoroutine(IMove());
					break;
			}
		}
	}

	IEnumerator IIceSpikeAttack() {
		float maxTime = 0;
		TankState.ChangeWeight(BossBehaviour.IIceSpikeAttack, 0);
		MoveMode.Push(MovementType.None);
		HeadMode.Push(TankHeadMode.AimAtPlayer);
		while(IsPlayReady && maxTime < 1f) {
			yield return IPauseTank();
			maxTime += GetTime;
		}
		bool finished = false;
		disable2DirectionMovement = true;
		this.RepeatUntil(() => finished == false, () => this.Delay(Time.deltaTime, () => RotateTank((Player.Pos - Pos).normalized)), null);
		yield return new WaitForSeconds(0.25f);
		finished = true;
		disable2DirectionMovement = false;

		float lerp = 1f;
		int spikeAmountRange = Mathf.RoundToInt(Vector3.Distance(Pos, Player.Pos) / iceSpikeAmount);
		Vector3 nearestPoint = Player.Pos;
		if(Vector3.Distance(Pos, nearestPoint) > maxSummonRange) {
			Vector3 dir = (Player.Pos - Pos).normalized;
			nearestPoint = Pos + dir * maxSummonRange;
		}
		for(int i = 1; i < spikeAmountRange + 1; i++) {
			Vector3 summonPos = Vector3.Lerp(Pos + transform.forward * 2, nearestPoint, lerp);
			IceSpike spike = PrefabManager.Spawn<IceSpike>(PrefabTypes.IceSpike, null, summonPos);
			spike.SummonSpike((spikeAmountRange - i) * iceSpikeAppearanceSpeed);
			lerp -= 1f / spikeAmountRange;
			yield return IPauseTank();
		}

		TankState.AddWeight(BossBehaviour.IMove, 40);
		CalculateMove(1f + reloadDuration);
	}

	IEnumerator ISlowWave() {
		TankState.ChangeWeight(BossBehaviour.ISlowWave, 10);
		MoveMode.Push(MovementType.Chase);
		HeadMode.Push(TankHeadMode.AimAtPlayer);
		float time = 0;
		while(time < 6) {
			if(HasSightContactToPlayer && Vector3.Distance(Pos, Player.Pos) < sprayDistance) {
				break;
			}
			yield return IPauseTank();
			time += GetTime;
		}

		MoveMode.Push(MovementType.None);
		yield return new WaitForSeconds(0.2f);
		GameCamera.ShortShake2D(0.1f, 50, 100);
		var sm = spraySmokeParticles.shape;
		var sf = sprayFloksParticles.shape;
		sm.angle = sprayAngle;
		sf.angle = sprayAngle;
		spraySmokeParticles.Play();
		sprayFloksParticles.Play();
		float frostTime = 0;
		AudioPlayer.Play("SnowBlow", AudioType.SoundEffect, 1f, 1f);

		while(frostTime < 1.25f) {
			if(Vector3.Distance(Pos, Player.Pos) < sprayDistance * 1.2f && HasSightContactToPlayer) {
				Player.FrostEffect(frostDuration);
				break;
			}
			yield return IPauseTank();
			frostTime += GetTime;
		}

		TankState.AddWeight(BossBehaviour.IMove, 30);
		CalculateMove(1f);
	}

	public override void TakeDamage(IDamageEffector effector, bool instantKill = false) {
		base.TakeDamage(effector);
		BossUI.BossTakeDamage(this, 1);
	}

	protected override IEnumerator IMove() {
		TankState.SubtractWeight(BossBehaviour.ISlowWave, 20f);
		HeadMode.Push(TankHeadMode.AimAtPlayer);
		if(RandomPath(Pos, playerLoseRadius, playerLoseRadius * 0.5f, true)) {
			MoveMode.Push(MovementType.MovePath);
		} else {
			RandomPath(Pos, playerLoseRadius, 0f, false);
		}

		float maxTime = 0;
		while(HasReachedDestination == false && maxTime < 4f) {
			if(RandomShootChance() && distToPlayer > 8) {
				ShootBullet();
			}
			yield return IPauseTank();
			maxTime += GetTime;
		}

		// A little pause for easier player damage
		if(Random(0f, 1f) <= 0.3f) {
			yield return new WaitForSeconds(2);
		}
		CalculateMove(0);
	}

	protected override void DrawDebug() {
		if(showDebug) {
			Draw.Text(Pos + Vector3.up, TankState.State.ToString());
			AIGrid.DrawPathLines(currentPath);
		}
	}
}
