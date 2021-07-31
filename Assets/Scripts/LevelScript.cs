using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LevelScript : MonoBehaviour {

	public GameObject userPrefab;
	public Transform spawnpoint;
	[Header("UI")]
	public GameObject endscreenUI;
	public TextMeshProUGUI ready_button;
	public GameObject next_level_button;

	public void Awake() {
		ready_button.text = "READY";
		endscreenUI.SetActive(false);
	}
	public void StartGame() {

	}
}
