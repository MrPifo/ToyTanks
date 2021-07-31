using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.VFX;

[RequireComponent(typeof(Rigidbody))]
public class TankBase : MonoBehaviour {
	[Header("VALUES")]
	public float moveSpeed;
	public float bodyRotSpeed;
	public float aimRotSpeed;
	public float angleDiffLock;
	public float shootStun;
	public float shootCooldown;
	public Bullet bullet;
	public Vector3 maxMoveAngle;
	[Header("REFERENCES")]
	public GameObject TankHead;
	public Transform bulletOutput;
	public GameObject tankTrack;
	public GameObject muzzleFlash;
	public VisualEffect explodeVFX;
	public ParticleSystem smokeVFX;
	public AudioSource driveSound;
	public Vector3 moveDir;
	Rigidbody rig;
	int muzzleFlashDelta;
	float angleDiff;
	public float engineVolume;

	void Awake() {
		rig = GetComponent<Rigidbody>();
	}

	public void Move(Vector2 inputDir) {
		moveDir = new Vector3(inputDir.x, 0, inputDir.y);
		if(Mathf.Abs(moveDir.x) >= 0.75f && Mathf.Abs(moveDir.z) >= 0.75f) {
			moveDir.x = Mathf.Clamp(moveDir.x, -0.75f, 0.75f);
			moveDir.z = Mathf.Clamp(moveDir.z, -0.75f, 0.75f);
		}
		AdjustRotation(moveDir);

		if(angleDiff < angleDiffLock) {
			var movePos = moveDir * moveSpeed * Time.fixedDeltaTime;
			rig.MovePosition(rig.position + movePos);
		}
	}

	public void AdjustRotation(Vector3 moveDir) {
		var rot = Quaternion.LookRotation(moveDir, Vector3.up);
		angleDiff = Quaternion.Angle(rig.rotation, rot);
		if(angleDiff > 0) {
			rot = Quaternion.RotateTowards(rig.rotation, rot, bodyRotSpeed * Time.fixedDeltaTime);
			rig.MoveRotation(rot);
		}
		Debug.DrawRay(rig.position, moveDir, Color.green);
		Debug.DrawRay(rig.position, rig.transform.forward, Color.blue);
	}

	void ShootBullet() {
		
	}

	public void GotHitByBullet() {
		
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
