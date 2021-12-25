using UnityEngine;
using DG.Tweening;
using CameraShake;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Camera))]
public class GameCamera : Singleton<GameCamera> {

	public enum GameCamState { Free, Overview, Focus }
	// Free - Camera is free to use
	// Overview - Camera is static and views the whole playground
	// Focus - Camera follows the player

	public float debugSpeed;
	private float preferredOrthographicSize;
	public float focusOnPlayerStrength = 1f;
	public float focusSmoothSpeed = 1;
	public float focusZoomSmoothSpeed = 0.5f;
	public float minOrthographicSize = 9;
	public float maxOrthographicSize = 19;

	// Debug
	bool moveUp;
	bool moveRight;
	bool moveLeft;
	bool moveDown;
	bool perspective;


	public float MaxOrthoSize => preferredOrthographicSize < maxOrthographicSize ? preferredOrthographicSize : maxOrthographicSize;
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
	public ParticleSystem confettiRight;
	public ParticleSystem confettiLeft;

	protected override void Awake() {
		base.Awake();
		Camera = GetComponent<Camera>();
		Shaker = new GameObject("Camera Holder").AddComponent<CameraShaker>();
		Shaker.SetCameraTransform(Shaker.transform);
		transform.SetParent(Shaker.transform);
		if(FindObjectOfType<GameManager>() != null) {
			SceneManager.MoveGameObjectToScene(Shaker.gameObject, SceneManager.GetSceneByName("Level"));
		}
		ChangeState(GameCamState.Overview);
	}

	void LateUpdate() {
		switch(cameraState) {
			case GameCamState.Free:
				return;
			case GameCamState.Focus:
				FollowPlayer();
				break;
		}


		// Debug
		if(moveLeft) {
			transform.position += new Vector3(-1, 0, 0) * debugSpeed * Time.deltaTime;
		}
		if(moveRight) {
			transform.position += new Vector3(1, 0, 0) * debugSpeed * Time.deltaTime;
		}
		if(moveDown) {
			transform.position += new Vector3(0, 0, -1) * debugSpeed * Time.deltaTime;
		}
		if(moveUp) {
			transform.position += new Vector3(0, 0, 1) * debugSpeed * Time.deltaTime;
		}
	}

	#region Debug
	public void MoveLeft() {
		moveLeft = !moveLeft;
	}
	public void MoveRight() {
		moveRight = !moveRight;
	}
	public void MoveForward() {
		moveUp = !moveUp;
	}
	public void MoveBackward() {
		moveDown = !moveDown;
	}
	public void ZoomIn() {
		Camera.orthographicSize += 0.2f;
	}
	public void ZoomOut() {
		Camera.orthographicSize -= 0.2f;
	}
	public void SwitchPerspective() {
		if(perspective) {
			Camera.orthographic = false;
			perspective = false;
		} else {
			Camera.orthographic = true;
			perspective = true;
		}
	}
	#endregion

	void FollowPlayer() {
		if(Player != null) {
			if(preferredOrthographicSize < maxOrthographicSize) {
				
			}
			Vector3 current = Camera.main.transform.position;
			Vector3 avg = Vector3.zero;
			Vector3 zoom = Vector3.zero;
			float ortho = 0;
			var tanks = FindObjectsOfType<TankAI>();
			int active = 1;
			foreach(TankAI t in tanks) {
				if(t.HasBeenDestroyed == false) {
					avg += new Vector3(t.Pos.x, 0, t.Pos.z);
					zoom -= t.Pos;
					ortho += Vector3.Distance(t.Pos, FindObjectOfType<PlayerInput>().Pos);
					active++;
				}
			}
			Vector3 playerPos = FindObjectOfType<PlayerInput>().Pos;
			playerPos.y = 0;
			avg += playerPos;
			avg /= active;
			avg += offset;
			avg += new Vector3(0, 30, -30);
			playerPos += new Vector3(0, 30, -30) + offset;

			Camera.main.transform.position = Vector3.Lerp(current, avg, Time.deltaTime * focusSmoothSpeed);
			Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, playerPos, Time.deltaTime * focusSmoothSpeed * focusOnPlayerStrength);
			ortho /= active;
			if(ortho < minOrthographicSize) {
				ortho = minOrthographicSize;
			} else if(ortho > MaxOrthoSize) {
				ortho = MaxOrthoSize;
			}
			Camera.orthographicSize = Mathf.Lerp(Camera.orthographicSize, ortho, Time.deltaTime * focusZoomSmoothSpeed);
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
				preferredOrthographicSize = LevelManager.GetOrthographicSize(LevelManager.GridSize);
				FollowPlayer();
				break;
		}
	}

	public static void ShakeExplosion2D(float strength, float duration) {
		CameraShaker.Presets.Explosion2D(strength, strength, duration);
	}

	public static void ShakeExplosion3D(float strength, float duration) {
		CameraShaker.Presets.Explosion2D(strength, duration);
	}

	public static void ShortShake3D(float strength, float frequency = 25, int numBounces = 5) {
		CameraShaker.Presets.ShortShake3D(strength, frequency, numBounces);
	}

	public static void ShortShake2D(float strength, float frequency = 25, int numBounces = 5) {
		CameraShaker.Presets.ShortShake2D(strength, strength, frequency, numBounces);
	}

	public void PlayConfetti() {
		confettiLeft.Play();
		confettiRight.Play();
		AudioPlayer.Play("ConfettiGun", AudioType.SoundEffect, 0.9f, 1.1f, 3f);
		AudioPlayer.Play("Clapping", AudioType.SoundEffect, 0.9f, 1.1f, 3f);
	}
}
