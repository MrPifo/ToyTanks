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
	public LevelSelector levelSelect;
	public bool isLoading;
	UnityAction<Scene, LoadSceneMode> levelBaseLoadAction;

	void Awake() {
		levelSelect.gameObject.SetActive(false);
		mainMenu.SetActive(true);
	}

	void FixedUpdate() {
		Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, camTarget, Time.fixedDeltaTime * cameraTransitionSpeed);
	}

	public void StartGame() {
		SceneManager.LoadScene("LevelBase", LoadSceneMode.Additive);
		levelBaseLoadAction = (Scene scene, LoadSceneMode mode) => {
			SceneManager.sceneLoaded -= levelBaseLoadAction;
			FindObjectOfType<GameManager>().StartCampaign();
		};
		SceneManager.sceneLoaded += levelBaseLoadAction;
	}

	public void ShowLobbyStartGameButton() {
		startGameButton.SetActive(true);
	}

	public void HideLobbyStartGameButton() {
		startGameButton.SetActive(false);
	}
}
