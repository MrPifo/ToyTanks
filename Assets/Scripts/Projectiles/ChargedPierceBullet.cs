using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargedPierceBullet : Bullet {

	public HashSet<IHittable> hitHittables;

	public override Bullet SetupBullet(GameObject owner, Vector3 dir, Vector3 startPos, bool firedFromPlayer, float customPitch = 1f) {
		IsHittable = false;
		hitHittables = new HashSet<IHittable>();
		Owner = owner;
		transform.position = startPos;
		Direction = dir;
		isFromPlayer = firedFromPlayer;
		float pitch = customPitch == 1f ? 1.2f : customPitch;
		AudioPlayer.Play(JSAM.Sounds.BulletShot, AudioType.SoundEffect, 0.8f * pitch, 1.2f * pitch, 1f);
		hitHittables.Add(owner.GetComponent<IHittable>());
		return this;
	}

	protected override void OnCollisionEnter(Collision coll) {
		if (time != lastHitTime) {
			coll.transform.TrySearchComponent(out IHittable hittable);
			if (Physics.SphereCast(transform.position, 0.25f, Direction, out RaycastHit hit, Mathf.Infinity, reflectLayers) && lastHitObject != hit.transform.gameObject) {
				bounces++;
				if (hitHittables.Contains(hittable) == false) {
					if (hittable != null && IsBulletBlocker(coll.collider.transform) == false && IsBulletReflector(coll.collider.transform) == false) {
						hitHittables.Add(hittable);
						hittable.TakeDamage(this, true);
						bounces++;
					} else if (bounces > maxBounces) {
						TakeDamage(this, true);
					} else if (IsBulletBlocker(coll.collider.transform) && IsBulletReflector(coll.collider.transform) == false) {
						AudioPlayer.Play(JSAM.Sounds.BulletRicochet, AudioType.SoundEffect, 0.8f * Pitch, 1.2f * Pitch, 1.5f);
						TakeDamage(this, true);
					} else {
						Reflect(hit.normal);
						lastHitTime = time;
						lastHitObject = hit.transform.gameObject;
					}
				}
			}
		}
	}

}
