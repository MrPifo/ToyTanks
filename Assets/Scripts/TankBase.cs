using UnityEngine;
using UnityEngine.VFX;
using CarterGames.Assets.AudioManager;
using SimpleMan.Extensions;
using Shapes;

[RequireComponent(typeof(Rigidbody))]
public class TankBase : MonoBehaviour {
	[Header("VALUES")]
	public int moveSpeed;
	public int healthPoints;
	public float bodyRotSpeed;
	public float aimRotSpeed;
	public float angleDiffLock;
	public float shootStunDuration;
	public float reloadDuration = 1f;
	public float trackSpawnDistance;
	public float destructionVelocity;
	public Bullet bullet;
	public LayerMask hitLayers;
	public bool makeInvincible;
	[Header("REFERENCES")]
	public Transform tankBody;
	public Transform tankHead;
	public Transform bulletOutput;
	public GameObject tankTrack;
	public GameObject muzzleFlash;
	public VisualEffect explodeVFX;
	public ParticleSystem smokeVFX;
	public Rectangle healthBar;
	Vector3 moveDir;
	public Vector3 Pos => rig.position;
	public bool CanShoot => !isReloading;
	public bool HasBeenDestroyed { get; set; }
	Rigidbody rig;
	Transform trackContainer;
	AudioManager Audio => LevelManager.audioManager;
	int muzzleFlashDelta;
	float angleDiff;
	Vector3 lastTrackPos;
	float engineVolume;
	int maxHealthPoints;
	bool isReloading;
	bool isShootStunned;

	protected virtual void Awake() {
		rig = GetComponent<Rigidbody>();
		trackContainer = GameObject.Find("TrackContainer").transform;
		maxHealthPoints = healthPoints;
		lastTrackPos = Pos;
		healthBar.Width = 2f;
		healthBar.transform.parent.gameObject.SetActive(false);
	}

	public void Move() => Move(new Vector3(transform.forward.x, 0, transform.forward.z));
	public void Move(Vector2 inputDir) {
		moveDir = new Vector3(inputDir.x, 0, inputDir.y);
		rig.velocity = Vector3.zero;
		if(Mathf.Abs(moveDir.x) >= 0.75f && Mathf.Abs(moveDir.z) >= 0.75f) {
			moveDir.x = Mathf.Clamp(moveDir.x, -0.75f, 0.75f);
			moveDir.z = Mathf.Clamp(moveDir.z, -0.75f, 0.75f);
		}
		AdjustRotation(moveDir);

		if(angleDiff < angleDiffLock && !isShootStunned) {
			var movePos = rig.transform.forward * moveSpeed * Time.deltaTime;
			rig.MovePosition(rig.position + movePos);
		}
		TrackTracer();
	}

	public void AdjustRotation(Vector3 moveDir) {
		var rot = Quaternion.LookRotation(moveDir, Vector3.up);
		angleDiff = Quaternion.Angle(rig.rotation, rot);
		if(angleDiff > 0) {
			rot = Quaternion.RotateTowards(rig.rotation, rot, bodyRotSpeed * Time.deltaTime);
			rig.MoveRotation(rot);
		}
	}

	public void MoveHead(Vector3 target) {
		target.y = tankHead.position.y;
		var rot = Quaternion.LookRotation((target - Pos).normalized, Vector3.up);
		rot = Quaternion.RotateTowards(tankHead.rotation, rot, Time.deltaTime * aimRotSpeed);
		tankHead.rotation = rot;
	}

	public Vector3 GetLookDirection(Vector3 lookTarget) => (new Vector3(lookTarget.x, 0, lookTarget.z) - new Vector3(Pos.x, 0, Pos.z)).normalized;

	void TrackTracer() {
		float distToLastTrack = Vector2.Distance(new Vector2(lastTrackPos.x, lastTrackPos.z), new Vector2(rig.position.x, rig.position.z));
		if(distToLastTrack > trackSpawnDistance) {
			lastTrackPos = SpawnTrack();
		}
	}

	Vector3 SpawnTrack() {
		Transform track = Instantiate(tankTrack, trackContainer).transform;
		track.position = new Vector3(rig.position.x, 0.025f, rig.position.z);
		track.rotation = rig.rotation * Quaternion.Euler(90, 0, 0);

		Audio.Play("TankDrive", 0.5f, Random.Range(1f, 1.1f));
		return track.position;
	}

	public void ShootBullet() {
		if(!isReloading) {
			Instantiate(bullet).SetupBullet(bulletOutput.forward, bulletOutput.position);
			
			if(reloadDuration > 0) {
				isReloading = true;
				this.Delay(reloadDuration, () => isReloading = false);
			}
			if(shootStunDuration > 0) {
				isShootStunned = true;
				this.Delay(shootStunDuration, () => isShootStunned = false);
			}
		}
	}

	public virtual void GotHitByBullet() {
		if(!HasBeenDestroyed && !makeInvincible) {
			if(healthPoints - 1 <= 0) {
				GotDestroyed();
			}
			healthPoints--;
			int width = maxHealthPoints == 0 ? 1 : healthPoints;
			healthBar.Width = 2f / maxHealthPoints * width;
		}
	}

	public void GotDestroyed() {
		healthPoints--;
		HasBeenDestroyed = true;
		explodeVFX.SendEvent("Play");
		new GameObject().AddComponent<DestructionTimer>().Destruct(5f, new GameObject[] { explodeVFX.gameObject });
		Destroy(healthBar.transform.parent.gameObject);
		tankHead.parent = null;
		tankBody.parent = null;
		var headRig = tankHead.gameObject.AddComponent<Rigidbody>();
		var bodyRig = tankBody.gameObject.AddComponent<Rigidbody>();
		headRig.gameObject.layer = LayerMask.NameToLayer("DestructionPieces");
		bodyRig.gameObject.layer = LayerMask.NameToLayer("DestructionPieces");
		var headVec = new Vector3(Random.Range(-destructionVelocity, destructionVelocity), destructionVelocity, Random.Range(-destructionVelocity, destructionVelocity));
		var bodyVec = new Vector3(Random.Range(-destructionVelocity, destructionVelocity), destructionVelocity, Random.Range(-destructionVelocity, destructionVelocity));
		headRig.AddForce(headVec);
		bodyRig.AddForce(bodyVec);
		headRig.AddTorque(headVec);
		bodyRig.AddTorque(bodyVec);
	}

	public void AimAssistant(Vector3 dir1) {
		/*RaycastHit FirstHit;
		RaycastHit SecondHit;
		Vector3 bulletOut = bulletOutput.transform.position;
		bulletOut.y -= 0.1f;
		int mask = LayerMask.GetMask("Default", "Destructable");

		if(Physics.Raycast(bulletOut, dir1, out FirstHit, assistantLinesRange, mask)) {
			Debug.DrawLine(bulletOut, FirstHit.point, Color.red);
			Vector3 dir2 = Vector3.Reflect(dir1, FirstHit.normal);
			AssistLine1.SetPosition(0, bulletOut);
			AssistLine1.SetPosition(1, FirstHit.point);
			AssistLine1.startWidth = assistantLinesWidth;
			AssistLine1.endWidth = assistantLinesWidth;
			mask = LayerMask.GetMask("Default");
			if(FirstHit.collider.tag == "destroyable") {
				return;
			}
			if(Physics.Raycast(FirstHit.point, dir2, out SecondHit, 9999, mask)) {
				Debug.DrawLine(FirstHit.point, SecondHit.point, Color.green);
				AssistLine2.SetPosition(0, FirstHit.point);
				AssistLine2.SetPosition(1, SecondHit.point);
				AssistLine2.startWidth = assistantLinesWidth;
				AssistLine2.endWidth = assistantLinesWidth;
			}
		}*/
	}
}
