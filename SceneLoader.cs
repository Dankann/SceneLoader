using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static SceneLoaderData SceneLoaderData
    {
        get
        {
            if (!sceneLoaderData)
                sceneLoaderData = Resources.Load<SceneLoaderData>(nameof(SceneLoaderData));
            return sceneLoaderData;
        }
    }

    static SceneLoaderData sceneLoaderData;

    static bool IsSceneLoaded(string sceneName) => SceneManager.GetSceneByName(sceneName).IsValid();

    static bool IsSceneLoaded(int sceneBuildIndex) => IsSceneLoaded(SceneManager.GetSceneByBuildIndex(sceneBuildIndex).name);

    public static async Task LoadSceneAsync(int sceneBuildIndex, string loadingSceneName = "", Func<bool> waitUntilPredicate = null, LoadSceneMode loadSceneMode = LoadSceneMode.Single, Action<float> onProgress = null, Action<AsyncOperation> onFinishLoad = null) =>
        await LoadSceneAsync(SceneManager.GetSceneByBuildIndex(sceneBuildIndex).name, loadingSceneName, waitUntilPredicate, loadSceneMode, onProgress, onFinishLoad);

    public static async Task UnloadScene(int sceneBuildIndex, Action<float> onProgress = null, Action<AsyncOperation> onFinishUnload = null) =>
        await UnloadSceneAsync(SceneManager.GetSceneByBuildIndex(sceneBuildIndex).name, onProgress, onFinishUnload);

    public static async Task LoadSceneAsync(string sceneName, string loadingSceneName = "", Func<bool> waitUntilPredicate = null, LoadSceneMode loadSceneMode = LoadSceneMode.Single, Action<float> onProgress = null, Action<AsyncOperation> onFinishLoad = null)
    {
        sceneName = System.IO.Path.GetFileNameWithoutExtension(sceneName);

        if (IsSceneLoaded(sceneName))
        {
            Debug.Log($"Scene {sceneName} is already loaded.");
            SceneLoaderData.scenesLoaded.AddUnique(sceneName);
            return;
        }

        if (SceneLoaderData.scenesLoading.Contains(sceneName))
        {
            Debug.Log($"Scene {sceneName} is already loading.");
            return;
        }

        await LoadLoadingScene(loadingSceneName, loadSceneMode);

        var loadingAsync = SceneManager.LoadSceneAsync(sceneName, !string.IsNullOrEmpty(loadingSceneName) ? LoadSceneMode.Additive : loadSceneMode);

        loadingAsync.allowSceneActivation = false;
        if (onFinishLoad != null) loadingAsync.completed += onFinishLoad;
        SceneLoaderData.scenesLoading.AddUnique(sceneName);

        while (loadingAsync.progress < .9f)
        {
            onProgress?.Invoke(loadingAsync.progress);
            await Task.Yield();
        }

        onProgress?.Invoke(loadingAsync.progress);

        if (waitUntilPredicate != null)
            await TaskExtensions.WaitUntilAsync(waitUntilPredicate);

        loadingAsync.allowSceneActivation = true;

        await TaskExtensions.WaitUntilAsync(() => loadingAsync.isDone);

        var scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid())
            SceneManager.SetActiveScene(scene);

        SceneLoaderData.scenesLoading.RemoveUnique(sceneName);
        SceneLoaderData.scenesLoaded.AddUnique(sceneName);

        onProgress?.Invoke(loadingAsync.progress);

        await UnloadLoadingScene(loadingSceneName);
    }

    public static async Task UnloadSceneAsync(string sceneName, Action<float> onProgress = null, Action<AsyncOperation> onFinishUnload = null)
    {
        sceneName = System.IO.Path.GetFileNameWithoutExtension(sceneName);

        if (!IsSceneLoaded(sceneName))
        {
            Debug.Log($"Scene {sceneName} is already unloaded.");
            SceneLoaderData.scenesUnloading.RemoveUnique(sceneName);
            return;
        }

        if (SceneLoaderData.scenesUnloading.Contains(sceneName))
        {
            Debug.Log($"Scene {sceneName} is already unloading.");
            return;
        }

        var unloadSceneAsync = SceneManager.UnloadSceneAsync(sceneName);
        SceneLoaderData.scenesUnloading.AddUnique(sceneName);

        if (onFinishUnload != null)
            unloadSceneAsync.completed += onFinishUnload;

        while (unloadSceneAsync.progress < .9f)
        {
            onProgress?.Invoke(unloadSceneAsync.progress * .9f);
            await Task.Yield();
        }
        onProgress?.Invoke(unloadSceneAsync.progress);

        await TaskExtensions.WaitUntilAsync(() => unloadSceneAsync.isDone);

        onProgress?.Invoke(unloadSceneAsync.progress);
        SceneLoaderData.scenesUnloading.RemoveUnique(sceneName);
        SceneLoaderData.scenesLoaded.RemoveUnique(sceneName);
    }

    public static async Task ReloadActiveScene(string loadingSceneName = "", LoadSceneMode loadSceneMode = LoadSceneMode.Single, Func<bool> waitUntilPredicate = null, Action<float> onProgress = null, Action<AsyncOperation> onFinishLoad = null)
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (!IsSceneLoaded(sceneName))
        {
            Debug.Log($"Scene {sceneName} is not loaded.");
            return;
        }

        await LoadLoadingScene(loadingSceneName, loadSceneMode);
        await UnloadSceneAsync(sceneName, (progress) => onProgress?.Invoke(progress * .5f));
        await LoadSceneAsync(sceneName, "", waitUntilPredicate, loadSceneMode, (progress) => onProgress?.Invoke(.5f + (progress * .5f)), onFinishLoad);
        await UnloadLoadingScene(loadingSceneName);
    }

    static async Task LoadLoadingScene(string loadingSceneName, LoadSceneMode loadSceneMode)
    {
        if (!string.IsNullOrEmpty(loadingSceneName))
        {
            if (!IsSceneLoaded(loadingSceneName) && !SceneLoaderData.scenesLoading.Contains(loadingSceneName))
            {
                var loadScreenAsync = SceneManager.LoadSceneAsync(loadingSceneName, loadSceneMode);
                SceneLoaderData.scenesLoading.AddUnique(loadingSceneName);

                await TaskExtensions.WaitUntilAsync(() => loadScreenAsync.isDone);
                SceneLoaderData.scenesLoading.RemoveUnique(loadingSceneName);
                SceneLoaderData.scenesLoaded.AddUnique(loadingSceneName);
            }
        }
    }

    static async Task UnloadLoadingScene(string loadingSceneName)
    {
        if (string.IsNullOrEmpty(loadingSceneName))
            return;

        if (IsSceneLoaded(loadingSceneName))
        {
            var unloadSceneAsync = SceneManager.UnloadSceneAsync(loadingSceneName);
            SceneLoaderData.scenesLoaded.RemoveUnique(loadingSceneName);
            SceneLoaderData.scenesUnloading.AddUnique(loadingSceneName);
            await TaskExtensions.WaitUntilAsync(() => unloadSceneAsync.isDone);
            SceneLoaderData.scenesUnloading.RemoveUnique(loadingSceneName);
        }
    }
}
