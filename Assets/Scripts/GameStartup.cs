using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

public class GameStartup : MonoBehaviour {

	[SerializeField] Canvas backgroundCanvas;
	[SerializeField] CanvasGroup canvasGroup;
	[SerializeField] CanvasGroup backgroundGroup;
	[SerializeField] TextMeshProUGUI loadingText;
	[SerializeField] Texture2D defaultCursor;
	[SerializeField] Texture2D pointerCursor;
	public Slider loadingBar;
	private float currentProgress;
	private int loadingSteps = 5;
	private int currentLoadingStep = 1;
	float waitTime = 0.5f;

	private void Start() => StartCoroutine(ILoadGame());

	public IEnumerator ILoadGame() {
		if(Game.ApplicationInitialized == false) {
			// Checking permissions
#if PLATFORM_ANDROID
			if(Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead) == false) {
				Permission.RequestUserPermission(Permission.ExternalStorageRead);
			}
			if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite) == false) {
				Permission.RequestUserPermission(Permission.ExternalStorageWrite);
			}
#endif

			loadingText.SetText("Loading Assets");
			try {
				Logger.Log(Channel.Loading, "Loading Assets.");
				Game.AddCursor("default", defaultCursor);
				Game.AddCursor("pointer", pointerCursor);
				Game.SetCursor("default");
			} catch {
				loadingText.SetText("Failed loading cursor textures.");
				yield break;
			}
			NextLoadingStep();
			yield return new WaitForSeconds(waitTime);
			loadingText.SetText("Initializing Game.");
			Logger.Log(Channel.Loading, "Initializing Game");
			string error = string.Empty;
			try {
				error = Game.Initialize();
				if(error != string.Empty) {
					throw new System.Exception("Failed to initialize Game.");
				}
			} catch {
				loadingText.SetText(error);
				yield break;
			}
			NextLoadingStep();

			yield return new WaitForSeconds(waitTime);
			loadingText.SetText("Loading Menu");
			Logger.Log(Channel.Loading, "Loading Menu.");
			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Additive);
			asyncLoad.allowSceneActivation = false;
			NextLoadingStep();

			yield return new WaitUntil(() => asyncLoad.progress >= 0.9f);
			asyncLoad.allowSceneActivation = true;
			yield return new WaitForSeconds(waitTime);
			loadingText.SetText("Loading complete");
			NextLoadingStep();

			yield return new WaitForSeconds(1.5f);
			backgroundGroup.DOFade(0, 1);
			canvasGroup.DOFade(0, 1).OnComplete(() => {
				SceneManager.UnloadSceneAsync(0);
			});
		}
	}

	private void Update() {
		loadingBar.value = Mathf.Lerp(loadingBar.value, currentProgress, Time.deltaTime * 2);
	}

	private void NextLoadingStep() {
		currentLoadingStep++;
		currentProgress = (1f - 1f / currentLoadingStep).Remap(0, 1f - 1f / loadingSteps, 0f, 1f);
	}
}
