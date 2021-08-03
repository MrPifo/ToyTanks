using SimpleMan.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructionTimer : MonoBehaviour {

	void Awake() {
		name = "Temp_Container";
	}

	void SetChild(GameObject o) {
		o.transform.SetParent(transform);
	}

	public void Destruct(float time, GameObject[] childs) {
		foreach(GameObject o in childs) {
			o.transform.SetParent(transform);
		}
		this.Delay(time, () => {
			Destroy(gameObject);
		});
	}
}
