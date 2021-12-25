using UnityEngine;

[System.Serializable]
public class MenuCameraSettings {

	public Vector3 pos;
	public Vector3 rot;
	public float orthograpicSize;

	public void ApplySettings(Camera cam) {
		cam.transform.position = pos;
		cam.transform.eulerAngles = rot;
		cam.orthographicSize = orthograpicSize;
	}

}
