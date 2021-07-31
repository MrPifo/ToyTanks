using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SimpleMan.Extensions;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour {
	public float cameraTransitionSpeed;
	private Vector3 camTarget;
	public GameObject lobbyMenu;
	public GameObject mainMenu;
	public GameObject startGameButton;
	public Transform menuDest;
	public Transform editTankDest;
	public LevelSelector levelSelect;
	public bool isLoading;

	public void Awake() {
		levelSelect.gameObject.SetActive(false);
		mainMenu.SetActive(true);
		camTarget = menuDest.position;
	}
	void FixedUpdate() {
		Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, camTarget, Time.fixedDeltaTime * cameraTransitionSpeed);
	}
	public void StartGame() {
		SceneManager.LoadScene("Level_1");
		SceneManager.sceneLoaded += (Scene scene, LoadSceneMode mode) => {
			FindObjectOfType<LevelScript>().StartGame();
		};
		
	}
	public void ShowLobbyStartGameButton() {
		startGameButton.SetActive(true);
	}
	public void HideLobbyStartGameButton() {
		startGameButton.SetActive(false);
	}
	public void SwitchToTankEditMenu() {
		camTarget = editTankDest.position;
	}
	public void SwitchBackToMainMenu() {
		camTarget = menuDest.position;
	}
}
