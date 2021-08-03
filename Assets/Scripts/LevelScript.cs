using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CarterGames.Assets.AudioManager;

public class LevelScript : MonoBehaviour {

	public GameObject userPrefab;
	public AudioManager audioManager;
	public Transform spawnpoint;
	public Transform trackContainer;
	public int maxTracksOnStage = 100;
	[Header("UI")]
	public GameObject endscreenUI;
	public TextMeshProUGUI ready_button;
	public GameObject next_level_button;

	public void Awake() {
		ready_button.text = "READY";
		endscreenUI.SetActive(false);

		// Must be called before TankBase Script
		trackContainer = new GameObject("TrackContainer").transform;
	}

	void Update() {
		CheckTankTracks();
	}

	public void StartGame() {

	}

	void CheckTankTracks() {
		if(trackContainer.childCount > maxTracksOnStage) {
			Destroy(trackContainer.GetChild(0).gameObject);
		}
	}
}
