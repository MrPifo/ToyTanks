using Sperlich.Debug.Draw;
using SimpleMan.Extensions;
using Sperlich.FSM;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class MortarTank : TankAI, IHittable, IDamageEffector {
	
	public float flyTime;
	public float explodeRadius = 0;
	public float explodeDuration = 1f;
	public float explodeShakeStrength = 8f;
	[Header("Impact Explosion Parts")]
	public GameObject pelletPrefab;
	public GameObject mortar;
	Vector3 pelletSpawnPos;

	public bool fireFromPlayer => false;
	public Vector3 damageOrigin => pelletPrefab.transform.position;

	protected override void Awake() {
		base.Awake();
		pelletSpawnPos = this.FindChild("pelletspawn").transform.position;
	}

	public override void InitializeTank() {
		base.InitializeTank();
		ProcessState(TankState.Attack);
	}

	protected override IEnumerator IAttack() {
		yield return new WaitForSeconds(reloadDuration + Random(0, randomReloadDuration));
		yield return new WaitUntil(() => IsPlayerInShootRadius && IsPlayReady);

		// Setup bullet flying path
		Vector3 impactPosition = Player.Pos;
		Vector3 headLookRotation = (impactPosition - Pos).normalized;

		// Fix head rotation and set impact indicator close to ground
		headLookRotation.y = 0;
		impactPosition.y = 0.05f;

		
		bool isHeadAligned = false;
		transform.DORotate(Quaternion.LookRotation(headLookRotation).eulerAngles, 0.2f).OnComplete(() => isHeadAligned = true);
		yield return new WaitUntil(() => isHeadAligned && IsPlayReady);
		// Animate head
		mortar.transform.localScale = Vector3.one * 1.2f;
		mortar.transform.DOScale(Vector3.one, 0.35f);

		Pellet pellet = Instantiate(pelletPrefab).transform.SearchComponent<Pellet>();
		pellet.SetupPellet(flyTime, explodeRadius, 8);
		pellet.BlastOff(pelletSpawnPos, Player.Pos, () => GoToNextState(TankState.Attack));
	}

	public new void TakeDamage(IDamageEffector effector) {
		base.TakeDamage(effector);
	}

	protected override void GotDestroyed() {
		base.GotDestroyed();
	}

	public override void Revive() {
		base.Revive();
	}

	protected override void DrawDebug() {
		base.DrawDebug();
		if(showDebug) {
			if(IsPlayerInDetectRadius == false) {
				Draw.Ring(Pos, Vector3.up, playerDetectRadius, 1f, Color.red, true);
			}
		}
	}
}
