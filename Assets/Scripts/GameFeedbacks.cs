using MoreMountains.Feedbacks;
using Sperlich.PrefabManager;
using UnityEngine;

public class GameFeedbacks : MonoBehaviour {

	[SerializeField]
    private MMFeedbacks playerDeath;
	[SerializeField]
	private MMFeedbacks playerHit;
	//public static MMFeedbacks PlayerDeath => Instance.playerDeath;
	//public static MMFeedbacks PlayerHit => Instance.playerHit;
	private static GameFeedbacks _instance;
	public static GameFeedbacks Instance {
		get {
			if(_instance == null) {
				//PlayerDeath.Initialization();
				//PlayerHit.Initialization();
				if(FindObjectOfType<GameFeedbacks>()) {
					_instance = FindObjectOfType<GameFeedbacks>();
				} else {
					_instance = PrefabManager.Instantiate<GameFeedbacks>(PrefabTypes.GameFeedbacks);
				}
			}
			return _instance;
		}
	}

}
