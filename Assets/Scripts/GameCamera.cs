using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Camera))]
public class GameCamera : MonoBehaviour {

	public int minOrthographicSize;
	public int maxOrthographicSize;
	public Camera Camera { get; set; }
	public float OrthoSize { get; set; }
	public bool DisableController { get; set; }
	public MenuCameraSettings camSettings = new MenuCameraSettings() {
		pos = new Vector3(-1, 30, -32),
		rot = new Vector3(44, 0, 0),
		orthograpicSize = 15
	};

	void Awake() {
		Camera = GetComponent<Camera>();
	}

	void Update() {
		if(DisableController) {
			return;
		}
	}

	public void SetOrthographicSize(float size) {
		//float aspect = (float)Screen.width / Screen.height;
		//Camera.projectionMatrix = Matrix4x4.Ortho(-size * aspect, size * aspect, -size, size, 0.2f, 200);
		OrthoSize = size;
		Camera.ResetAspect();
		Camera.ResetProjectionMatrix();
		Camera.orthographicSize = size;
	}

	public void SetCameraView(MenuCameraSettings settings, float duration, bool disableOrtho = false) {
		if(disableOrtho == false) {
			Camera.DOOrthoSize(settings.orthograpicSize, duration);
		}
		Camera.transform.DOMove(settings.pos, duration);
		Camera.transform.DORotate(settings.rot, duration);
	}
}
