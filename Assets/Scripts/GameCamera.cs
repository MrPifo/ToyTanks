using UnityEngine;
using DG.Tweening;
using CameraShake;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Camera))]
public class GameCamera : MonoBehaviour {

	public enum GameCamState { Free, Overview, Focus }
	// Free - Camera is free to use
	// Overview - Camera is static and views the whole playground
	// Focus - Camera follows the player

	public float followLeadAhead = 5;
	public float minOrthographicSize;
	public float maxOrthographicSize;
	public float currentOrthographicSize;
	public CameraShaker Shaker { get; set; }
	public Camera Camera { get; set; }
	public PlayerInput Player { get; set; }
	public float OrthoSize { get; set; }
	public MenuCameraSettings camSettings = new MenuCameraSettings() {
		pos = new Vector3(-1, 30, -32),
		rot = new Vector3(44, 0, 0),
		orthograpicSize = 15
	};
	public Vector3 initPos;
	public Vector3 offset;
	public GameCamState cameraState = GameCamState.Free;

	void Awake() {
		Camera = GetComponent<Camera>();
		currentOrthographicSize = minOrthographicSize;
		Shaker = new GameObject("Camera Holder").AddComponent<CameraShaker>();
		Shaker.SetCameraTransform(Shaker.transform);
		transform.SetParent(Shaker.transform);
		if(FindObjectOfType<GameManager>() != null) {
			SceneManager.MoveGameObjectToScene(Shaker.gameObject, SceneManager.GetSceneByName("Level"));
		}
		ChangeState(GameCamState.Overview);
	}

	void Update() {
		switch(cameraState) {
			case GameCamState.Free:
				return;
			case GameCamState.Focus:
				FollowPlayer();
				break;
		}
	}

	void FollowPlayer() {
		if(Player != null) {
			Vector3 current = Camera.main.transform.position;
			Vector3 targetPos = new Vector3() {
				x = Player.transform.position.x,
				y = Camera.main.transform.position.y,
				z = Player.transform.position.z,
			};
			targetPos += new Vector3(0, 0, -30) + offset + Player.MovingDir * followLeadAhead;
			float dist = Vector2.Distance(new Vector2(current.x, current.z), new Vector2(targetPos.x, targetPos.z));

			Camera.main.transform.position = Vector3.Lerp(current, targetPos, Time.deltaTime);

			//if(Input.mouseScrollDelta > 0)
		} else {
			Player = FindObjectOfType<PlayerInput>();
		}
	}

	void Overview() {
		float duration = 2f;
		Camera.transform.DOMove(camSettings.pos, duration);
		Camera.transform.DORotate(camSettings.rot, duration);
		Camera.DOOrthoSize(camSettings.orthograpicSize, duration);
	}

	public void SetOrthographicSize(float size) {
		//float aspect = (float)Screen.width / Screen.height;
		//Camera.projectionMatrix = Matrix4x4.Ortho(-size * aspect, size * aspect, -size, size, 0.2f, 200);
		OrthoSize = size;
		Camera.ResetAspect();
		Camera.ResetProjectionMatrix();
		Camera.orthographicSize = size;
	}

	/*public void SetCameraView(MenuCameraSettings settings, float duration, bool disableOrtho = false) {
		if(disableOrtho == false) {
			Camera.DOOrthoSize(settings.orthograpicSize, duration);
		}
		Camera.transform.DOMove(settings.pos, duration);
		Camera.transform.DORotate(settings.rot, duration);
		Player = FindObjectOfType<PlayerInput>();
		initPos = settings.pos;
	}*/

	public void ChangeState(GameCamState state) {
		cameraState = state;
		switch(state) {
			case GameCamState.Free:
				break;
			case GameCamState.Overview:
				Overview();
				break;
			case GameCamState.Focus:
				Camera.DOOrthoSize(camSettings.orthograpicSize, 2);
				FollowPlayer();
				break;
		}
	}

	public static void ShakeExplosion2D(float strength, float duration) {
		CameraShaker.Presets.Explosion2D(strength, strength, duration);
	}

	public static void ShakeExplosion3D(float strength, float duration) {
		CameraShaker.Presets.Explosion3D(strength, duration);
	}
}
