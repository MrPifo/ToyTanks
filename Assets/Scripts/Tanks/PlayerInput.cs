using UnityEngine;
using Shapes;
using Rewired;
using DG.Tweening;
using ToyTanks.LevelEditor;

public class PlayerInput : TankBase, IHittable, IResettable {

	public Player player;
	public Material crossHairMat;
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
	public float crossHairRotSpeed;
	Vector2 mouseLocation;

	private new void Awake() {
		base.Awake();
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
		SetupCross();
	}

	public void SetupCross() {
		Crosshair.gameObject.SetActive(true);
		crossLine.gameObject.SetActive(true);
		Crosshair.transform.position = Vector3.zero;
		crossHairMat.SetFloat("_RotAngle", 0);
		Crosshair.transform.rotation = Quaternion.identity;
	}

	void Update() {
		if(HasBeenInitialized && IsPaused == false || IgnoreDisables) {
			if(!disableControl && Crosshair != null || IgnoreDisables) {
				MoveTank();
				if(player.GetButtonDown("Shoot")) {
					ShootBullet();
				}
			}
		}

		if(Camera.main != null && Crosshair != null && Game.GamePaused == false && HasBeenDestroyed == false && Crosshair.activeSelf || DebugPlay.isDebug) {
			UpdateCrosshair();
			MoveHead(new Vector3(Crosshair.transform.position.x, 0, Crosshair.transform.position.z));
		}
	}

	void MoveTank() {
		var moveVector = new Vector2(player.GetAxis("Move X"), player.GetAxis("Move Y"));

		if(moveVector.x != 0f || moveVector.y != 0f) {
			Move(moveVector);
		}
		AdjustOccupiedGridPos();
	}

	void UpdateCrosshair() {
		//mouseLocation += new Vector2(player.GetAxis("Aim X"), player.GetAxis("Aim Y"));
		//float distToPlayground = Vector3.Distance(transform.position, camRay.origin);
		mouseLocation = Input.mousePosition;
		Ray camRay = Camera.main.ScreenPointToRay(mouseLocation);
		Plane plane = new Plane(Vector3.up, -1f);
		plane.Raycast(camRay, out float enter);
		Vector3 hitPoint = camRay.GetPoint(enter);

		Crosshair.transform.position = hitPoint;
		Crosshair.transform.localScale = Vector3.one * (crossHairSize / 10f);

		if(Vector3.Distance(Pos, Crosshair.transform.position) > crossHairMaxAppearanceDistance) {
			crossLine.gameObject.SetActive(true);
			crossLine.Start = crossLine.transform.InverseTransformPoint(((tankHead.position + bulletOutput.position) / 2f) + Vector3.up * 0.35f);
			crossLine.End = Vector3.zero;
			crossLine.DashOffset += Time.deltaTime / 2f;
		} else {
			crossLine.gameObject.SetActive(false);
		}
	}

	public new void TakeDamage(IDamageEffector effector) {
		base.TakeDamage(effector);
		if(HasBeenDestroyed && IsInvincible == false) {
			DisableControls();
		}
	}

	public override Bullet ShootBullet() {
		if(CanShoot) {
			crossHairMat.DOFloat(crossHairMat.GetFloat("_RotAngle") + crossHairRotSpeed, "_RotAngle", reloadDuration).SetEase(Ease.OutCirc);
		}
		return base.ShootBullet();
	}

	public void DisableControls() {
		disableControl = true;
		IsInvincible = true;
	}

	public void EnableControls() {
		disableControl = false;
		IsInvincible = false;
	}

	public void EnableCrossHair() => Crosshair.SetActive(true);
	public void DisableCrossHair() => Crosshair.SetActive(false);

	public override void ResetState() {
		base.ResetState();
	}
}
