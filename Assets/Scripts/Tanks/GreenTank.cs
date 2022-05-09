using Cysharp.Threading.Tasks;
using Sperlich.Debug.Draw;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Sperlich.Debug.Draw.Draw;

public class GreenTank : TankAI {

	[Header("Green")]
	public int sensorsAmount = 10;
	public int sensorBounces = 2;
	public bool extraDebug;
	float angleOffset;

	Ray lastFoundRay;
	List<(Vector3 from, Vector3 end)> validPath = new List<(Vector3, Vector3)>();
	protected static LayerMask BounceLayers = LayerMaskExtension.Create(GameMasks.Player, GameMasks.LevelBoundary, GameMasks.Block);


	public override void InitializeTank() {
		base.InitializeTank();
		lastFoundRay = new Ray(Pos + Vector3.up / 2f, (Target.Pos - Pos));
		lastFoundRay.origin = new Vector3(lastFoundRay.origin.x, 1, lastFoundRay.origin.z);
		Attack().Forget();
	}

	async UniTaskVoid Attack() {
		while(IsPlayReady) {
			if(HasSightContactToPlayer) {
				enableInaccurateAim = true;
				if(IsFacingTarget(Target.Pos) && CanShoot && !WouldFriendlyFire && RequestAttack(3f)) {
					ShootBullet();
				}
				SetAiming(AimingMode.AimAtPlayerOnSight);
			} else if (IsBouncePathValid() && CanShoot) {
				FindBouncePath();
				enableInaccurateAim = false;
				SetAiming(AimingMode.AimAtTarget);
				aimTarget = validPath[1].Item1;
				if (IsFacingTarget(validPath[0].Item2, 0.999f) && CanShoot && RequestAttack(3f)) {
					ShootBullet();
				}
			} else {
				if (IsBouncePathValid() == false) {
					angleOffset = Random(0, 360f);
					enableInaccurateAim = true;
				}
				SetAiming(AimingMode.None);
				FindBouncePath();
			}
			await CheckPause();
		}
	}

	public bool IsBouncePathValid() {
		if (validPath.Count > 1) {
			for (int i = 0; i < validPath.Count; i++) {
				if (Physics.Raycast(validPath[i].from, (validPath[i].end - validPath[i].from).normalized, out RaycastHit hit, Mathf.Infinity, HitLayers)) {
					if (hit.transform.CompareTag("Player")) {
						return true;
					}
					if (extraDebug) {
						Line(validPath[i].from, hit.point, Color.yellow);
					}
				}
			}
		}
		return false;
	}

	public bool FindBouncePath() {
		if(validPath != null && validPath.Count > 1 && false) {
			if(IsBouncePathValid()) {
				return true;
			}
		}

		(bool, List<(Vector3 from, Vector3 end)>) result = BounceSensors(bulletOutput.position, "Player", sensorBounces, sensorsAmount, angleOffset, extraDebug);
		if (result.Item1) {
			validPath = result.Item2;
			if (extraDebug) {
				for (int i = 0; i < result.Item2.Count; i++) {
					Line(result.Item2[i].from, result.Item2[i].end, 5, Color.cyan);
				}
			}
		}
		return result.Item1;
	}

	public static (bool, List<(Vector3 from, Vector3 end)>) BounceSensors(Vector3 origin, string hitTarget, int bounces, int amount, float angleOffset = 0, bool visualize = false) {
		var ray = new Ray(origin, Vector3.zero);
		Vector3[] dirs = GetDirs(amount, angleOffset);
		Dictionary<int, List<(Vector3 from, Vector3 end)>> rays = new Dictionary<int, List<(Vector3 from, Vector3 end)>>();

		for (int i = 0; i < amount; i++) {
			List<(Vector3 from, Vector3 end)> path = new List<(Vector3 from, Vector3 end)>();
			if(SendRay(origin, dirs[i], hitTarget, 1, bounces, path)) {
				return (true, path);
			}
			rays.Add(i, path);
		}
		if (visualize) {
			foreach (var r in rays.SelectMany(r => r.Value)) {
				Line(r.from, r.end, Color.black);
			}
		}
		return (false, new List<(Vector3 from, Vector3 end)>());

		bool SendRay(Vector3 startPos, Vector3 direction, string hitTarget, int iteration, int maxBounces, List<(Vector3 from, Vector3 end)> path) {
			if (Physics.Raycast(startPos, direction, out RaycastHit hit, Mathf.Infinity, BounceLayers)) {
				path.Add((startPos, hit.point));
				if(hit.transform.gameObject.CompareTag(hitTarget)) {
					return true;
				}
				if (iteration < maxBounces && Bullet.IsBulletBlocker(hit.transform) == false) {
					return SendRay(hit.point, Vector3.Reflect(direction, hit.normal), hitTarget, iteration + 1, maxBounces, path);
				}
			}
			return false;
		}
	}
}
