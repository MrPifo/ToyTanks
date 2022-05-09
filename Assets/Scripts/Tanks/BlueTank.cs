using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class BlueTank : TankAI {

	[Header("Blue")]
	public float bulletCurveDelay = 0.5f;
	public float bulletCurveSpeed = 0.5f;
	public float bulletCurveVelocityMultiplier = 2f;
	public float minDistance = 5;
	public int scanDistanceAmount = 8;
	public AnimationCurve bulletCurve;
	Vector3 playersLastPos = Vector3.zero;

	public override void InitializeTank() {
		base.InitializeTank();
	}

	/*protected override async void AttackST() {
		await TaskEx.WaitUntil(() => CanShoot);
		// Scan for possible direction in a circle
		List<Vector3> possibleDirs = new List<Vector3>();
		for(int x = -scanDistanceAmount; x < scanDistanceAmount; x++) {
			for(int y = -scanDistanceAmount; y < scanDistanceAmount; y++) {
				if(Physics.Raycast(bulletOutput.position, new Vector3((float)x / scanDistanceAmount, 0, (float)y / scanDistanceAmount), out RaycastHit hit, Mathf.Infinity, HitLayers)) {
					possibleDirs.Add(hit.point);
				}
			}
		}

		float isAlignedToPlayer = 0;
		// Filter directions out that are too nearby
		List<Vector3> possibleDirsFiltered = possibleDirs.Where(d => Vector3.Distance(d, Pos) > minDistance).ToList();
		Vector3 targetHitPos;
		// Filter and choose flying path
		if(possibleDirsFiltered.Count <= 0) {
			// If no direction matches the filtered, a random direction is chosen
			targetHitPos = possibleDirs.RandomItem();
		} else {
			// 80/20 for random filtered or remembered last position of player
			if(playersLastPos == Vector3.zero || Random(0f, 1f) >= 0.8f) {
				targetHitPos = possibleDirsFiltered.RandomItem();
			} else {
				targetHitPos = playersLastPos;
			}
		}

		// Choose Bullets fly path
		if(HasSightContactToPlayer == false) {
			// Shoot random defined direction
			while(isAlignedToPlayer < 0.98f && IsPlayReady) {
				MoveHead(targetHitPos);
				isAlignedToPlayer = Vector3.Dot((targetHitPos - bulletOutput.position).normalized, bulletOutput.forward);
				await CheckPause();
			}
		} else {
			// Shoot at player if visible
			while(IsAimingAtPlayer == false) {
				AimAtPlayer();
				await CheckPause();
			}
		}

		Bullet bullet = ShootBullet();
		bool bulletAlive = true;
		bullet.OnBulletDestroyed.AddListener(() => bulletAlive = false);

		// Check if bullet has sight contact to player
		while(IsPlayReady && bulletAlive) {
			if(Physics.Linecast(bullet.Pos, Player.Pos, out RaycastHit hit, HitLayers)) {
				if(hit.transform.CompareTag("Player")) {
					break;
				}
			}
			await CheckPause();
		}

		await Task.Delay(Mathf.RoundToInt(bulletCurveDelay * 1000));
		if(bulletAlive) {
			// Redirect Bullet to Player
			float time = 0;
			Vector3 baseDir = bullet.Direction;
			Vector3 targetDir = (Player.Pos - bullet.Pos).normalized;
			float baseVelocity = bullet.velocity;
			float targetVelocity = bullet.velocity * bulletCurveVelocityMultiplier;
			bullet.BurstParticles.Play();
			AudioPlayer.Play("TrackerBulletBurst", AudioType.Default, 1f, 1.2f, 2f);
			// Lerp the bullet to the new path
			while(IsPlayReady && time < bulletCurveSpeed) {
				// Bullets can have a maximal turn rate of 90 degree
				if(Vector3.Angle(baseDir, bullet.Direction) > 90) {
					bullet.Direction = Vector3.RotateTowards(baseDir, bullet.Direction, 90 * Mathf.Deg2Rad, 1f);
					break;
				}
				time += Time.deltaTime;
				float mapValue = bulletCurve.Evaluate(time.Remap(0, bulletCurveSpeed, 0, 1));
				bullet.velocity = Mathf.Lerp(baseVelocity, targetVelocity, mapValue);
				bullet.Direction = Vector3.Lerp(baseDir, targetDir, mapValue);
				await CheckPause();
			}
		}
	}

	protected override void LateUpdate() {
		base.LateUpdate();
		if(IsPlayReady) {
			// Let's the tank remember the players last seen position
			if(HasSightContactToPlayer) {
				playersLastPos = Player.Pos;
			}
		}
	}*/
}
