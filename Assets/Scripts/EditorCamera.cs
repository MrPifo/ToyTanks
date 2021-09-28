using System.Collections;
using ToyTanks.LevelEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ToyTanks.LevelEditor {
	[RequireComponent(typeof(Camera))]
	public class EditorCamera : MonoBehaviour {

		public Slider distanceSlider;
		public LevelEditor editor;
		public float moveSpeed;
		[SerializeField] private Transform target;
		[SerializeField] private float distanceToTarget = 50;
		[SerializeField] private float orthographicSize = 15;
		[SerializeField] private float fov;
		public bool LockControls { get; set; }
		public bool DisableController { get; set; }
		public Camera Camera { get; set; }
		public Vector3 Offset { get; set; }
		float aspect;
		Matrix4x4 ortho;
		Matrix4x4 perspective;
		Vector3 previousPosition;
		bool hasBeenInitialized;

		public void Initialize() {
			hasBeenInitialized = true;
			Camera = GetComponent<Camera>();
			aspect = (float)Screen.width / Screen.height;
			ortho = Matrix4x4.Ortho(-orthographicSize * aspect, orthographicSize * aspect, -orthographicSize, orthographicSize, 0.2f, 200);
			perspective = Matrix4x4.Perspective(fov, aspect, 0.2f, 200);
			Camera.projectionMatrix = ortho;
		}

		void Update() {
			if(DisableController || !hasBeenInitialized || !editor.HasLevelBeenLoaded) {
				return;
			}
			Vector3 newPosition = Camera.ScreenToViewportPoint(Input.mousePosition);
			if(Input.GetKeyDown(KeyCode.Mouse1)) {
				previousPosition = Camera.ScreenToViewportPoint(Input.mousePosition);
			} else if(Input.GetKey(KeyCode.Mouse1) && !LockControls) {
				Vector3 direction = previousPosition - newPosition;
				float rotationAroundYAxis = -direction.x * 180; // camera moves horizontally
				float rotationAroundXAxis = direction.y * 180; // camera moves vertically
				var originalRot = Camera.transform.rotation;
				var originalPos = Camera.transform.position;
				Camera.transform.Rotate(new Vector3(1, 0, 0), rotationAroundXAxis);
				Camera.transform.Rotate(new Vector3(0, 1, 0), rotationAroundYAxis, Space.World);

				if(Camera.transform.rotation.eulerAngles.x < 5 && rotationAroundXAxis < 0 || Camera.transform.rotation.eulerAngles.x > 70 && rotationAroundXAxis > 0) {
					Camera.transform.rotation = originalRot;
					Camera.transform.position = originalPos;
				}
			}


			float dist = LevelEditor.Remap(distanceSlider.value, 0, 1, distanceToTarget / 2f, distanceToTarget);
			Camera.transform.position = target.position + Offset;
			Camera.transform.Translate(new Vector3(0, 0, -dist));
			previousPosition = newPosition;

			Vector3 moveOffset = Vector3.zero;
			float amount = Time.deltaTime * moveSpeed;
			if(Input.GetKey(KeyCode.A)) {
				moveOffset -= Camera.transform.right;
			} else if(Input.GetKey(KeyCode.D)) {
				moveOffset += Camera.transform.right;
			}

			if(Input.GetKey(KeyCode.W)) {
				moveOffset += Camera.transform.forward;
			} else if(Input.GetKey(KeyCode.S)) {
				moveOffset -= Camera.transform.forward;
			}

			moveOffset.y = 0;
			var newPos = Camera.transform.position + Offset + moveOffset * amount;

			float newDistance = Vector3.Distance(Vector3.zero, new Vector3(newPos.x, 0, newPos.z));
			float maxDistance = Vector2.Distance(Vector2.zero, new Vector2(LevelManager.GridBoundary.x * 2.2f, LevelManager.GridBoundary.z * 2.2f));

			if(newDistance > maxDistance) {
				moveOffset -= (Camera.transform.position - Vector3.zero).normalized * Mathf.Clamp(newDistance - 50, 0f, 2f);
				moveOffset.y = 0;
			}
			Offset += moveOffset * amount;
		}

		IEnumerator LerpFromTo(Matrix4x4 src, Matrix4x4 dest, float duration) {
			float startTime = Time.time;
			while(Time.time - startTime < duration) {
				Camera.projectionMatrix = MatrixLerp(src, dest, (Time.time - startTime) / duration);
				yield return 1;
			}
			Camera.projectionMatrix = dest;
		}

		Coroutine BlendToMatrix(Matrix4x4 targetMatrix, float duration) {
			StopAllCoroutines();
			return StartCoroutine(LerpFromTo(Camera.projectionMatrix, targetMatrix, duration));
		}

		public void LerpToOrtho(int duration, float orthographicSize) {
			ortho = Matrix4x4.Ortho(-orthographicSize * aspect, orthographicSize * aspect, -orthographicSize, orthographicSize, 0.2f, 200);
			BlendToMatrix(ortho, duration);
		}

		public void LerpToPerspective(int duration) {
			BlendToMatrix(perspective, duration);
		}

		static Matrix4x4 MatrixLerp(Matrix4x4 from, Matrix4x4 to, float time) {
			Matrix4x4 ret = new Matrix4x4();
			for(int i = 0; i < 16; i++)
				ret[i] = Mathf.Lerp(from[i], to[i], time);
			return ret;
		}
	}
}