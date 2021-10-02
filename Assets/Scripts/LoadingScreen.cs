using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LoadingScreen : MonoBehaviour {

	public CanvasGroup screen;
	public CanvasGroup banner;
	public Image liveIcon;
	public TMP_Text level;
	public TMP_Text lives;
	public TMP_Text singleMessage;
	public Slider progressBar;
	public float value;
	public bool onFadeInFinished;
	public bool onFadeOutFinished;
	public bool onBannerFadeInFinished;
	public bool onBannerFadeOutFinished;

	private void Awake() {
		screen.alpha = 0;
		banner.alpha = 0;
	}

	public void FadeIn(float duration, Ease ease = Ease.Linear) {
		DOTween.To(() => screen.alpha, x => screen.alpha = x, 1, duration).SetEase(ease).OnComplete(() => {
			onFadeInFinished = true;
		});
	}

	public void FadeOut(float duration, Ease ease = Ease.Linear) {
		DOTween.To(() => screen.alpha, x => screen.alpha = x, 0, duration).SetEase(ease).OnComplete(() => {
			onFadeOutFinished = true;
		});
	}

	public void FadeInBanner(float duration, Ease ease = Ease.Linear) {
		DOTween.To(() => banner.alpha, x => banner.alpha = x, 1, duration).SetEase(ease).OnComplete(() => {
			onBannerFadeInFinished = true;
		});
	}

	public void FadeOutBanner(float duration, Ease ease = Ease.Linear) {
		DOTween.To(() => banner.alpha, x => banner.alpha = x, 0, duration).SetEase(ease).OnComplete(() => {
			onBannerFadeOutFinished = true;
		});
	}

	public void SetProgress(float value) {
		progressBar.value = value;
	}

	public void SetInfo(string level, string lives) {
		this.level.gameObject.SetActive(true);
		this.lives.gameObject.SetActive(true);
		liveIcon.gameObject.SetActive(true);
		singleMessage.gameObject.SetActive(false);
		this.level.SetText(level);
		this.lives.SetText(lives);
	}

	public void SetSingleMessage(string message) {
		level.gameObject.SetActive(false);
		lives.gameObject.SetActive(false);
		liveIcon.gameObject.SetActive(false);
		singleMessage.gameObject.SetActive(true);
		singleMessage.SetText(message);
	}
}
