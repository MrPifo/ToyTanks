using Rewired;
using Shapes;
using Sperlich.PrefabManager;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class CrossHair : MonoBehaviour {

	public TankBase assignedTank;
	public float crossHairSize = 1f;
	public float lineAppearanceDistance = 5f;
	private Player input;
	private Material crossHairMat;
	[SerializeField]
	private GameObject crossHair;
	[SerializeField]
	private Line crossLine;

	public void SetupCross() {
		crossHair = transform.GetChild(0).gameObject;
		crossHairMat = crossHair.GetComponent<MeshRenderer>().material;
		transform.position = Vector3.zero;
		crossHairMat.SetFloat("_RotAngle", 0);
		transform.rotation = Quaternion.identity;
	}

	void UpdateCrosshair() {
		Vector2 mouseLocation = Input.mousePosition;
		if(Game.Platform == GamePlatform.Mobile) {
			if(input.GetAxis("TouchX") != 0 && input.GetAxis("TouchY") != 0) {
				mouseLocation = new Vector2(input.GetAxis("TouchX") * Screen.width, input.GetAxis("TouchY") * Screen.height);
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

		transform.position = hitPoint;
		transform.localScale = Vector3.one * (crossHairSize / 10f);

		if(Vector3.Distance(transform.position, assignedTank.Pos) > lineAppearanceDistance) {
			crossLine.gameObject.SetActive(true);
			crossLine.Start = crossLine.transform.InverseTransformPoint(((assignedTank.tankHead.position + assignedTank.bulletOutput.position) / 2f) + Vector3.up * 0.35f);
			crossLine.End = Vector3.zero;
			crossLine.DashOffset += Time.deltaTime / 2f;
		} else {
			crossLine.gameObject.SetActive(false);
		}
		assignedTank.MoveHead(transform.position, true);
	}

	private void LateUpdate() {
		if(crossHair.activeSelf) {
			UpdateCrosshair();
		}
	}

	public void EnableCrossHair() => crossHair.SetActive(true);
	public void DisableCrossHair() { crossHair.SetActive(false); }
	public void DestroyCrossHair() => Destroy(gameObject);

	public static CrossHair CreateCrossHair(TankBase assignedTank, Player inputModule) {
		var waiter = Addressables.InstantiateAsync("CrossHair", null, true);
		waiter.WaitForCompletion();
		var crossHair = waiter.Result.gameObject.GetComponent<CrossHair>();
		crossHair.input = inputModule;
		crossHair.assignedTank = assignedTank;
		crossHair.EnableCrossHair();
		crossHair.SetupCross();
		return crossHair;
	}

}
