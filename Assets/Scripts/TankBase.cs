using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using CarterGames.Assets.AudioManager;
using SimpleMan.Extensions;

[RequireComponent(typeof(Rigidbody))]
public class TankBase : MonoBehaviour {
	[Header("VALUES")]
	public float moveSpeed;
	public float bodyRotSpeed;
	public float aimRotSpeed;
	public float angleDiffLock;
	public float shootStun;
	public float shootCooldown;
	public float trackSpawnDistance;
	public Bullet bullet;
	public LayerMask hitLayers;
	public bool makeInvincible;
	[Header("REFERENCES")]
	public Transform tankHead;
	public Transform bulletOutput;
	public GameObject tankTrack;
	public GameObject muzzleFlash;
	public VisualEffect explodeVFX;
	public ParticleSystem smokeVFX;
	Vector3 moveDir;
	public Vector3 Pos => rig.position;
	public bool CanShoot => !onShootCooldown;
	public bool HasBeenDestroyed { get; set; }
	Rigidbody rig;
	Transform trackContainer;
	AudioManager Audio => LevelManager.audioManager;
	int muzzleFlashDelta;
	float angleDiff;
	Vector3 lastTrackPos;
	float engineVolume;
	bool onShootCooldown;
	bool onShootMoveCooldown;

	protected virtual void Awake() {
		rig = GetComponent<Rigidbody>();
		trackContainer = GameObject.Find("TrackContainer").transform;
		lastTrackPos = Pos;
	}

	public void Move(Vector2 inputDir) {
		moveDir = new Vector3(inputDir.x, 0, inputDir.y);
		rig.velocity = Vector3.zero;
		if(Mathf.Abs(moveDir.x) >= 0.75f && Mathf.Abs(moveDir.z) >= 0.75f) {
			moveDir.x = Mathf.Clamp(moveDir.x, -0.75f, 0.75f);
			moveDir.z = Mathf.Clamp(moveDir.z, -0.75f, 0.75f);
		}
		AdjustRotation(moveDir);

		if(angleDiff < angleDiffLock && !onShootMoveCooldown) {
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
		Debug.DrawRay(rig.position, moveDir, Color.green);
		Debug.DrawRay(rig.position, rig.transform.forward, Color.blue);
	}

	public void MoveHead(Vector3 target) {
		target.y = tankHead.position.y;
		var rot = Quaternion.LookRotation((target - Pos).normalized, Vector3.up);
		rot = Quaternion.RotateTowards(tankHead.rotation, rot, Time.deltaTime * aimRotSpeed);
		tankHead.rotation = rot;
	}

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
		if(!onShootCooldown) {
			Instantiate(bullet).SetupBullet(bulletOutput.forward, bulletOutput.position);
			onShootCooldown = true;
			onShootMoveCooldown = true;
			this.Delay(shootCooldown, () => { onShootCooldown = false; onShootMoveCooldown = false; });
		}
	}

	public virtual void GotHitByBullet() {
		if(!makeInvincible) {
			HasBeenDestroyed = true;
			explodeVFX.SendEvent("Play");
			new GameObject().AddComponent<DestructionTimer>().Destruct(5f, new GameObject[] { explodeVFX.gameObject });
			gameObject.SetActive(false);
		}
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
