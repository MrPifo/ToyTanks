using Sperlich.Debug.Draw;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreenTank : TankAI {

	public int predictionIterations = 10;
	bool pathFound;
	List<(Ray, RaycastHit)> validPath = new List<(Ray, RaycastHit)>();
	Ray lastFoundRay;

	protected override void Awake() {
		base.Awake();
	}

	void Start() {
		lastFoundRay = new Ray(Pos + Vector3.up / 2f, (player.Pos - Pos));
	}

	public override void Attack() {
		if(!IsPlayerInDetectRadius) {
			stateMachine.Push(TankState.Idle);
			return;
		}
		if(HasSightContact(player)) {
			if(IsAimingOnTarget(player.transform) && CanShoot && !WouldFriendlyFire) {
				ShootBullet();
			}
			Aim();
		} else if(IsBouncePathValid()) {
			MoveHead(validPath[1].Item1.origin);
			if(IsAimingOnTarget(validPath[0].Item2.point, 0.97f) && CanShoot && !WouldFriendlyFire) {
				ShootBullet();
			}
		} else if(!IsBouncePathValid()) {
			stateMachine.Push(TankState.Idle);
		}
	}

	public override void Defense() {
		
	}

	public override void Idle() {
		if(IsPlayerInDetectRadius) {
			if(HasSightContactToPlayer) {
				stateMachine.Push(TankState.Attack);
			} else if(FindBouncePath()) {
				stateMachine.Push(TankState.Attack);
			}
		}
	}

	public override void Patrol() {
		
	}

	public bool IsBouncePathValid() {
		if(validPath.Count > 1) {
			if(Physics.Raycast(validPath[1].Item1, out RaycastHit hit, Mathf.Infinity, hitLayers)) {
				if(hit.transform.CompareTag("Player")) {
					if(showDebug) {
						Draw.Line(validPath[1].Item1.origin, hit.point, Color.yellow);
					}
					return true;
				}
			}
			Draw.Ray(validPath[1].Item1.origin, validPath[1].Item1.direction, Color.magenta);
		}
		return false;
	}

	public bool FindBouncePath() {
		if(pathFound) {
			if(showDebug) {
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
				var success1 = Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, hitLayers);
				var ray2 = new Ray(hit.point, Vector3.Reflect(ray.direction, hit.normal));
				var sucess2 = Physics.Raycast(ray2, out RaycastHit hit2, Mathf.Infinity, hitLayers);
				if(success1 && sucess2 && hit2.transform.CompareTag("Player")) {
					validPath = new List<(Ray, RaycastHit)> {
					(ray, hit),
					(ray2, hit2)
					};
					lastFoundRay = ray;
					return true;
				}
				if(showDebug) {
					Draw.Line(Pos, hit.point, Color.cyan);
					Draw.Line(hit.point, hit2.point, Color.blue);
				}
			}
		}
		return false;
		// Random Soltion
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
				ray.direction = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
			}
			if(showDebug) {
				Draw.Line(Pos, hit.point, Color.cyan);
				Draw.Line(hit.point, hit2.point, Color.blue);
			}
		}
		return false;
	}
}
