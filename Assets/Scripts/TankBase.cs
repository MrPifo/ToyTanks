using UnityEngine;
using UnityEngine.VFX;
using CarterGames.Assets.AudioManager;
using SimpleMan.Extensions;
using Shapes;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MMFeedbacks))]
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
	public Transform billboardHolder;
	public Rectangle healthBar;
	[Header("Explosion Effects")]
	public GameObject destroyFlash;
	public ParticleSystem sparkDestroyEffect;
	public ParticleSystem smokeDestroyEffect;
	public ParticleSystem smokeFireDestroyEffect;
	public ParticleSystem damageSmokeBody;
	public ParticleSystem damageSmokeHead;
	public Disc shockwaveDisc;
	Vector3 moveDir;
	public Vector3 Pos => rig.position;
	public bool CanShoot => !isReloading;
	public bool HasBeenDestroyed { get; set; }
	Rigidbody rig;
	MMFeedbacks feedback;
	MMWiggle headWiggle;
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
		feedback = GetComponent<MMFeedbacks>();
		headWiggle = tankHead.GetComponent<MMWiggle>();
		trackContainer = GameObject.Find("TrackContainer").transform;
		maxHealthPoints = healthPoints;
		lastTrackPos = Pos;
		healthBar.Width = 2f;
		healthBar.transform.parent.gameObject.SetActive(false);
		shockwaveDisc.gameObject.SetActive(false);
	}

	protected virtual void LateUpdate() {
		billboardHolder.rotation = Quaternion.LookRotation((Pos - Camera.main.transform.position).normalized, Vector3.up);
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
			var bounceDir = -tankHead.right * 2f;
			bounceDir.y = 0;
			headWiggle.PositionWiggleProperties.AmplitudeMin = bounceDir;
			headWiggle.PositionWiggleProperties.AmplitudeMax = bounceDir;
			feedback.PlayFeedbacks();
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
		tankHead.parent = null;
		tankBody.parent = null;
		healthBar.gameObject.SetActive(false);
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
		FindObjectOfType<LevelManager>().TankDestroyedCheck();
		PlayDestroyExplosion();
	}

	public void PlayDestroyExplosion() {
		sparkDestroyEffect.Play();
		smokeDestroyEffect.Play();
		damageSmokeBody.Play();
		damageSmokeHead.Play();
		smokeFireDestroyEffect.Play();
		LevelManager.Feedback.PlayTankExplode();
		shockwaveDisc.gameObject.SetActive(true);
		
		StartCoroutine(IDestroyAnimate());
	}

	IEnumerator IDestroyAnimate() {
		float time = 0f;
		float thickness = shockwaveDisc.Thickness;
		destroyFlash.SetActive(true);
		var rend = destroyFlash.GetComponent<MeshRenderer>();
		var flashMat = new Material(rend.sharedMaterial);
		rend.sharedMaterial = flashMat;
		flashMat.SetColor("_BaseColor", Color.white);
		while(time < 1f) {
			shockwaveDisc.Radius = MMTween.Tween(time, 0, 1f, 0, 10, MMTween.MMTweenCurve.EaseOutCubic);
			shockwaveDisc.Thickness = MMTween.Tween(time, 0, 1f, thickness, 0, MMTween.MMTweenCurve.EaseOutCubic);
			float flashScale = MMTween.Tween(time, 0f, 0.3f, 0f, 1f, MMTween.MMTweenCurve.EaseOutCubic);
			float flashAlpha = MMTween.Tween(time, 0f, 0.3f, 1f, 0f, MMTween.MMTweenCurve.EaseOutCubic);
			flashMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, flashAlpha));
			if(time >= 0.3f) {
				destroyFlash.SetActive(false);
			} else {
				destroyFlash.transform.localScale = new Vector3(flashScale, flashScale, flashScale);
			}
			time += Time.deltaTime;
			yield return null;
		}
		shockwaveDisc.Thickness = thickness;
		shockwaveDisc.Radius = 0f;
		shockwaveDisc.gameObject.SetActive(false);
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

#if UNITY_EDITOR
[CustomEditor(typeof(PlayerInput))]
public class TankBaseDebugEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		PlayerInput builder = (PlayerInput)target;
		if(GUILayout.Button("Play Destroy")) {
			builder.PlayDestroyExplosion();
		}
	}
}
#endif