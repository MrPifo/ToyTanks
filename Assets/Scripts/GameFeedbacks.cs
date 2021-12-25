using MoreMountains.Feedbacks;
using UnityEngine;

public class GameFeedbacks : Singleton<GameFeedbacks> {

	[SerializeField]
    private MMFeedbacks playerDeath;
	public static MMFeedbacks PlayerDeath => Instance.playerDeath;

}
