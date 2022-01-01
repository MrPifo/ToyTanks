using DG.Tweening;
using SimpleMan.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamouflageTank : TankAI {

	public float nextLocationRadius = 10;
	public float fleeRadius = 20;
	public float camouflageSpeed = 1;
	bool isCamouflageTransitioning;
	bool isPreparingShot = false;

	protected override void Awake() {
		base.Awake();
	}

	public override void InitializeTank() {
		base.InitializeTank();
		ProcessState(TankState.Move);
		this.Delay(2, () => {
			TurnOnCamouflage();
		});
	}

	protected override IEnumerator IMove() {
		bool isFleeing = false;

		if(HasSightContactToPlayer) {
			if(FleeFrom(Player.Pos, fleeRadius, fleeRadius * 0.9f) == false) {
				RandomPath(Pos, nextLocationRadius, nextLocationRadius * 0.5f, true);
			} else {
				isFleeing = true;
			}
		} else {
			RandomPath(Pos, nextLocationRadius, nextLocationRadius * 0.5f, true);
		}

		MoveMode.Push(MovementType.MoveSmart);
		HeadMode.Push(TankHeadMode.AimAtPlayerOnSight);
		yield return null;
		while(IsPlayReady) {
			if(HasSightContactToPlayer && isFleeing == false) {
				break;
			}
			
			if(isFleeing && isPreparingShot == false && HasSightContactToPlayer && CanShoot) {
				isPreparingShot = true;
				TurnOffCamouflage();
				this.Delay(Random(reloadDuration, reloadDuration + randomReloadDuration), () => {
					isPreparingShot = false;
					if(HasSightContactToPlayer) {
						ShootBullet();
					}
					TurnOnCamouflage();
				});
			}
			if(HasReachedDestination) {
				break;
			}
			yield return IPauseTank();
		}

		ProcessState(TankState.Move);
	}

	private void TurnOnCamouflage() {
		if(isCamouflageTransitioning == false) {
			isCamouflageTransitioning = true;
			disableTracks = true;
			foreach(var t in destroyTransformPieces) {
				foreach(var mat in t.GetComponent<MeshRenderer>().materials) {
					mat.SetFloat("Camouflage", 0);
					mat.SetFloat("Transparency", 1);
					DOTween.To(() => mat.GetFloat("Camouflage"), x => mat.SetFloat("Camouflage", x), 1, camouflageSpeed);
					DOTween.To(() => mat.GetFloat("Transparency"), x => mat.SetFloat("Transparency", x), 0.1f, camouflageSpeed);
					DOTween.To(() => FakeShadow.fadeFactor, x => FakeShadow.fadeFactor = x, 0, camouflageSpeed);
					this.Delay(camouflageSpeed, () => isCamouflageTransitioning = false);
				}
			}
		}
	}

	private void TurnOffCamouflage() {
		if(isCamouflageTransitioning == false) {
			isCamouflageTransitioning = true;
			disableTracks = false;
			foreach(var t in destroyTransformPieces) {
				foreach(var mat in t.GetComponent<MeshRenderer>().materials) {
					mat.SetFloat("Camouflage", 1);
					mat.SetFloat("Transparency", 0);
					DOTween.To(() => mat.GetFloat("Camouflage"), x => mat.SetFloat("Camouflage", x), 0, camouflageSpeed);
					DOTween.To(() => mat.GetFloat("Transparency"), x => mat.SetFloat("Transparency", x), 1f, camouflageSpeed);
					DOTween.To(() => FakeShadow.fadeFactor, x => FakeShadow.fadeFactor = x, 1, camouflageSpeed);
					this.Delay(camouflageSpeed, () => isCamouflageTransitioning = false);
				}
			}
		}
	}
}
