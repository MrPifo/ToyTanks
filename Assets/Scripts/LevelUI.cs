using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using MoreMountains.Feedbacks;

public class LevelUI : MonoBehaviour {

	public float loadingScreenTransitionSpeed;
	public Camera transitionCamera;
	public GameObject counterBanner;
	public GameObject blurLayer;
	public GameObject loadingScreen;
	public GameObject gameplay;
	public GameObject crossHair;
	public GameObject bossUI;
	public TextMeshProUGUI loadLevelName;
	public TextMeshProUGUI playerScore;
	public TextMeshProUGUI playerLives;
	public TextMeshProUGUI levelStage;
	public TextMeshProUGUI playTime;
	public MMFeedbacks bossUIInitFeedback;
	public MMFeedbacks bossUIHitFeedback;
	public Slider bossBar;
	public CanvasGroup loadingScreenCanvasGroup;
	public Canvas canvas;

	void Awake() {
		gameplay.SetActive(false);
		bossUI.gameObject.SetActive(false);
		crossHair.transform.position = new Vector3(999, crossHair.transform.position.y, 999);
	}

	public void ShowCounter() {
		counterBanner.SetActive(true);
	}

	public void HideCounter() {
		counterBanner.SetActive(false);
	}

	public void InitBossBar(int maxHealth, float initSpeed) {
		bossUI.gameObject.SetActive(true);
		bossBar.maxValue = maxHealth;
		bossBar.value = 0;
		bossBar.DOValue(maxHealth, initSpeed).SetEase(Ease.Linear);
		bossUIInitFeedback.PlayFeedbacks();
	}
	public void SetBossBar(int value, float speed = 1f) {
		bossUIHitFeedback.PlayFeedbacks();
		bossBar.DOValue(value, speed).SetEase(Ease.Linear);
	}

	public void EnableBlur() => blurLayer.SetActive(true);
	public void DisableBlur() => blurLayer.SetActive(false);
}
