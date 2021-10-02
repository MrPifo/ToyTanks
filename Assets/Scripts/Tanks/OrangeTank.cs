using Shapes;
using SimpleMan.Extensions;
using Sperlich.FSM;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class OrangeTank : TankAI {

	public enum OrangeState { Waiting, Attack }
	protected FSM<OrangeState> stateMachine = new FSM<OrangeState>();
	public AnimationCurve flyCurve;
	public GameObject projectile;
	public Disc impactIndication;
	public float flyTime;
	GameObject pellet;
	[Header("Impact Explosion Parts")]
	public float explodeDuration = 1f;
	public GameObject mortar;
	public GameObject impactExplosionContainer;
	public GameObject explosionCrater;
	public Sphere sphere;
	public ParticleSystem impactExplodeFire;
	public ParticleSystem shotExplodeFire;
	float explodeRadius = 0;

	protected override void Awake() {
		base.Awake();
		
		impactIndication.gameObject.SetActive(false);
		pellet = Instantiate(projectile, Pos + Vector3.up * 2, Quaternion.identity);
		explodeRadius = projectile.GetComponent<SphereCollider>().radius;
		impactIndication.gameObject.GetComponent<Disc>().Radius = explodeRadius;
		pellet.SetActive(false);
		impactExplosionContainer.SetActive(false);
	}

	public override void InitializeTank() {
		base.InitializeTank();
		GoToNextState();
	}

	IEnumerator Attack() {
		//var blobShadow = pellet.transform.GetChild(1);
		yield return new WaitForSeconds(reloadDuration + Random(0, randomReloadDuration));
		float time = 0;
		Vector3 startPos = pellet.transform.position;
		Vector3 impactPos = Player.Pos;
		impactPos.y = 0.05f;
		bool allowShot = false;
		impactIndication.gameObject.SetActive(true);
		transform.DORotate(Quaternion.LookRotation((impactPos - Pos).normalized).eulerAngles, 0.2f).OnComplete(() => {
			allowShot = true;
		});
		while(allowShot == false) {
			impactExplosionContainer.transform.position = impactPos;
			impactIndication.transform.position = impactPos;
			yield return null;
		}

		yield return new WaitUntil(() => allowShot);
		pellet.SetActive(true);
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
			yield return null;
			while(LevelManager.GamePaused) yield return null;	// Pause AI
		}
		// Check if Hit
		foreach(var hit in Physics.OverlapSphere(pellet.transform.position, pellet.GetComponent<SphereCollider>().radius)) {
			if(hit.transform.parent != null && hit.transform.parent.TryGetComponent(out TankBase tank)) {
				tank.GotHitByBullet();
			}
		}

		AudioPlayer.Play("SmallExplosion", 0.9f, 1.3f, 1f);
		pellet.SetActive(false);
		impactIndication.gameObject.SetActive(false);
		impactExplosionContainer.SetActive(true);
		pellet.transform.position = Pos;
		Color colorLerp = sphere.Color;
		Color targetColor = colorLerp;
		targetColor.a = 0;
		LevelManager.Feedback?.TankExplode();
		impactExplodeFire.Play();
		Instantiate(explosionCrater, impactPos + Vector3.up, Quaternion.Euler(90, Random(0, 360), 0));
		DOTween.To(() => colorLerp, a => sphere.Color = a, targetColor, explodeDuration);
		DOTween.To(() => sphere.Radius = 0, x => sphere.Radius = x, explodeRadius, explodeDuration).SetEase(Ease.OutCubic).OnComplete(() => {
			impactExplosionContainer.SetActive(false);
			sphere.Color = colorLerp;
			GoToNextState(1);
		});
	}

	protected override void GoToNextState(float delay = 0.01f) {
		if(IsAIEnabled && HasBeenInitialized) {
			this.Delay(delay, () => {
				stateMachine.Push(OrangeState.Waiting);
				while(stateMachine == OrangeState.Waiting) {
					stateMachine.Push(stateMachine.GetRandom());
				}
				ProcessState();
			});
		}
	}

	protected override void ProcessState() {
		if(HasBeenDestroyed == false) {
			switch(stateMachine.State) {
				case OrangeState.Attack:
					StartCoroutine(Attack());
					break;
			}
		}
	}
}
