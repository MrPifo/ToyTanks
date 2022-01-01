using MoreMountains.Feedbacks;
using UnityEngine;

public class GameFeedbacks : Singleton<GameFeedbacks> {

	[SerializeField]
    private MMFeedbacks playerDeath;
	[SerializeField]
	private MMFeedbacks playerHit;
	public static MMFeedbacks PlayerDeath => Instance.playerDeath;
	public static MMFeedbacks PlayerHit => Instance.playerHit;

}
