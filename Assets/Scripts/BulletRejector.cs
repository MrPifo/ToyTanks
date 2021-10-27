using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletRejector : MonoBehaviour {

	public IHittable bullet;
	public IDamageEffector damageEffector;

	void Awake() {
		bullet = transform.SearchComponent<IHittable>();
		damageEffector = transform.SearchComponent<IDamageEffector>();
	}

	void OnCollisionEnter(Collision target) {
		target.transform.GetComponent<BulletRejector>().bullet.TakeDamage(damageEffector);
		bullet.TakeDamage(damageEffector);
	}

}
