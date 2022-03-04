using UnityEngine;
using TMPro;
using DG.Tweening;
using SimpleMan.Extensions;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LevelUI : MonoBehaviour {

	public Color32 promptColor;
	public GameObject counterBanner;
	public CanvasGroup gameplay;
	public CanvasGroup transitionScreen;
	public RectTransform transitionMask;
	public TMP_Text playerScore;
	public TMP_Text playerLives;
	public TMP_Text levelStage;
	public TMP_Text playTime;
	public TMP_Text loadingScreenText;
	public Text bannerText;
	public Button graphicsButton;
	public GameObject pauseBlur;
	public GameObject bannerBlur;
	public GameObject tutorial;
	public List<ButtonPrompt> buttons = new List<ButtonPrompt>();
	
	Vector3 bannerPos;

	void Awake() {
		gameplay.alpha = 0f;
		counterBanner.gameObject.SetActive(false);
		bannerPos = bannerText.transform.position;
		tutorial.gameObject.SetActive(false);
		bannerBlur.Hide();
		pauseBlur.Hide();
		graphicsButton.onClick.RemoveAllListeners();
		graphicsButton.onClick.AddListener(GraphicSettings.OpenOptionsMenu);
	}

	private void Update() {
		if(Game.IsGamePlaying && Game.IsGameRunningDebug == false && GameManager.LevelId == 1) {
			var player = FindObjectOfType<PlayerInput>();

			SetButtonColor("W", Color.white);
			SetButtonColor("A", Color.white);
			SetButtonColor("S", Color.white);
			SetButtonColor("D", Color.white);
			SetButtonColor("MouseLeft", Color.white);
			if(player != null) {
				if(player.moveVector.x > 0) {
					SetButtonColor("A", promptColor);
				} else if(player.moveVector.x < 0) {
					SetButtonColor("D", promptColor);
				}
				if(player.moveVector.y > 0) {
					SetButtonColor("W", promptColor);
				} else if(player.moveVector.y < 0) {
					SetButtonColor("S", promptColor);
				}
				if(player.player.GetButton("Shoot")) {
					SetButtonColor("MouseLeft", promptColor);
				}
			}
		}
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
		bannerText.text = "3";
		bannerText.transform.localScale = Vector3.one;
		bannerText.transform.position = new Vector3(bannerPos.x - 1000, bannerPos.y, bannerPos.z);
		bannerText.transform.DOMoveX(bannerPos.x, speed / 2f);
		counterBanner.transform.localScale = new Vector3(1, 0, 1);
		counterBanner.transform.DOScaleY(1, 0.3f);
		bannerBlur.Show();

		this.Delay(1f, () => bannerText.text = "2");
		this.Delay(2f, () => bannerText.text = "1");
		this.Delay(3f, () => {
			bannerText.text = "Start";
			bannerText.transform.DOMoveX(bannerPos.x + 2000, speed / 2f);
			bannerText.Stretch(1f, 1.25f, 0.25f);
			counterBanner.transform.DOScaleY(0, 0.3f).OnComplete(() => {
				counterBanner.gameObject.SetActive(false);
				bannerText.transform.position = bannerPos;
				bannerBlur.Hide();
			});
		});
	}

	public async Task ShowTransitionScreen() {
		transitionScreen.DOFade(1f, 0.5f).SetEase(Ease.OutCubic);
		transitionScreen.alpha = 1;
		loadingScreenText.SetText("");
		transitionMask.sizeDelta = new Vector2(3000, 3000);
		await transitionMask.DOSizeDelta(new Vector2(0, 0), 1f).AsyncWaitForCompletion();
	}

	public async Task HideTransitionScreen() {
		transitionScreen.DOFade(0f, 1f);
		transitionMask.sizeDelta = new Vector2(0, 0);
		await transitionMask.DOSizeDelta(new Vector2(3000, 3000), 1f).AsyncWaitForCompletion();
	}

	public void SetButtonColor(string letter, Color color) {
		buttons.Find(b => b.button == letter)?.SetColor(color);
	}

	[Serializable]
	public class ButtonPrompt {
		public string button;
		public Image sprite;

		public void SetColor(Color col) => sprite.color = col;
	}
}
