using Sperlich.Debug.Draw;
using SimpleMan.Extensions;
using Sperlich.FSM;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class MortarTank : TankAI, IHittable, IDamageEffector {

	public AnimationCurve flyCurve;
	public Shapes.Disc impactIndication;
	public float flyTime;
	public GameObject pellet;
	[Header("Impact Explosion Parts")]
	public float explodeDuration = 1f;
	public float explodeShakeStrength = 8f;
	public GameObject mortar;
	public GameObject impactExplosionContainer;
	public GameObject explosionCrater;
	public Transform pelletSpawn;
	public Shapes.Sphere impactExplodeSphere;
	public ParticleSystem impactExplodeFire;
	public ParticleSystem shotExplodeFire;
	public Transform pelletShadow;
	float explodeRadius = 0;

	public bool fireFromPlayer => false;
	public Vector3 damageOrigin => pellet.transform.position;

	protected override void Awake() {
		base.Awake();
		impactIndication.gameObject.SetActive(false);
		explodeRadius = pellet.GetComponent<SphereCollider>().radius;
		impactIndication.gameObject.GetComponent<Shapes.Disc>().Radius = explodeRadius;
		impactExplosionContainer.SetActive(false);
		pellet.SetActive(false);
	}

	public override void InitializeTank() {
		base.InitializeTank();
		pelletShadow.parent = null;
		ProcessState(TankState.Attack);
	}

	protected override IEnumerator IAttack() {
		//var blobShadow = pellet.transform.GetChild(1);
		yield return new WaitForSeconds(reloadDuration + Random(0, randomReloadDuration));
		float time = 0;
		while(IsPlayerInShootRadius == false) {
			yield return null;
			while(IsPaused) yield return null;   // Pause AI
		}
		Vector3 startPos = pelletSpawn.position;
		Vector3 impactPos = Player.Pos;
		Vector3 flyDir = (impactPos - startPos).normalized;
		Vector3 lookRot = (impactPos - Pos).normalized;
		lookRot.y = 0;
		impactPos.y = 0.05f;
		bool allowShot = false;
		impactIndication.gameObject.SetActive(true);
		transform.DORotate(Quaternion.LookRotation(lookRot).eulerAngles, 0.2f).OnComplete(() => {
			allowShot = true;
		});
		while(allowShot == false) {
			impactExplosionContainer.transform.position = impactPos;
			impactIndication.transform.position = impactPos;
			yield return null;
			while(IsPaused) yield return null;   // Pause AI
		}

		yield return new WaitUntil(() => allowShot);
		// Stop if destroyed
		if(IsPlayReady == false) {
			yield break;
		}
		pellet.SetActive(true);
		pelletShadow.gameObject.SetActive(true);
		shotExplodeFire.Play();
		mortar.transform.localScale = Vector3.one * 1.2f;
		mortar.transform.DOScale(Vector3.one, 0.35f);
		AudioPlayer.Play("PelletShot", 1f, 0.5f, 1.5f);
		
		while(time < flyTime) {
			float y = flyCurve.Evaluate(time.Remap(0, flyTime, 0, 1f));
			var pos = Vector3.Lerp(startPos, impactPos, time.Remap(0, flyTime, 0, 1f));
			pos.y = y;
			pellet.transform.position = pos;
			time += Time.deltaTime;
			pellet.transform.Rotate(flyDir, 500 * Time.deltaTime);
			pelletShadow.position = pellet.transform.position;
			yield return null;
			while(IsPaused) yield return null;   // Pause AI
		}
		// Check if Hit
		foreach(var hit in Physics.OverlapSphere(pellet.transform.position, pellet.GetComponent<SphereCollider>().radius, hitLayers)) {
			if(hit.transform.TrySearchComponent(out IHittable hittable)) {
				hittable.TakeDamage(this);
				TakeDamage(this);
			}
		}

		AudioPlayer.Play("SmallExplosion", 0.9f, 1.3f, 1f);
		pellet.SetActive(false);
		pelletShadow.gameObject.SetActive(false);
		impactIndication.gameObject.SetActive(false);
		impactExplosionContainer.SetActive(true);
		Color colorLerp = impactExplodeSphere.Color;
		Color targetColor = colorLerp;
		targetColor.a = 0;
		LevelManager.Feedback?.TankExplode();
		GameCamera.ShakeExplosion2D(explodeShakeStrength, 0.2f);
		impactExplodeFire.Play();
		Instantiate(explosionCrater, impactPos + Vector3.up, Quaternion.Euler(90, Random(0, 360), 0));
		DOTween.To(() => colorLerp, a => impactExplodeSphere.Color = a, targetColor, explodeDuration);
		DOTween.To(() => impactExplodeSphere.Radius = 0, x => impactExplodeSphere.Radius = x, explodeRadius, explodeDuration).SetEase(Ease.OutCubic).OnComplete(() => {
			impactExplosionContainer.SetActive(false);
			impactExplodeSphere.Color = colorLerp;
			GoToNextState(1);
		});
	}

	public new void TakeDamage(IDamageEffector effector) {
		base.TakeDamage(effector);
	}

	public override void Revive() {
		base.Revive();
		pelletShadow.SetParent(transform);
		pellet.SetActive(false);
		impactIndication.gameObject.SetActive(false);
		impactExplosionContainer.SetActive(false);
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
