using UnityEngine;
using TMPro;
using DG.Tweening;
using SimpleMan.Extensions;
using UnityEngine.UI;

public class LevelUI : MonoBehaviour {

	public GameObject counterBanner;
	public CanvasGroup gameplay;
	public TMP_Text playerScore;
	public TMP_Text playerLives;
	public TMP_Text levelStage;
	public TMP_Text playTime;
	public Text bannerText;
	public GameObject pauseBlur;
	public GameObject bannerBlur;
	Vector3 bannerPos;

	void Awake() {
		gameplay.alpha = 0f;
		counterBanner.gameObject.SetActive(false);
		bannerPos = bannerText.transform.position;
		bannerBlur.Hide();
		pauseBlur.Hide();
	}

	public void ShowCounter() {
		counterBanner.SetActive(true);
	}

	public void HideCounter() {
		counterBanner.SetActive(false);
	}

	public void ShowGameplayUI(float speed = 0.5f) {
		gameplay.DOFade(1, speed);
	}

	public void HideGameplayUI(float speed = 0.5f) {
		gameplay.DOFade(0, speed);
	}

	public void PlayBannerAnimation() {
		float speed = 2f;
		counterBanner.gameObject.SetActive(true);
		bannerText.transform.localScale = Vector3.one;
		bannerText.transform.position = new Vector3(bannerPos.x - 1000, bannerPos.y, bannerPos.z);
		bannerText.transform.DOMoveX(bannerPos.x, speed / 2f);
		counterBanner.transform.localScale = new Vector3(1, 0, 1);
		counterBanner.transform.DOScaleY(1, 0.3f);
		bannerBlur.Show();

		this.Delay(speed + 1, () => {
			bannerText.transform.DOMoveX(bannerPos.x + 2000, speed / 2f);
			bannerText.Stretch(1f, 1.25f, 0.25f);
			counterBanner.transform.DOScaleY(0, 0.3f).OnComplete(() => {
				counterBanner.gameObject.SetActive(false);
				bannerText.transform.position = bannerPos;
				bannerBlur.Hide();
			});
		});
	}
}
