using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Features.Transitions.Utils
{
    public static class SceneHelper
    {
        public static bool IsSceneValid(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            
            try
            {
                return Application.CanStreamedLevelBeLoaded(sceneName);
            }
            catch
            {
                return false;
            }
        }
        
        public static IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Additive)
        {
            if (!IsSceneValid(sceneName))
            {
                Debug.LogError($"Scene {sceneName} cannot be loaded!");
                yield break;
            }
            
            var loadOp = SceneManager.LoadSceneAsync(sceneName, mode);
            if (loadOp != null)
            {
                loadOp.allowSceneActivation = false;
                yield return loadOp;
            }
        }
        
        public static IEnumerator UnloadAllScenesExcept(string[] excludeScenes)
        {
            List<string> scenesToUnload = new List<string>();
            
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && !ShouldExcludeScene(scene.name, excludeScenes))
                {
                    scenesToUnload.Add(scene.name);
                }
            }
            
            foreach (string sceneName in scenesToUnload)
            {
                Debug.Log($"Unloading scene: {sceneName}");
                yield return SceneManager.UnloadSceneAsync(sceneName);
            }
            
            MemoryHelper.ForceGarbageCollection();
            yield return Resources.UnloadUnusedAssets();
        }
        
        public static void SetActiveScene(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.SetActiveScene(scene);
            }
        }
        
        private static bool ShouldExcludeScene(string sceneName, string[] excludeScenes)
        {
            if (excludeScenes == null) return false;
            if (sceneName == "DontDestroyOnLoad") return true;
            
            foreach (string excludeScene in excludeScenes)
            {
                if (sceneName == excludeScene) return true;
            }
            return false;
        }
    }
}