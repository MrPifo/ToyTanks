using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TankBase))]
public class PlayerInput : MonoBehaviour {

	TankBase tank;

	void Awake() {
		tank = GetComponent<TankBase>();
	}

	void Update() {
		MoveTank();
	}

	void MoveTank() {
		var moveVector = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

		if(moveVector.x != 0f || moveVector.y != 0f) {
			tank.Move(moveVector);
		}
	}
}
