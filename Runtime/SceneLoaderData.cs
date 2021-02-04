using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace Dankann.SceneLoader
{
    [CreateAssetMenu(fileName = "SceneLoaderData", menuName = "Scene Loader/Scene Loader Data")]
    public class SceneLoaderData : ScriptableObject
    {
        public List<string> scenesLoading = new List<string>();
        public List<string> scenesUnloading = new List<string>();
        public List<string> scenesLoaded = new List<string>();

        public void OnEnable()
        {
            Clear();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public void Clear()
        {
            scenesLoading.Clear();
            scenesUnloading.Clear();
            scenesLoaded.Clear();
        }

        async void OnSceneUnloaded(Scene scene)
        {
            await Task.Yield();
            if (scenesUnloading.Contains(scene.name))
            {
                scenesUnloading.RemoveUnique(scene.name);
                Debug.Log($"Scene {scene.name} unloaded outside SceneLoader flow. Data updated!");
            }

            scenesLoaded.Clear();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                scenesLoaded.AddUnique(SceneManager.GetSceneAt(i).name);
        }

        async void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            await Task.Yield();
            if (scenesLoading.Contains(scene.name))
            {
                scenesLoading.RemoveUnique(scene.name);
                Debug.Log($"Scene {scene.name} loaded outside SceneLoader flow. Data updated!");
            }

            scenesLoaded.Clear();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                scenesLoaded.AddUnique(SceneManager.GetSceneAt(i).name);
        }
    }
}