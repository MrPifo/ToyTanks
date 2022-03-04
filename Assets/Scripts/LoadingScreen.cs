using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using ToyTanks.UI;
using SimpleMan.Extensions;
using System.Threading.Tasks;

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

	private void Awake() {
		screen.alpha = 0;
		banner.alpha = 0;
	}

	public async Task FadeIn(float duration, Ease ease = Ease.Linear) {
		Debug.Log("0");
		background.transform.localScale = Vector3.zero;
		Debug.Log("1");
		background.DOScale(1, duration / 3f).SetEase(ease);
		Debug.Log("2");

		var randomMenu = FindObjectOfType<MenuItem>();
		if(randomMenu != null) {
			randomMenu.TriggerFlashTransition();
		}
		Debug.Log("3");

		await DOTween.To(() => screen.alpha, x => screen.alpha = x, 1, duration).SetEase(ease).AsyncWaitForCompletion();
	}

	public async Task FadeOut(float duration, Ease ease = Ease.Linear) {
		background.transform.localScale = Vector3.one;
		background.DOScale(0, duration / 3f).SetEase(ease);
		await DOTween.To(() => screen.alpha, x => screen.alpha = x, 0, duration).SetEase(ease).AsyncWaitForCompletion();
	}

	public async Task FadeInBanner(float duration, Ease ease = Ease.Linear) {
		await DOTween.To(() => banner.alpha, x => banner.alpha = x, 1, duration).SetEase(ease).AsyncWaitForCompletion();
	}

	public async Task FadeOutBanner(float duration, Ease ease = Ease.Linear) {
		await DOTween.To(() => banner.alpha, x => banner.alpha = x, 0, duration).SetEase(ease).AsyncWaitForCompletion();
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
				this.lives.Stretch(1f, 1.2f, 0.5f);
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
