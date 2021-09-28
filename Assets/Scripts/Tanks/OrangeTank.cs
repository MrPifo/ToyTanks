using Shapes;
using SimpleMan.Extensions;
using Sperlich.FSM;
using System.Collections;
using System.Collections.Generic;
using ToyTanks.LevelEditor;
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
	public GameObject impactExplosionContainer;
	public GameObject explosionCrater;
	public Sphere sphere;
	public ParticleSystem explodeFire;
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
		float time = 0;
		Vector3 startPos = pellet.transform.position;
		Vector3 impactPos = Player.Pos;
		impactIndication.transform.position = impactPos;
		impactIndication.gameObject.SetActive(true);
		impactExplosionContainer.transform.position = impactPos;
		pellet.SetActive(true);
		while(time < flyTime) {
			float y = flyCurve.Evaluate(time.Remap(0, flyTime, 0, 1f));
			var pos = Vector3.Lerp(startPos, impactPos, time.Remap(0, flyTime, 0, 1f));
			pos.y = y;
			pellet.transform.position = pos;
			time += Time.deltaTime;
			yield return null;
		}
		// Check if Hit
		foreach(var hit in Physics.OverlapSphere(pellet.transform.position, pellet.GetComponent<SphereCollider>().radius)) {
			if(hit.transform.parent != null && hit.transform.parent.TryGetComponent(out TankBase tank)) {
				tank.GotHitByBullet();
			}
		}

		pellet.SetActive(false);
		impactIndication.gameObject.SetActive(false);
		impactExplosionContainer.SetActive(true);
		pellet.transform.position = Pos;
		Color colorLerp = sphere.Color;
		Color targetColor = colorLerp;
		targetColor.a = 0;
		LevelManager.Feedback.TankExplode();
		explodeFire.Play();
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
