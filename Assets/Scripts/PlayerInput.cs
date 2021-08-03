using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes;

[RequireComponent(typeof(TankBase))]
public class PlayerInput : MonoBehaviour {

	TankBase tank;
	public Transform crosshair;
	public Color crossHairColor;
	public float crossHairSize;
	public float crossHairThickness;
	public bool hideCursor;
	Line[] crossHairLines;

	void Awake() {
		tank = GetComponent<TankBase>();
		crossHairLines = crosshair.GetComponentsInChildren<Line>();
		crosshair.LookAt(Camera.main.transform);
	}

	void Update() {
		Cursor.visible = !hideCursor;
		Crosshair();
		if(Input.GetKeyDown(KeyCode.Mouse0)) {
			tank.ShootBullet();
		}
	}

	void FixedUpdate() {
		MoveTank();
	}

	void MoveTank() {
		var moveVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

		if(moveVector.x != 0f || moveVector.y != 0f) {
			tank.Move(moveVector);
		}
		tank.MoveHead(new Vector3(crosshair.position.x, 0, crosshair.position.z));
	}

	void Crosshair() {
		Vector2 mouseInput = Input.mousePosition;
		Ray camRay = Camera.main.ScreenPointToRay(mouseInput);
		float distToPlayground = Vector3.Distance(tank.transform.position, camRay.origin);
		float my = camRay.origin.y / camRay.direction.y;

		Plane plane = new Plane(Vector3.up, 0f);
		plane.Raycast(camRay, out float enter);
		Vector3 hitPoint = camRay.GetPoint(enter);

		crosshair.position = hitPoint;
		Debug.DrawLine(camRay.origin, camRay.origin + camRay.direction * distToPlayground * 1.5f, Color.red);
		AdjustCrosshair();
	}

	void AdjustCrosshair() {
		crossHairLines[0].Start = new Vector3(crossHairSize, crossHairSize, 0);
		crossHairLines[0].End = new Vector3(crossHairSize * 2f, crossHairSize * 2f, 0);
		crossHairLines[0].Thickness = crossHairThickness;
		crossHairLines[0].Color = crossHairColor;

		crossHairLines[1].Start = new Vector3(-crossHairSize, crossHairSize, 0);
		crossHairLines[1].End = new Vector3(-crossHairSize * 2f, crossHairSize * 2f, 0);
		crossHairLines[1].Thickness = crossHairThickness;
		crossHairLines[1].Color = crossHairColor;

		crossHairLines[2].Start = new Vector3(crossHairSize, -crossHairSize, 0);
		crossHairLines[2].End = new Vector3(crossHairSize * 2f, -crossHairSize * 2f, 0);
		crossHairLines[2].Thickness = crossHairThickness;
		crossHairLines[2].Color = crossHairColor;

		crossHairLines[3].Start = new Vector3(-crossHairSize, -crossHairSize, 0);
		crossHairLines[3].End = new Vector3(-crossHairSize * 2f, -crossHairSize * 2f, 0);
		crossHairLines[3].Thickness = crossHairThickness;
		crossHairLines[3].Color = crossHairColor;
	}
}
