using System;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public static class SceneEx {

    public static async Task LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Additive) {
        var op = SceneManager.LoadSceneAsync(sceneName, mode);
        op.allowSceneActivation = true;
        await TaskEx.WaitUntil(() => op.isDone);
    }
    public static async Task UnloadSceneAsync(string sceneName) {
        var op = SceneManager.UnloadSceneAsync(sceneName);
        op.allowSceneActivation = true;
        await TaskEx.WaitUntil(() => op.isDone);
    }
}
