using Cysharp.Threading.Tasks;
using DG.Tweening;
using SimpleMan.Extensions;
using UnityEngine;

public class CamouflageTank : TankAI {

	[Header("Camouflage")]
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
		TurnOnCamouflage();
		Attack().Forget();
	}
	
	async UniTaskVoid Attack() {
		if (IsPlayReady == false) return;
		bool isFleeing = false;
		if(HasSightContactToPlayer) {
			if(await FleeFromAsync(Target.Pos, fleeRadius, fleeRadius * 0.9f) == false) {
				await RandomPathAsync(Pos, nextLocationRadius, nextLocationRadius * 0.5f, true);
			} else {
				isFleeing = true;
			}
		} else {
			await RandomPathAsync(Pos, nextLocationRadius, nextLocationRadius * 0.5f, true);
		}

		SetMovement(MovementType.MoveSmart);
		SetAiming(AimingMode.AimAtPlayerOnSight);
		await UniTask.Delay(100);
		while(IsPlayReady) {
			if(HasSightContactToPlayer && isFleeing == false || NextPathPointInReach) {
				break;
			}
			
			if(isFleeing && isPreparingShot == false && HasSightContactToPlayer && CanShoot && RequestAttack(3f)) {
				isPreparingShot = true;
				TurnOffCamouflage();
				this.Delay(2 + Random(0, randomReloadDuration), () => {
					isPreparingShot = false;
					if(HasSightContactToPlayer) {
						ShootBullet();
					}
					TurnOnCamouflage();
				});
			}
			await CheckPause();
		}
		Attack().Forget();
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
				}
			}
			this.Delay(camouflageSpeed, () => {
				isCamouflageTransitioning = false;
				IsHittable = false;
				//tankBody.gameObject.layer = GameMasks.Default;
				//tankHead.gameObject.layer = GameMasks.Default;
			});
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
				}
			}
			this.Delay(camouflageSpeed, () => {
				isCamouflageTransitioning = false;
				IsHittable = true;
				//tankBody.gameObject.layer = GameMasks.Bot;
				//tankHead.gameObject.layer = GameMasks.Bot;
			});
		}
	}
}
