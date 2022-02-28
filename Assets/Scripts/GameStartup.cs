using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

public class GameStartup : MonoBehaviour {

	[SerializeField] Canvas backgroundCanvas;
	[SerializeField] CanvasGroup canvasGroup;
	[SerializeField] CanvasGroup backgroundGroup;
	[SerializeField] public TextMeshProUGUI loadingText;
	[SerializeField] Texture2D defaultCursor;
	[SerializeField] Texture2D pointerCursor;
	public Slider loadingBar;
	private float currentProgress;
	private int loadingSteps = 5;
	private int currentLoadingStep = 1;
	public static GameStartup Instance;
	public static TextMeshProUGUI LoadingText => Instance.loadingText;
	int waitTime = 500;	// 0.5s wait time between loading steps

	private void Start() {
		Instance = this;
		_ = LoadGame();
	}

	public async Task LoadGame() {
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

			Addressables.Initialize();
			await Task.Delay(waitTime);
			loadingText.SetText("Initializing Game.");
			Logger.Log(Channel.Loading, "Initializing Game");

			loadingText.SetText("Loading Assets");
			try {
				Logger.Log(Channel.Loading, "Loading Assets.");
				Game.AddCursor("default", defaultCursor);
				Game.AddCursor("pointer", pointerCursor);
				Game.SetCursor("default");
			} catch {
				loadingText.SetText("Failed loading cursor textures.");
				await Task.Delay(1000);
			}
			NextLoadingStep();

			await Game.Initialize(true);
			NextLoadingStep();

			await Task.Delay(waitTime);
			loadingText.SetText("Loading Menu");
			Logger.Log(Channel.Loading, "Loading Menu.");
			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Additive);
			asyncLoad.allowSceneActivation = false;
			NextLoadingStep();

			await TaskEx.WaitWhile(() => asyncLoad.progress < 0.9f);
			asyncLoad.allowSceneActivation = true;
			await Task.Delay(waitTime);
			loadingText.SetText("Loading complete");
			NextLoadingStep();

			await Task.Delay(1500);
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
