using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankTrack : MonoBehaviour {

	public float DeleteTrackTime;

	void Awake() {
		StartCoroutine(DeleteTrack());
	}

	public void ReceiveDestroy() {
		Destroy(gameObject);
	}

	IEnumerator DeleteTrack() {
		yield return new WaitForSeconds(DeleteTrackTime);
	}
}
