using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameStartup : MonoBehaviour {

	[SerializeField] CanvasGroup canvasGroup;
	[SerializeField] TextMeshProUGUI loadingText;
	[SerializeField] Texture2D defaultCursor;
	[SerializeField] Texture2D pointerCursor;
	public Slider loadingBar;
	private float currentProgress;
	private int loadingSteps = 5;
	private int currentLoadingStep = 1;

	private void Start() => StartCoroutine(ILoadGame());

	public IEnumerator ILoadGame() {
		loadingText.SetText("Loading Assets");
		Game.AddCursor("default", defaultCursor);
		Game.AddCursor("pointer", pointerCursor);
		Game.SetCursor("default");
		NextLoadingStep();

		yield return new WaitForSeconds(0.5f);
		loadingText.SetText("Loading Savegame");
		SaveGame.GameStartUp();
		NextLoadingStep();

		yield return new WaitForSeconds(0.5f);
		loadingText.SetText("Preparing Game");
		AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Additive);
		asyncLoad.allowSceneActivation = false;
		NextLoadingStep();

		yield return new WaitUntil(() => asyncLoad.progress >= 0.9f);
		asyncLoad.allowSceneActivation = true;
		yield return new WaitForSeconds(0.5f);
		NextLoadingStep();
		canvasGroup.DOFade(0, 1).OnComplete(() => {
			SceneManager.UnloadSceneAsync(0);
		});
	}

	private void Update() {
		loadingBar.value = Mathf.Lerp(loadingBar.value, currentProgress, Time.deltaTime * 2);
	}

	private void NextLoadingStep() {
		currentLoadingStep++;
		currentProgress = (1f - 1f / currentLoadingStep).Remap(0, 1f - 1f / loadingSteps, 0f, 1f);
	}
}
