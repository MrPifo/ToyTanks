using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ToyTanks.UI;
using SimpleMan.Extensions;

public class LoadingScreen : MonoBehaviour {

	public CanvasGroup screen;
	public CanvasGroup banner;
	public Transform background;
	public Image liveIcon;
	public TMP_Text level;
	public Text lives;
	public Text singleMessage;
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
		background.transform.localScale = Vector3.zero;
		background.DOScale(1, duration / 3f).SetEase(ease);

		var randomMenu = FindObjectOfType<MenuItem>();
		if(randomMenu != null) {
			randomMenu.TriggerFlashTransition();
		}
	}

	public void FadeOut(float duration, Ease ease = Ease.Linear) {
		DOTween.To(() => screen.alpha, x => screen.alpha = x, 0, duration).SetEase(ease).OnComplete(() => {
			onFadeOutFinished = true;
		});
		background.transform.localScale = Vector3.one;
		background.DOScale(0, duration / 3f).SetEase(ease);
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

	public void SetInfo(string level, byte lives) {
		this.level.gameObject.SetActive(true);
		this.lives.gameObject.SetActive(true);
		liveIcon.gameObject.SetActive(true);
		singleMessage.gameObject.SetActive(false);
		this.level.text = level;
		if(GameManager.rewardLive == false) {
			this.lives.text = (lives) + "";
		} else {
			GameManager.rewardLive = false;
			this.lives.text = (lives - 1) + "";
			this.Delay(2, () => {
				this.lives.text = (lives) + "";
				this.lives.Stretch(1.2f, 0.5f);
			});
		}
	}

	public void SetSingleMessage(string message) {
		level.gameObject.SetActive(false);
		lives.gameObject.SetActive(false);
		liveIcon.gameObject.SetActive(false);
		singleMessage.gameObject.SetActive(true);
		singleMessage.text = message;
	}
}
