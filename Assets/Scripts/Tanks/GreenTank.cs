using SimpleMan.Extensions;
using Sperlich.Debug.Draw;
using Sperlich.FSM;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreenTank : TankAI {

	public int predictionIterations = 10;
	public bool advancedDebug;
	bool pathFound;
	List<(Ray, RaycastHit)> validPath = new List<(Ray, RaycastHit)>();
	Ray lastFoundRay;

	public override void InitializeTank() {
		base.InitializeTank();
		lastFoundRay = new Ray(Pos + Vector3.up / 2f, (Player.Pos - Pos));
		lastFoundRay.origin = new Vector3(lastFoundRay.origin.x, 1, lastFoundRay.origin.z);
		ProcessState(TankState.Attack);
	}

	protected override IEnumerator IAttack() {
		while(IsPlayReady) {
			if(IsPlayerInDetectRadius) {
				if(HasSightContact(Player)) {
					if(IsFacingTarget(Player.transform) && CanShoot && !WouldFriendlyFire) {
						ShootBullet();
					}
					AimAtPlayer();
				} else if(IsBouncePathValid()) {
					MoveHead(validPath[1].Item1.origin);
					if(IsFacingTarget(validPath[0].Item2.point, 0.97f) && CanShoot && !WouldFriendlyFire) {
						ShootBullet();
					}
				} else {
					FindBouncePath();
				}
			}
			yield return null;
			while(IsPaused) yield return null;   // Pause AI
		}
	}

	public bool IsBouncePathValid() {
		if(validPath.Count > 1) {
			if(Physics.Raycast(validPath[1].Item1, out RaycastHit hit, Mathf.Infinity, HitLayers)) {
				if(hit.transform.CompareTag("Player")) {
					if(advancedDebug) {
						Draw.Line(validPath[1].Item1.origin, hit.point, Color.yellow);
					}
					return true;
				}
			}
		}
		return false;
	}

	public bool FindBouncePath() {
		if(pathFound) {
			if(advancedDebug) {
				Draw.Line(validPath[0].Item1.origin, validPath[0].Item2.point, Color.red);
				Draw.Line(validPath[1].Item1.origin, validPath[1].Item2.point, Color.red);
			}
			if(IsBouncePathValid()) {
				return true;
			}
		}
		var ray = lastFoundRay;
		pathFound = false;

		for(int x = -predictionIterations; x < predictionIterations && !pathFound; x++) {
			for(int y = -predictionIterations; y < predictionIterations && !pathFound; y++) {
				ray.direction = new Vector3((float)x/predictionIterations, 0, (float)y/predictionIterations);
				var success1 = Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, HitLayers);
				var ray2 = new Ray(hit.point, Vector3.Reflect(ray.direction, hit.normal));
				var sucess2 = Physics.Raycast(ray2, out RaycastHit hit2, Mathf.Infinity, HitLayers);
				if(success1 && sucess2 && hit2.transform.CompareTag("Player")) {
					validPath = new List<(Ray, RaycastHit)> {
					(ray, hit),
					(ray2, hit2)
					};
					lastFoundRay = ray;
					return true;
				}
				if(advancedDebug) {
					Draw.Line(Pos, hit.point, Color.cyan);
					Draw.Line(hit.point, hit2.point, Color.blue);
				}
			}
		}
		return false;
		/* Random Soltion
		for(int i = 0; i < predictionIterations && !pathFound; i++) {
			Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, hitLayers);
			var ray2 = new Ray(hit.point, Vector3.Reflect(ray.direction, hit.normal));
			Physics.Raycast(ray2, out RaycastHit hit2, Mathf.Infinity, hitLayers);

			if(hit2.transform.CompareTag("Player")) {
				validPath = new List<(Ray, RaycastHit)> {
					(ray, hit),
					(ray2, hit2)
				};
				lastFoundRay = ray;
				return true;
			} else {
				ray.direction = new Vector3(Random(-1f, 1f), 0, Random(-1f, 1f));
			}
			if(advancedDebug) {
				Draw.Line(Pos, hit.point, Color.cyan);
				Draw.Line(hit.point, hit2.point, Color.blue);
			}
		}
		return false;*/
	}

	protected override void DrawDebug() {
		base.DrawDebug();
	}
}
