using UnityEngine;
using Shapes;
using Rewired;
using DG.Tweening;
using ToyTanks.LevelEditor;
using Rewired.ComponentControls;
using UnityEngine.EventSystems;

public class PlayerInput : TankBase, IHittable, IResettable {

	public Player player;
	public Material crossHairMat;
	public GameObject mobileAimHelper;
	public PlayerControlSchemes playerControlScheme = PlayerControlSchemes.Desktop;
	public Line mobileAimLine;
	public Line mobileAimLineRight;
	public Line mobileAimLineLeft;
	TouchPad touchPad;
	TouchRegion touchRegion;
	TouchJoystick aimJoystick;
	public float mobileAimHelperLineDistance = 5;
	GameObject _crossHair;
	Line crossLine;
	public GameObject Crosshair {
		get {
			if(_crossHair == null) {
				if(GameObject.FindGameObjectWithTag("CrossHair") == null) {
					_crossHair = Instantiate(Resources.Load<GameObject>("CrossHair"));
				} else {
					_crossHair = GameObject.FindGameObjectWithTag("CrossHair");
				}
				crossLine = _crossHair.transform.GetChild(0).GetComponent<Line>();
			}
			return _crossHair;
		}
	}
	public float crossHairMaxAppearanceDistance = 5f;
	public float crossHairSize = 1.5f;
	public bool disableControl;
	public bool IgnoreDisables;
	public bool touchShootRegionPressed;
	public float crossHairRotSpeed;
	public Vector2 mouseLocation;
	Vector3 oldAimVector;

	protected override void Awake() {
		base.Awake();
		player = ReInput.players.GetPlayer(0);
		mobileAimLine.gameObject.SetActive(false);
	}

	public override void InitializeTank() {
		base.InitializeTank();
		playerControlScheme = Game.PlayerControlScheme;
		makeInvincible = Game.isPlayerGodMode;
		SetupControls();
		SetupCross();
	}

	public void SetupControls() {
		if(Game.Platform == GamePlatform.Mobile) {
			touchRegion = GameObject.FindGameObjectWithTag("ShootTouchRegion")?.GetComponent<TouchRegion>();
			aimJoystick = GameObject.FindGameObjectWithTag("AimJoystick")?.GetComponent<TouchJoystick>();
			touchPad = GameObject.FindGameObjectWithTag("TouchPad")?.GetComponent<TouchPad>();
			PlayerInputManager.SetPlayerControlScheme(playerControlScheme);

			switch(playerControlScheme) {
				case PlayerControlSchemes.Desktop:
					PlayerInputManager.Instance.touchController.Hide();
					mobileAimLine.gameObject.SetActive(false);
					EnableCrossHair();

					break;
				case PlayerControlSchemes.DoubleDPad:
					PlayerInputManager.Instance.DoubleDPadUI.gameObject.Show();
					PlayerInputManager.Instance.touchController.Show();
					mobileAimLine.gameObject.SetActive(true);
					DisableCrossHair();
					touchRegion.InteractionStateChangedToPressed += Shoot;
					aimJoystick.TapEvent += Shoot;

					break;
				case PlayerControlSchemes.DpadAndTap:
					PlayerInputManager.Instance.DPadTapUI.gameObject.Show();
					PlayerInputManager.Instance.touchController.Show();
					mobileAimLine.gameObject.SetActive(false);
					EnableCrossHair();
					touchPad.TapEvent += MobileInputDPadAndTapUpdate;

					break;
				case PlayerControlSchemes.DoubleDPadAimAssistant:
					PlayerInputManager.Instance.touchController.Show();
					mobileAimLine.gameObject.SetActive(true);
					DisableCrossHair();
					aimJoystick.TapEvent += Shoot;

					break;
			}
		}
	}

	public void SetupCross() {
		Crosshair.gameObject.SetActive(true);
		crossLine.gameObject.SetActive(true);
		Crosshair.transform.position = Vector3.zero;
		crossHairMat.SetFloat("_RotAngle", 0);
		Crosshair.transform.rotation = Quaternion.identity;
	}

	protected void Update() {
		if(HasBeenInitialized && IsPaused == false || IgnoreDisables) {
			if(!disableControl && Crosshair != null || IgnoreDisables) {
				PlayerControl();
			}
		}
	}

	public override void Move(Vector2 inputDir) {
		moveDir = new Vector3(inputDir.x, 0, inputDir.y);
		rig.velocity = Vector3.zero;
		float dirFactor = Mathf.Sign(Vector3.Dot(moveDir.normalized, rig.transform.forward));
		if(disable2DirectionMovement) {
			dirFactor = 1;
		}
		RotateTank(moveDir * dirFactor);

		float maxDir = Mathf.Max(Mathf.Abs(inputDir.x), Mathf.Abs(inputDir.y));
		if(maxDir > 0.7f) {
			maxDir = 1;
		}
		dirFactor *= maxDir;
		var movePos = dirFactor * moveSpeed * Time.deltaTime * rig.transform.forward;
		if(!isShootStunned && canMove) {
			rig.MovePosition(rig.position + movePos);
		}
		TrackTracer(dirFactor);
	}

	void PlayerControl() {
		switch(playerControlScheme) {
			case PlayerControlSchemes.Desktop:
				DesktopInput();
				break;
			case PlayerControlSchemes.DoubleDPad:
				MobileInputDoubleDPad();
				break;
			case PlayerControlSchemes.DpadAndTap:
				MobileInputDPadAndTap();
				break;
			case PlayerControlSchemes.DoubleDPadAimAssistant:
				MobileInputDPadAimAssist();
				break;
		}
	}

	void DesktopInput() {
		Vector2 moveVector = new Vector2(player.GetAxis("MoveX"), player.GetAxis("MoveY"));
		UpdateCrosshair();

		if(player.GetButton("Shoot") && CanShoot) {
			ShootBullet();
		}
		if(moveVector.x != 0f || moveVector.y != 0f) {
			Move(moveVector);
		}
	}

	void MobileInputDoubleDPad() {
		DisableCrossHair();
		Vector2 moveVector = new Vector2(player.GetAxis("MoveX"), player.GetAxis("MoveY"));
		Vector3 aimVector = new Vector3(player.GetAxis("AimX"), Pos.y, player.GetAxis("AimY"));
		float aimReach = Mathf.Abs(aimVector.x) + Mathf.Abs(aimVector.z);
		mobileAimLine.Start = Vector3.zero;

		if(Physics.Raycast(bulletOutput.position, bulletOutput.forward, out RaycastHit hit, mobileAimHelperLineDistance)) {
			float distance = Vector3.Distance(bulletOutput.position, hit.point);
			if(distance < aimReach.Remap(0, 2, 1, mobileAimHelperLineDistance)) {
				mobileAimLineRight.transform.position = hit.point;
				mobileAimLineLeft.transform.position = hit.point;
				mobileAimLine.End = bulletOutput.transform.InverseTransformDirection(bulletOutput.forward) * distance;
			} else {
				mobileAimLineLeft.transform.position = bulletOutput.position + bulletOutput.transform.forward * aimReach.Remap(0, 2, 2, mobileAimHelperLineDistance);
				mobileAimLineRight.transform.position = mobileAimLineLeft.transform.position;
				mobileAimLine.End = bulletOutput.transform.InverseTransformDirection(bulletOutput.forward) * aimReach.Remap(0, 2, 2, mobileAimHelperLineDistance);
			}
        } else {
			mobileAimLineLeft.transform.position = bulletOutput.position + bulletOutput.transform.forward * aimReach.Remap(0, 2, 2, mobileAimHelperLineDistance);
			mobileAimLineRight.transform.position = mobileAimLineLeft.transform.position;
			mobileAimLine.End = bulletOutput.transform.InverseTransformDirection(bulletOutput.forward) * aimReach.Remap(0, 2, 2, mobileAimHelperLineDistance);
		}

		if (moveVector.x != 0f || moveVector.y != 0f) {
			Move(moveVector);
		}
		if(Mathf.Abs(aimVector.x) > 0.2f || Mathf.Abs(aimVector.z) > 0.2f) {
			MoveHead(Pos + aimVector * 5f);
			oldAimVector = aimVector;
        } else {
			MoveHead(Pos + oldAimVector * 5f);
		}
	}

	void MobileInputDPadAndTap() {
		Vector2 moveVector = new Vector2(player.GetAxis("MoveX"), player.GetAxis("MoveY"));
		UpdateCrosshair();

		if (moveVector.x != 0f || moveVector.y != 0f) {
			Move(moveVector);
		}
	}

	void MobileInputDPadAimAssist() {

    }

	void MobileInputDPadAndTapUpdate() {
		mouseLocation = new Vector2(player.GetAxis("TouchX") * Screen.width, player.GetAxis("TouchY") * Screen.height);
		UpdateCrosshair();
		ShootBullet();
    }
	void UpdateCrosshair() {
		if(Game.Platform == GamePlatform.Mobile) {
			if(player.GetAxis("TouchX") != 0 && player.GetAxis("TouchY") != 0) {
				mouseLocation = new Vector2(player.GetAxis("TouchX") * Screen.width, player.GetAxis("TouchY") * Screen.height);
			}
		} else if(Game.Platform == GamePlatform.Desktop) {
			if(Input.mousePosition.x != 0 && Input.mousePosition.y != 0) {
				mouseLocation = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
			}
		}
		
		Ray camRay = Camera.main.ScreenPointToRay(mouseLocation);
		Plane plane = new Plane(Vector3.up, -1f);
		plane.Raycast(camRay, out float enter);
		Vector3 hitPoint = camRay.GetPoint(enter);

		Crosshair.transform.position = hitPoint;
		Crosshair.transform.localScale = Vector3.one * (crossHairSize / 10f);

		if (Vector3.Distance(Pos, Crosshair.transform.position) > crossHairMaxAppearanceDistance) {
			crossLine.gameObject.SetActive(true);
			crossLine.Start = crossLine.transform.InverseTransformPoint(((tankHead.position + bulletOutput.position) / 2f) + Vector3.up * 0.35f);
			crossLine.End = Vector3.zero;
			crossLine.DashOffset += Time.deltaTime / 2f;
		} else {
			crossLine.gameObject.SetActive(false);
		}
		MoveHead(Crosshair.transform.position, true);
	}

	public override void Kill() {
		base.Kill();
		if(HasBeenDestroyed) {
			DisableControls();
			GameFeedbacks.PlayerDeath.PlayFeedbacks();
			GameCamera.ShortShake2D(0.5f, 40, 40);
		}
	}

	public new void TakeDamage(IDamageEffector effector) {
		base.TakeDamage(effector);

		if(HasBeenDestroyed) {
			DisableControls();
			GameFeedbacks.PlayerDeath.PlayFeedbacks();
			GameCamera.ShortShake2D(0.5f, 40, 40);
		}
	}

	public void Shoot() => ShootBullet();
	public override Bullet ShootBullet() {
		if(CanShoot) {
			crossHairMat.DOFloat(crossHairMat.GetFloat("_RotAngle") + crossHairRotSpeed, "_RotAngle", reloadDuration).SetEase(Ease.OutCirc);
		}
		return base.ShootBullet();
	}

	public void DisableControls() {
		disableControl = true;
		IsInvincible = true;
		PlayerInputManager.HideControls();
	}

	public void EnableControls() {
		disableControl = false;
		IsInvincible = false;
		PlayerInputManager.ShowControls();
	}

	public void EnableCrossHair() => Crosshair.SetActive(true);
	public void DisableCrossHair() => Crosshair.SetActive(false);

	public override void ResetState() {
		base.ResetState();
	}
}
