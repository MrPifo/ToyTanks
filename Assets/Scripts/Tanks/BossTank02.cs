using DG.Tweening;
using SimpleMan.Extensions;
using Sperlich.Debug.Draw;
using Sperlich.FSM;
using Sperlich.PrefabManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// HDRP Related: using UnityEngine.Rendering.HighDefinition;

public class BossTank02 : BossAI, IHittable, IDamageEffector {

	public float bigBlastExplodeRadius;
	public float spreadBlastRadius;
	public int spreadBlastAmount;
	public AnimationCurve pelletFlyingCurve;
	[Header("Impact Explosion Parts")]
	public GameObject pelletPrefab;
	public ParticleSystem blastFireParticles;
	public Transform pelletSpawnPosTransform;
	Vector3 pelletSpawnPos;

	public bool fireFromPlayer => false;
	public Vector3 damageOrigin => pelletPrefab.transform.position;
	public enum BossBehaviour { Waiting, BigBlast, SpreadBlast, QuickMove }
	FSM<BossBehaviour> bossStates = new FSM<BossBehaviour>();

	// Boss Moves:
	// 
	// 

	protected override void Awake() {
		base.Awake();
	}

	public override void InitializeTank() {
		base.InitializeTank();
		bossStates.Push(BossBehaviour.Waiting);
		GoToNextState(2);
		healthBar.gameObject.SetActive(false);
	}

	// State Decision Maker
	protected override void GoToNextState(float delay = 0) {
		if(IsAIEnabled) {
			this.Delay(delay, () => {
				bossStates.Push(BossBehaviour.Waiting);
				while(bossStates == BossBehaviour.Waiting || bossStates == BossBehaviour.QuickMove) {
					bossStates.Push(bossStates.GetRandom());
				}
				ProcessState();
			});
		}
	}

	protected void ProcessState() {
		if(IsPlayReady) {
			pelletSpawnPos = this.FindChild("pelletspawn").transform.position;
			switch(bossStates.State) {
				case BossBehaviour.BigBlast:
					StartCoroutine(IBigBlast());
					break;
				case BossBehaviour.SpreadBlast:
					StartCoroutine(ISpreadBlast());
					break;
				case BossBehaviour.QuickMove:
					StartCoroutine(IQuickMove());
					break;
			}
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

	protected IEnumerator IBigBlast() {
		// Setup bullet flying path
		pelletSpawnPos = pelletSpawnPosTransform.position;
		Vector3 impactPosition = Player.Pos;
		Vector3 headLookRotation = (impactPosition - Pos).normalized;

		// Fix head rotation and set impact indicator close to ground
		headLookRotation.y = 0;
		impactPosition.y = 0.05f;

		bool isHeadAligned = false;
		transform.DORotate(Quaternion.LookRotation(headLookRotation).eulerAngles, 0.2f).OnComplete(() => isHeadAligned = true);
		yield return new WaitUntil(() => isHeadAligned && IsPlayReady);
		// Animate head
		tankBody.transform.localScale = Vector3.one * 1.2f;
		tankBody.transform.DOScale(Vector3.one, 0.35f);

		Pellet pellet = PrefabManager.Spawn<Pellet>(PrefabTypes.MortarPellet);
		pellet.SetupPellet(2f, bigBlastExplodeRadius, 25);
		pellet.transform.localScale *= 1.5f;
		// HDRP Related: pellet.pelletBlobShadow.GetComponent<DecalProjector>().size = new Vector3(1.5f, 1.5f, 20f);
		pellet.AdjustPellet(pelletFlyingCurve);
		pellet.BlastOff(pelletSpawnPos, Player.Pos, null);
		blastFireParticles.Play();
		yield return new WaitForSeconds(1.5f);
		bossStates.Push(BossBehaviour.QuickMove);
		ProcessState();
	}

	protected IEnumerator ISpreadBlast() {
		for(int i = 0; i < Random(spreadBlastAmount, spreadBlastAmount * 2); i++) {
			pelletSpawnPos = pelletSpawnPosTransform.position;
			Vector3 impactPosition = Player.Pos + new Vector3(Random(-spreadBlastRadius, spreadBlastRadius), 0, Random(-spreadBlastRadius, spreadBlastRadius));
			Vector3 headLookRotation = (impactPosition - Pos).normalized;
			bool isHeadAligned = false;
			transform.DORotate(Quaternion.LookRotation(headLookRotation).eulerAngles, 0.1f).OnComplete(() => isHeadAligned = true);
			yield return new WaitUntil(() => isHeadAligned && IsPlayReady);
			tankBody.transform.localScale = Vector3.one * 1.3f;
			tankBody.transform.DOScale(Vector3.one, 0.1f);

			Pellet pellet = PrefabManager.Spawn<Pellet>(PrefabTypes.MortarPellet);
			pellet.SetupPellet(1.5f, 3, 3);
			pellet.AdjustPellet(pelletFlyingCurve);
			pellet.BlastOff(pelletSpawnPos, impactPosition);
			blastFireParticles.Play();
			yield return new WaitForSeconds(reloadDuration);
			yield return IPauseTank();
		}
		GoToNextState(2);
	}

	protected IEnumerator IQuickMove() {
		float time = 0;
		if(RandomPath(Pos, playerDetectRadius, playerDetectRadius * 0.75f)) {
			MoveMode.Push(MovementType.MovePath);
			HeadMode.Push(TankHeadMode.KeepRotation);
			while(IsPlayReady) {
				if(HasReachedDestination || time > 3f) {
					break;
				}
				time += GetTime;
				yield return IPauseTank();   // Pause AI
			}
		}
		MoveMode.Push(MovementType.None);
		if(Random(0, 1) == 0) {
			bossStates.Push(BossBehaviour.BigBlast);
		} else {
			bossStates.Push(BossBehaviour.SpreadBlast);
		}
		ProcessState();
	}

	protected override void DrawDebug() {
		if(showDebug) {
			base.DrawDebug();
			Draw.Text(Pos + Vector3.up * 2, bossStates.Text, 8, Color.black);
		}
	}
}
