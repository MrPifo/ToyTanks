using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletRejector : MonoBehaviour {

	[HideInInspector]
	public Bullet bullet;

	void Awake() {
		bullet = GetComponentInParent<Bullet>();
	}

	void OnCollisionEnter(Collision target) {
		target.transform.GetComponent<BulletRejector>().bullet.Destroy();
		bullet.Destroy();
	}

}
