using DG.Tweening;
using SimpleMan.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ToyTanks.UI {
	[RequireComponent(typeof(CanvasGroup))]
	public class MenuItem : MonoBehaviour {

		Canvas canvas;
		CanvasGroup canvasGroup;
		CanvasScaler scaler;
		CanvasGroup flashScreen;
		public List<MenuItem> menus;

		public void Initialize() {
			canvas = GetComponent<Canvas>();
			canvasGroup = GetComponent<CanvasGroup>();
			scaler = GetComponent<CanvasScaler>();
			flashScreen = GameObject.FindGameObjectWithTag("TransitionFlashScreen").GetComponent<CanvasGroup>();
			canvasGroup.alpha = 0;
			canvas.enabled = false;
		}

		public void TransitionMenu(int menu) {
			this.Delay(MenuManager.fadeDuration, () => menus[menu].FadeIn());
			FadeOut();
			TriggerFlashTransition();
		}

		public void FadeIn() {
			canvas.enabled = true;
			canvasGroup.alpha = 0;
			DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1, MenuManager.fadeDuration).SetEase(Ease.Linear);
		}

		public void FadeOut() {
			canvasGroup.alpha = 1;
			DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0, MenuManager.fadeDuration).SetEase(Ease.Linear).OnComplete(() => {
				canvas.enabled = false;
			});
		}

		public void TriggerFlashTransition() {
			flashScreen.alpha = 0;
			DOTween.To(() => flashScreen.alpha, x => flashScreen.alpha = x, 1, MenuManager.fadeDuration / 4f).SetEase(Ease.Linear).OnComplete(() => {
				this.Delay(MenuManager.fadeDuration / 2f, () => {
					DOTween.To(() => flashScreen.alpha, x => flashScreen.alpha = x, 0, MenuManager.fadeDuration / 2f);
				});
			});
		}
	}
}