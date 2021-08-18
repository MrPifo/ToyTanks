using MoreMountains.Feedbacks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelFeedbacks : MonoBehaviour {

	[SerializeField] MMFeedbacks tankExplode;
	[SerializeField] MMFeedbacks bulletShoot;
	[SerializeField] MMFeedbacks bulletReflect;
	[SerializeField] MMFeedbacks bulletExplode;

	public void PlayBulletShoot() => bulletShoot.PlayFeedbacks();
	public void PlayBulletReflect() => bulletReflect.PlayFeedbacks();
	public void PlayBulletExplode() => bulletExplode.PlayFeedbacks();
	public void PlayTankExplode() {
		tankExplode.PlayFeedbacks();
	}

}
