using UnityEngine;
using TMPro;

public class LevelUI : MonoBehaviour {

	public float loadingScreenTransitionSpeed;
	public Camera transitionCamera;
	public GameObject counterBanner;
	public GameObject blurLayer;
	public GameObject loadingScreen;
	public GameObject gameplay;
	public GameObject crossHair;
	public TextMeshProUGUI loadLevelName;
	public TextMeshProUGUI playerScore;
	public TextMeshProUGUI playerLives;
	public TextMeshProUGUI levelStage;
	public CanvasGroup loadingScreenCanvasGroup;
	public Canvas canvas;

	void Awake() {
		gameplay.SetActive(false);
		crossHair.transform.position = new Vector3(999, crossHair.transform.position.y, 999);
	}

	public void ShowCounter() {
		counterBanner.SetActive(true);
	}

	public void HideCounter() {
		counterBanner.SetActive(false);
	}

	public void EnableBlur() => blurLayer.SetActive(true);
	public void DisableBlur() => blurLayer.SetActive(false);
}
