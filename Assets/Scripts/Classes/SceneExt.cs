using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

public static class SceneEx {

    public static async UniTask LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Additive) {
        var op = SceneManager.LoadSceneAsync(sceneName, mode);
        op.allowSceneActivation = true;
        await UniTask.WaitUntil(() => op.isDone);
    }
    public static async UniTask UnloadSceneAsync(string sceneName) {
        var op = SceneManager.UnloadSceneAsync(sceneName);
        op.allowSceneActivation = true;
        await UniTask.WaitUntil(() => op.isDone);
    }
}
