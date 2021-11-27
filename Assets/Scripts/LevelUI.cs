using UnityEngine;
using TMPro;

using DG.Tweening;
using MoreMountains.Feedbacks;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

public class LevelUI : MonoBehaviour {

	public float loadingScreenTransitionSpeed;
	public Camera transitionCamera;
	public GameObject counterBanner;
	public GameObject blurLayer;
	public GameObject loadingScreen;
	public CanvasGroup gameplay;
	public TextMeshProUGUI loadLevelName;
	public TextMeshProUGUI playerScore;
	public TextMeshProUGUI playerLives;
	public TextMeshProUGUI levelStage;
	public TextMeshProUGUI playTime;
	public CanvasGroup loadingScreenCanvasGroup;
	public Canvas canvas;
	[SerializeField] CustomPassVolume outlinePass;
	public Outline OutlinePass => (Outline)outlinePass.customPasses[0];

	void Awake() {
		gameplay.alpha = 0f;
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
