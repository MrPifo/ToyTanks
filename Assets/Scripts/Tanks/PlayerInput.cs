using UnityEngine;
using Shapes;
using Rewired;
using DG.Tweening;
using ToyTanks.LevelEditor;
using Rewired.ComponentControls;
using UnityEngine.EventSystems;
using SimpleMan.Extensions;

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
	public CrossHair CrossHair { private set; get; }
	public float mobileAimHelperLineDistance = 5;
	public float crossHairMaxAppearanceDistance = 5f;
	public float crossHairSize = 1.5f;
	public bool disableControl;
	public bool IgnoreDisables;
	public bool touchShootRegionPressed;
	public float crossHairRotSpeed;
	public Vector2 mouseLocation;
	Vector3 oldAimVector;
	private bool isFrosted;
	private Color frostColor;
	public Canvas ScreenEffectCamera;
	public UnityEngine.UI.Image FrostScreen;

	protected override void Awake() {
		base.Awake();
		mobileAimLine.gameObject.SetActive(false);
		frostColor = FrostScreen.color;
		FrostScreen.Hide();
	}

	public override void InitializeTank() {
		base.InitializeTank();
		playerControlScheme = Game.PlayerControlScheme;
		Debug.Log("Scheme: " + Game.PlayerControlScheme);
		makeInvincible = Game.isPlayerGodMode;
		player = ReInput.players.GetPlayer(0);
		ScreenEffectCamera.worldCamera = GameCamera.Instance.Camera;
		ScreenEffectCamera.planeDistance = 0.5f;
		SetupControls();
		if(CrossHair != null) {
			CrossHair.DestroyCrossHair();
		}
		CrossHair = CrossHair.CreateCrossHair(this, player);
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
					CrossHair.EnableCrossHair();

					break;
				case PlayerControlSchemes.DoubleDPad:
					PlayerInputManager.Instance.DoubleDPadUI.gameObject.Show();
					PlayerInputManager.Instance.touchController.Show();
					mobileAimLine.gameObject.SetActive(true);
					CrossHair.DisableCrossHair();
					touchRegion.InteractionStateChangedToPressed += Shoot;
					aimJoystick.TapEvent += Shoot;

					break;
				case PlayerControlSchemes.DpadAndTap:
					PlayerInputManager.Instance.DPadTapUI.gameObject.Show();
					PlayerInputManager.Instance.touchController.Show();
					mobileAimLine.gameObject.SetActive(false);
					CrossHair.EnableCrossHair();
					touchPad.TapEvent += MobileInputDPadAndTapUpdate;

					break;
				case PlayerControlSchemes.DoubleDPadAimAssistant:
					PlayerInputManager.Instance.touchController.Show();
					mobileAimLine.gameObject.SetActive(true);
					CrossHair.DisableCrossHair();
					aimJoystick.TapEvent += Shoot;

					break;
			}
		}
	}

	protected void FixedUpdate() {
		rig.velocity = Vector3.zero;
		if(HasBeenInitialized && IsPaused == false || IgnoreDisables) {
			if(!disableControl && CrossHair != null || IgnoreDisables) {
				PlayerControl();
			}
		}
	}

	public override void Move(Vector2 inputDir) {
		moveDir = new Vector3(inputDir.x, 0, inputDir.y);
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
		var movePos = dirFactor * moveSpeed * rig.transform.forward * (isFrosted ? 0.5f : 1f);
		if(!isShootStunned && canMove) {
			rig.velocity = movePos;
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

		if(player.GetButton("Shoot") && CanShoot) {
			ShootBullet();
		}
		if(moveVector.x != 0f || moveVector.y != 0f) {
			Move(moveVector);
		}
	}

	void MobileInputDoubleDPad() {
		CrossHair.DisableCrossHair();
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

		if (moveVector.x != 0f || moveVector.y != 0f) {
			Move(moveVector);
		}
	}

	void MobileInputDPadAimAssist() {

    }

	void MobileInputDPadAndTapUpdate() {
		mouseLocation = new Vector2(player.GetAxis("TouchX") * Screen.width, player.GetAxis("TouchY") * Screen.height);
		ShootBullet();
    }
	

	public override void TakeDamage(IDamageEffector effector, bool instantKill = false) {
		base.TakeDamage(effector, instantKill);

		if(IsInvincible == false) {
			StreakBubble.Interrupt();
			// Player can only end game once and when it is in control
			if(HasBeenDestroyed && disableControl == false) {
				DisableControls();
				try {
					GameFeedbacks.PlayerDeath.PlayFeedbacks();
				} catch {
					Logger.Log(Channel.Gameplay, "An error occured while playing PlayerDeath Feedbacks");
				}
				GameCamera.ShortShake2D(0.5f, 40, 40);
				LevelManager.Instance?.PlayerDead();
			} else {
				GameFeedbacks.PlayerHit.PlayFeedbacks();
				GameCamera.ShortShake2D(0.5f, 5, 5);
			}
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
		PlayerInputManager.HideControls();
	}

	public void EnableControls() {
		disableControl = false;
		PlayerInputManager.ShowControls();
	}

	public override void ResetState() {
		base.ResetState();
	}

	//Screen Effects
	public void FrostEffect(float duration) {
		if(isFrosted == false) {
			isFrosted = true;
			FrostScreen.Show();
			DOTween.To(() => FrostScreen.material.GetFloat("_AlphaClip"), x => FrostScreen.material.SetFloat("_AlphaClip", x), 0, 0.4f);
			this.Delay(duration, () => {
				DOTween.To(() => FrostScreen.material.GetFloat("_AlphaClip"), x => FrostScreen.material.SetFloat("_AlphaClip", x), 1, 0.4f).OnComplete(() => {
					isFrosted = false;
					FrostScreen.Hide();
				});
			});
		}
	}

	public void OnDestroy() {
		if(CrossHair != null) {
			CrossHair.DestroyCrossHair();
		}
	}
}
