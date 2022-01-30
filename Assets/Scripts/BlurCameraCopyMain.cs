using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class BlurCameraCopyMain : MonoBehaviour {

	public Camera copyFrom;
	[Min(1)]
	public int refreshRate = 1;
	private Camera target;

	private void Awake() {
		target = GetComponent<Camera>();
		if(copyFrom == null) {
			copyFrom = GetComponentInParent<Camera>();
		}
	}

	private void LateUpdate() {
		if(Time.frameCount % refreshRate == 0) {
			Copy(target, copyFrom);
		}
	}

	public void Copy(Camera copyTo, Camera copyFrom) {
        copyTo.orthographicSize = copyFrom.orthographicSize;
		copyTo.orthographic = copyFrom.orthographic;
        copyTo.transform.position = copyFrom.transform.position;
        copyTo.transform.rotation = copyFrom.transform.rotation;
        copyTo.nearClipPlane = copyFrom.nearClipPlane;
        copyTo.farClipPlane = copyFrom.farClipPlane;
        copyTo.fieldOfView = copyFrom.fieldOfView;
    }

}
