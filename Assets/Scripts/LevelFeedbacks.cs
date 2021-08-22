using MoreMountains.Feedbacks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelFeedbacks : MonoBehaviour {

	[SerializeField] MMFeedbacks playerExplode;
	[SerializeField] MMFeedbacks tankExplode;
	[SerializeField] MMFeedbacks bulletShoot;
	[SerializeField] MMFeedbacks bulletReflect;
	[SerializeField] MMFeedbacks bulletExplode;
	[SerializeField] MMFeedbacks playerScore;
	[SerializeField] MMFeedbacks playerLives;

	public void PlayBulletShoot() => bulletShoot.PlayFeedbacks();
	public void PlayBulletReflect() => bulletReflect.PlayFeedbacks();
	public void PlayBulletExplode() => bulletExplode.PlayFeedbacks();
	public void TankExplode() => tankExplode.PlayFeedbacks();
	public void PlayerDead() => playerExplode.PlayFeedbacks();
	public void PlayScore() => playerScore.PlayFeedbacks();
	public void PlayLives() => playerLives.PlayFeedbacks();
}
