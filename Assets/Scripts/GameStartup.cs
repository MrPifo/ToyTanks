using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

public class GameStartup : MonoBehaviour {

	[SerializeField] CanvasGroup canvasGroup;
	public TMP_Text loadingText;
	public TMP_Text loadingIcon;
	public static GameStartup Instance;

	private void Start() {
		Instance = this;
		Repeat().Forget();
		LoadGame().Forget();

		async UniTaskVoid Repeat() {
			while(true) {
				if(loadingIcon.text.Length < 3) {
					loadingIcon.text += ".";
				} else {
					loadingIcon.text = "";
				}
				await UniTask.Delay(500);
			}
		}
	}

	public async UniTaskVoid LoadGame() {
		if(Game.ApplicationInitialized == false) {
			// Checking permissions
			await SetLoadingText("Checking Permissions");
#if PLATFORM_ANDROID
			if(Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead) == false) {
				Permission.RequestUserPermission(Permission.ExternalStorageRead);
			}
			if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite) == false) {
				Permission.RequestUserPermission(Permission.ExternalStorageWrite);
			}
#endif
			await SetLoadingText("Initializing Game");
			await Game.Initialize();
			AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Menu", LoadSceneMode.Additive);
			asyncLoad.allowSceneActivation = false;

			await UniTask.WaitWhile(() => asyncLoad.progress < 0.9f);
			asyncLoad.allowSceneActivation = true;

			await UniTask.WaitWhile(() => MenuManager.Instance == null);
			MenuManager.Instance.startupTransition.alpha = 1f;
			canvasGroup.DOFade(0, 1f);
			await UniTask.Delay(600);
			MenuManager.Instance.startupTransition.DOFade(0, 1.5f);
			await UniTask.Delay(2000);
			SceneManager.UnloadSceneAsync(0).ToUniTask().Forget();
		}
	}

	

	public static async UniTask SetLoadingText(string text) {
		if(Instance != null && Instance.loadingText != null) {
			if (Game.IsGameRunningDebug == false) {
				Instance.loadingText.SetText(text);
				Logger.Log(Channel.Loading, text);
				await UniTask.NextFrame();
			}
		}
	}
}
