using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class MenuManager : MonoBehaviour {
	public float cameraTransitionSpeed;
	private Vector3 camTarget;
	public GameObject lobbyMenu;
	public GameObject mainMenu;
	public GameObject startGameButton;
	public bool isLoading;
	UnityAction<Scene, LoadSceneMode> levelBaseLoadAction;

	void Awake() {
		mainMenu.SetActive(true);
	}

	public void StartGame() => FindObjectOfType<GameManager>().StartCampaign();

	public void ShowLobbyStartGameButton() {
		startGameButton.SetActive(true);
	}

	public void HideLobbyStartGameButton() {
		startGameButton.SetActive(false);
	}
}
