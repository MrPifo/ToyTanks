using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes;

public class PlayerInput : TankBase {

	public GameObject crosshair => LevelManager.UI.crossHair;
	public Color crossHairColor;
	public Color crossHairColor2;
	public float crossHairSize;
	public float crossHairThickness;
	public bool disableCrossHair = true;
	public bool disableControl;
	Line[] crossHairLines;

	public override void InitializeTank() {
		base.InitializeTank();
		healthPoints = 0;
	}

	public void SetupCross() {
		crosshair.gameObject.SetActive(true);
		crossHairLines = crosshair.transform.GetComponentsInChildren<Line>();
		crosshair.transform.position = Vector3.zero;
		crosshair.transform.LookAt(Camera.main.transform);
		disableCrossHair = false;
		AdjustCrosshair();
	}

	void Update() {
		if(HasBeenInitialized) {
			if(!disableCrossHair && crosshair != null) {
				Crosshair();
			}
			if(!disableControl && crosshair != null) {
				MoveTank();
				if(Input.GetKeyDown(KeyCode.Mouse0)) {
					ShootBullet();
				}
			}
		}
	}

	void MoveTank() {
		var moveVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

		if(moveVector.x != 0f || moveVector.y != 0f) {
			Move(moveVector);
		}
		MoveHead(new Vector3(crosshair.transform.position.x, 0, crosshair.transform.position.z));
	}

	void Crosshair() {
		Vector2 mouseInput = Input.mousePosition;
		Ray camRay = Camera.main.ScreenPointToRay(mouseInput);
		float distToPlayground = Vector3.Distance(transform.position, camRay.origin);
		float my = camRay.origin.y / camRay.direction.y;

		Plane plane = new Plane(Vector3.up, 0f);
		plane.Raycast(camRay, out float enter);
		Vector3 hitPoint = camRay.GetPoint(enter);

		crosshair.transform.position = hitPoint;
		Debug.DrawLine(camRay.origin, camRay.origin + camRay.direction * distToPlayground * 1.5f, Color.red);
	}

	void AdjustCrosshair() {
		crossHairLines[0].Start = new Vector3(crossHairSize, crossHairSize, 0);
		crossHairLines[0].End = new Vector3(crossHairSize * 2f, crossHairSize * 2f, 0);
		crossHairLines[0].Thickness = crossHairThickness;
		crossHairLines[0].Color = crossHairColor;
		crossHairLines[0].ColorStart = crossHairColor;
		crossHairLines[0].ColorEnd = crossHairColor2;

		crossHairLines[1].Start = new Vector3(-crossHairSize, crossHairSize, 0);
		crossHairLines[1].End = new Vector3(-crossHairSize * 2f, crossHairSize * 2f, 0);
		crossHairLines[1].Thickness = crossHairThickness;
		crossHairLines[1].Color = crossHairColor;
		crossHairLines[1].ColorStart = crossHairColor;
		crossHairLines[1].ColorEnd = crossHairColor2;

		crossHairLines[2].Start = new Vector3(crossHairSize, -crossHairSize, 0);
		crossHairLines[2].End = new Vector3(crossHairSize * 2f, -crossHairSize * 2f, 0);
		crossHairLines[2].Thickness = crossHairThickness;
		crossHairLines[2].Color = crossHairColor;
		crossHairLines[2].ColorStart = crossHairColor;
		crossHairLines[2].ColorEnd = crossHairColor2;

		crossHairLines[3].Start = new Vector3(-crossHairSize, -crossHairSize, 0);
		crossHairLines[3].End = new Vector3(-crossHairSize * 2f, -crossHairSize * 2f, 0);
		crossHairLines[3].Thickness = crossHairThickness;
		crossHairLines[3].Color = crossHairColor;
		crossHairLines[3].ColorStart = crossHairColor;
		crossHairLines[3].ColorEnd = crossHairColor2;
	}

	public override void GotHitByBullet() {
		base.GotHitByBullet();
	}

	public void DisablePlayer() {
		disableControl = true;
		disableCrossHair = true;
		IsInvincible = true;
	}

	public void EnablePlayer() {
		disableControl = false;
		disableCrossHair = false;
		IsInvincible = false;
	}
}
