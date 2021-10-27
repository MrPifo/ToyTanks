using UnityEngine;
using Shapes;
using Rewired;
using DG.Tweening;

public class PlayerInput : TankBase, IHittable {

	GameObject _crossHair;
	Line crossLine;
	public Player player;
	public Material crossHairMat;
	public GameObject crosshair {
		get {
			if(_crossHair == null) {
				_crossHair = GameObject.FindGameObjectWithTag("CrossHair");
				crossLine = _crossHair.transform.GetChild(0).GetComponent<Line>();
			}
			return _crossHair;
		}
	}
	public float crossHairMaxAppearanceDistance = 10f;
	public float crossHairSize = 1.5f;
	public bool disableCrossHair = true;
	public bool disableControl;
	public bool IgnoreDisables;
	public float crossHairRotSpeed;
	Vector2 mouseLocation;

	private new void Awake() {
		base.Awake();
		HideCrosshair();
	}

	public override void InitializeTank() {
		base.InitializeTank();
		player = ReInput.players.GetPlayer(0);

		Debug.Log("Currently connected controllers: " + ReInput.controllers.controllerCount);
		foreach(var controller in ReInput.controllers.Controllers) {
			Debug.Log(controller.hardwareName + " : " + controller.identifier.controllerType);
		}
		ReInput.ControllerConnectedEvent += (ControllerStatusChangedEventArgs args) => {
			Debug.Log("Controller connected: " + args.name);
		};
		makeInvincible = Game.isPlayerGodMode;
		EnableCrosshair();
		SetupCross();
	}

	public void SetupCross() {
		crosshair.gameObject.SetActive(true);
		crossLine.gameObject.SetActive(true);
		crosshair.transform.position = Vector3.zero;
		disableCrossHair = false;
		crossHairMat.SetFloat("_RotAngle", 0);
		crosshair.transform.parent = null;
		crosshair.transform.rotation = Quaternion.identity;
		EnableCrosshair();
	}

	void Update() {
		if(HasBeenInitialized && IsPaused == false || IgnoreDisables) {
			if(!disableControl && crosshair != null || IgnoreDisables) {
				MoveTank();
				if(player.GetButtonDown("Shoot")) {
					ShootBullet();
				}
			}
		}

		if(Camera.main != null && crosshair != null && !disableCrossHair || DebugPlay.isDebug) {
			Crosshair();
		}
	}

	void MoveTank() {
		var moveVector = new Vector2(player.GetAxis("Move X"), player.GetAxis("Move Y"));

		if(moveVector.x != 0f || moveVector.y != 0f) {
			Move(moveVector);
		}
		MoveHead(new Vector3(crosshair.transform.position.x, 0, crosshair.transform.position.z));
		AdjustOccupiedGridPos();
	}

	void Crosshair() {
		//mouseLocation += new Vector2(player.GetAxis("Aim X"), player.GetAxis("Aim Y"));
		//float distToPlayground = Vector3.Distance(transform.position, camRay.origin);
		mouseLocation = Input.mousePosition;
		Ray camRay = Camera.main.ScreenPointToRay(mouseLocation);
		Plane plane = new Plane(Vector3.up, 0f);
		plane.Raycast(camRay, out float enter);
		Vector3 hitPoint = camRay.GetPoint(enter);

		crosshair.transform.position = hitPoint;
		crosshair.transform.localScale = Vector3.one * (crossHairSize / 10f);

		if(Vector3.Distance(Pos, crosshair.transform.position) > crossHairMaxAppearanceDistance) {
			crossLine.gameObject.SetActive(true);
			crossLine.Start = crossLine.transform.InverseTransformPoint(((tankHead.position + bulletOutput.position) / 2f) + Vector3.up * 0.35f);
			crossLine.End = Vector3.zero;
			crossLine.DashOffset += Time.deltaTime / 2f;
		} else {
			crossLine.gameObject.SetActive(false);
		}
	}

	public new void Revive() {
		base.Revive();
		HideCrosshair();
		EnablePlayer();
	}

	public new void TakeDamage(IDamageEffector effector) {
		base.TakeDamage(effector);
		if(IsInvincible == false) {
			DisablePlayer();
		}
	}

	public override void ShootBullet() {
		if(CanShoot) {
			crossHairMat.DOFloat(crossHairMat.GetFloat("_RotAngle") + crossHairRotSpeed, "_RotAngle", reloadDuration).SetEase(Ease.OutCirc);
		}
		base.ShootBullet();
		//DOTween.To(() => t = x, , reloadDuration);
		//crossHairContainer.transform.DOLocalRotate(crossHairContainer.rotation.eulerAngles + new Vector3(0, 0, 45), 1f);
	}

	public void DisablePlayer() {
		disableControl = true;
		IsInvincible = true;
	}

	public void EnablePlayer() {
		disableControl = false;
		IsInvincible = false;
	}

	public void HideCrosshair() {
		crosshair.gameObject.SetActive(false);
		crossLine.gameObject.SetActive(false);
	}

	public void EnableCrosshair() {
		crosshair.gameObject.SetActive(true);
		crossLine.gameObject.SetActive(true);
	}
}
