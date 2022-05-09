using UnityEngine;
using Shapes;
using Rewired;
using DG.Tweening;
using Rewired.ComponentControls;
using SimpleMan.Extensions;
using MoreMountains.Feedbacks;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Sperlich.Types;

public class PlayerTank : TankBase, IHittable, IResettable {

	public Player inputController;
	[Header("Player")]
	public CombatAbility equippedAbility;
	public float overheatPenaltyDuration;
	public float overheatBuildupRate;
	public float mobileAimHelperLineDistance = 5;
	public float crossHairMaxAppearanceDistance = 5f;
	public float crossHairSize = 1.5f;
	public float crossHairRotSpeed;
	public bool disableControl;
	public bool IgnoreDisables;
	public bool touchShootRegionPressed;
	public bool combatAbilityUsed;
	public Slider overheatMeter;
	public Image overheatFill;
	public CanvasGroup combatAbilityHolder;
	public RectTransform combatAbilityButtonNotice;
	public RectTransform combatAbilityBorder;
	public Image combatAbilityIcon;
	public Line mobileAimLine;
	public Line mobileAimLineRight;
	public Line mobileAimLineLeft;
	public Sphere overheatMuzzle;
	public Material crossHairMat;
	public Canvas ScreenEffectCamera;
	public MMFeedbacks playerDeath;
	public UnityEngine.UI.Image FrostScreen;
	
	bool isFrosted;
	float combatAbilityUseTime;
	float overheatBuildup;
	Vector3 oldAimVector;
	TouchPad touchPad;
	TouchRegion touchRegion;
	TouchJoystick aimJoystick;

	public PlayerControlSchemes playerControlScheme => Game.PlayerControlScheme;
	public PlayerAbility CombatAbilityAsset => AssetLoader.GetCombatAbility(equippedAbility);
	public CrossHair CrossHair { private set; get; }
	public Vector2 moveVector { get; private set; }

	protected override void Awake() {
		base.Awake();
		mobileAimLine.gameObject.SetActive(false);
		FrostScreen.Hide();
		combatAbilityHolder.gameObject.Hide();
	}

	public override void InitializeTank() {
		base.InitializeTank();
		makeInvincible = Game.isPlayerGodMode;
		inputController = ReInput.players.GetPlayer(0);
		ScreenEffectCamera.worldCamera = GameCamera.Instance.Camera;
		ScreenEffectCamera.planeDistance = 0.5f;
		SetupControls();
		AIManager.RegisterPlayer(this);
		if(equippedAbility != CombatAbility.None) {
			combatAbilityHolder.transform.localScale = Vector3.zero;
			combatAbilityHolder.gameObject.Show();
			combatAbilityHolder.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBounce);
			combatAbilityIcon.sprite = CombatAbilityAsset.icon;
		} else {
			combatAbilityHolder.gameObject.Hide();
		}
	}

	public void SetupControls() {
		if(CrossHair != null) {
			CrossHair.DestroyCrossHair();
		}
		inputController = ReInput.players.GetPlayer(0);
		switch(playerControlScheme) {
			case PlayerControlSchemes.Desktop:
				CrossHair = CrossHair.CreateCrossHair(this, inputController);
				mobileAimLine.gameObject.SetActive(false);
				CrossHair.EnableCrossHair();
				break;
			case PlayerControlSchemes.DoubleDPad:
				mobileAimLine.gameObject.SetActive(true);
				touchRegion = GameObject.FindGameObjectWithTag("ShootTouchRegion").GetComponent<TouchRegion>();
				aimJoystick = GameObject.FindGameObjectWithTag("AimJoystick").GetComponent<TouchJoystick>();
				touchRegion.InteractionStateChangedToPressed += Shoot;
				aimJoystick.TapEvent += Shoot;

				break;
			case PlayerControlSchemes.DpadAndTap:
				CrossHair = CrossHair.CreateCrossHair(this, inputController);
				mobileAimLine.gameObject.SetActive(false);
				CrossHair.EnableCrossHair();
				touchPad = GameObject.FindGameObjectWithTag("TouchPad").GetComponent<TouchPad>();
				touchPad.TapEvent += MobileInputDPadAndTapUpdate;

				break;
			case PlayerControlSchemes.DoubleDPadAimAssistant:
				mobileAimLine.gameObject.SetActive(true);
				aimJoystick = GameObject.FindGameObjectWithTag("AimJoystick").GetComponent<TouchJoystick>();
				aimJoystick.TapEvent += Shoot;
				break;
		}
	}

	protected void FixedUpdate() {
		rig.velocity = Vector3.zero;
		if (disableShooting == false) {
			if (overheatBuildup > 0) {
				overheatBuildup -= Time.fixedDeltaTime / 3f;
			} else if (overheatBuildup < 0) {
				overheatBuildup = 0;
			}
			overheatMeter.value = overheatBuildup;
			overheatFill.color = Color.Lerp(new Color(0, 1f, 0) * 2.5f, new Color(1f, 0, 0) * 2.5f, overheatBuildup);
			overheatMuzzle.Color = Color.Lerp(new Color(1f, 1f, 1f, 0f), new Color(1f, 0f, 0f, 1f) * 5, overheatBuildup - 0.5f);
		}

		if(HasBeenInitialized && Game.IsGameCurrentlyPlaying && IsPaused == false && disableControl == false || IgnoreDisables) {
			PlayerControl();
		}
	}

	private void Update() {
		if(HasBeenInitialized && Game.IsGameCurrentlyPlaying) {
			if (Input.GetKeyDown(KeyCode.R) && combatAbilityUsed) {
				combatAbilityUsed = false;
				combatAbilityHolder.alpha = 1f;
			}
		}
	}

	protected override void Move(Vector2 inputDir) {
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
		TrackTracer();
	}

	public void ApplyCustomizations(TankCustomizer.TankPreset preset) {
		Destroy(tankBody.gameObject);
		Destroy(tankHead.gameObject);
		tankBody = Instantiate(AssetLoader.GetPart(TankPartAsset.TankPartType.Body, preset.bodyIndex).prefab, Vector3.zero, Quaternion.identity, transform).transform;
		tankHead = Instantiate(AssetLoader.GetPart(TankPartAsset.TankPartType.Head, preset.headIndex).prefab, Vector3.zero, Quaternion.identity, transform).transform;
		tankBody.localPosition = Vector3.zero;
		tankBody.localRotation = Quaternion.identity;
		tankHead.localPosition = Vector3.zero;
		tankHead.localRotation = Quaternion.identity;
		bulletOutput = tankHead.Find("BulletOutput").transform;
		mobileAimLine = bulletOutput.Find("Line").GetComponent<Line>();
		mobileAimLineRight = mobileAimLine.transform.Find("Line_Right").GetComponent<Line>();
		mobileAimLineLeft = mobileAimLine.transform.Find("Line_Left").GetComponent<Line>();
		muzzleFlash = bulletOutput.transform.Find("MuzzleFlash").GetComponent<ParticleSystem>();
		overheatMuzzle = tankHead.Find("MuzzleOverheat").GetComponent<Sphere>();
		References.damageSmokeBody = tankBody.Find("Damaged_Smoke").GetComponent<ParticleSystem>();
		References.damageSmokeHead = tankHead.Find("Damaged_Smoke").GetComponent<ParticleSystem>();
		References.destroyTransformPieces = new System.Collections.Generic.List<Transform>();
		References.destroyTransformPieces.Add(tankBody);
		References.destroyTransformPieces.Add(tankHead);
	}

	#region Combat Abilities
	void UseCombatAbility() {
		combatAbilityUsed = true;
		switch (equippedAbility) {
			case CombatAbility.RapidFire:
				RapidFire();
				break;
			case CombatAbility.PierceSuperBounce:
				PierceSuperBounce();
				break;
		}
	}
	void RapidFire() {
		Run().Forget();

		async UniTaskVoid Run() {
			float initReloadSpeed = reloadDuration;
			float initShootStun = shootStunDuration;
			combatAbilityUseTime = 0;
			reloadDuration.Value /= 2.5f;
			shootStunDuration.Value = 0f;
			combatAbilityHolder.alpha = 1f;
			combatAbilityHolder.transform.DOShakePosition(CombatAbilityAsset.useDuration, 15, 50);
			Camera.main.transform.DOShakePosition(CombatAbilityAsset.useDuration, 0.2f, 50);
			combatAbilityHolder.transform.DOScale(1.1f, 0.35f);
			combatAbilityButtonNotice.Hide();

			while(combatAbilityUseTime < CombatAbilityAsset.useDuration && HasBeenDestroyed == false && Game.IsGameCurrentlyPlaying) {
				await UniTask.WaitForFixedUpdate();
				combatAbilityUseTime += Time.fixedDeltaTime;
				overheatBuildup = 0;
				combatAbilityBorder.localRotation = Quaternion.Euler(0, 0, combatAbilityUseTime * 120);
			}
			reloadDuration.Value = initReloadSpeed;
			shootStunDuration.Value = initShootStun;
			combatAbilityHolder.alpha = 0.5f;
			combatAbilityHolder.transform.DOScale(1f, 0.35f);
		}
	}
	void PierceSuperBounce() {
		Run().Forget();

		async UniTaskVoid Run() {
			combatAbilityHolder.alpha = 1f;
			combatAbilityHolder.transform.DOShakePosition(CombatAbilityAsset.useDuration, 15, 50, 90, false, false);
			Camera.main.transform.DOShakePosition(CombatAbilityAsset.useDuration, 0.2f, 50, 90, false, false);
			combatAbilityHolder.transform.DOScale(1.1f, 0.35f);
			combatAbilityButtonNotice.Hide();
			References.bullet = Sperlich.PrefabManager.PrefabTypes.SuperPierceBounce;
			combatAbilityUseTime = 0;

			while (combatAbilityUseTime < CombatAbilityAsset.useDuration && HasBeenDestroyed == false && Game.IsGameCurrentlyPlaying) {
				await UniTask.WaitForFixedUpdate();
				combatAbilityUseTime += Time.fixedDeltaTime;
				combatAbilityBorder.localRotation = Quaternion.Euler(0, 0, combatAbilityUseTime * 120);
			}
			ShootBullet();
			combatAbilityHolder.alpha = 0.5f;
			combatAbilityHolder.transform.DOScale(1f, 0.35f);
			References.bullet = Sperlich.PrefabManager.PrefabTypes.Bullet;
		}
	}
	#endregion

	#region Input-Controllers
	void PlayerControl() {
		switch (playerControlScheme) {
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
		moveVector = new Vector2(inputController.GetAxis("MoveX"), inputController.GetAxis("MoveY"));

		if (inputController.GetButton("Shoot") && CanShoot) {
			ShootBullet();
		}
		if (moveVector.x != 0f || moveVector.y != 0f) {
			Move(moveVector);
		}
		if (inputController.GetButtonDown("CombatAbility") && combatAbilityUsed == false) {
			UseCombatAbility();
		}
	}
	void MobileInputDoubleDPad() {
		Vector2 moveVector = new Vector2(inputController.GetAxis("MoveX"), inputController.GetAxis("MoveY"));
		Vector3 aimVector = new Vector3(inputController.GetAxis("AimX"), Pos.y, inputController.GetAxis("AimY"));
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
		Vector2 moveVector = new Vector2(inputController.GetAxis("MoveX"), inputController.GetAxis("MoveY"));

		if (moveVector.x != 0f || moveVector.y != 0f) {
			Move(moveVector);
		}
	}
	void MobileInputDPadAimAssist() {

    }
	void MobileInputDPadAndTapUpdate() {
		//mouseLocation = new Vector2(inputController.GetAxis("TouchX") * Screen.width, inputController.GetAxis("TouchY") * Screen.height);
		ShootBullet();
    }
	public void DisableControls() {
		disableControl = true;
		CrossHair?.DisableCrossHair();
		PlayerInputManager.HideControls();
	}
	public void EnableControls() {
		disableControl = false;
		CrossHair?.EnableCrossHair();
		PlayerInputManager.ShowControls();
	}
	#endregion

	public override void TakeDamage(IDamageEffector effector, bool instantKill = false) {
		base.TakeDamage(effector, instantKill);

		if(IsInvincible == false) {
			StreakBubble.Interrupt();
			// Player can only end game once and when it is in control
			if(HasBeenDestroyed && disableControl == false) {
				DisableControls();
				try {
					//GameFeedbacks.PlayerDeath.PlayFeedbacks();
				} catch {
					Logger.Log(Channel.Gameplay, "An error occured while playing PlayerDeath Feedbacks");
				}
				GameCamera.ShortShake2D(0.5f, 40, 40);
				playerDeath.PlayFeedbacks();
				LevelManager.Instance?.PlayerDead();
			} else {
				GameCamera.ShortShake2D(0.5f, 5, 5);
			}
		}
	}
	public void Shoot() => ShootBullet();
	protected override Bullet ShootBullet() {
		if(CanShoot) {
			overheatBuildup += overheatBuildupRate;
			if(overheatBuildup > 1.1f) {
				TriggerOverheat().Forget();
            }
			crossHairMat.DOFloat(crossHairMat.GetFloat("_RotAngle") + crossHairRotSpeed, "_RotAngle", reloadDuration).SetEase(Ease.OutCirc);
		}
		return base.ShootBullet(1 + overheatBuildup);
	}

	public async UniTaskVoid TriggerOverheat() {
		overheatBuildup = 0;
		disableShooting = true;
		overheatFill.color = Color.white * 2;
		DOTween.To(() => overheatMeter.value, x => overheatMeter.value = x, 0f, overheatPenaltyDuration).SetEase(Ease.Linear);
		DOTween.To(() => overheatMuzzle.Color, x => overheatMuzzle.Color = x, new Color(0f, 0f, 0f, 0f), overheatPenaltyDuration);
		await UniTask.WaitForSeconds(overheatPenaltyDuration);
		disableShooting = false;
    }

	#region Effects
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
	#endregion

	public void OnDestroy() {
		if(CrossHair != null) {
			HasBeenDestroyed = true;
			CrossHair.DestroyCrossHair();
		}
	}
}
