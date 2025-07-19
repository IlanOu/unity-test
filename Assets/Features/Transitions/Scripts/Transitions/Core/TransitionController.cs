using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Features.Transitions.Configuration;
using Features.Transitions.UI;
using Features.Transitions.Utils;

namespace Features.Transitions.Core
{
    public class TransitionController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TransitionConfig transitionConfig;

        [Header("Components")]
        [SerializeField] private TransitionUI transitionUI;

        private TransitionData currentTransition;

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (transitionUI == null)
            {
                transitionUI = GetComponent<TransitionUI>();
                if (transitionUI == null)
                {
                    transitionUI = gameObject.AddComponent<TransitionUI>();
                }
            }

            AudioHelper.EnsureAudioSource(gameObject);
        }

        #region Public API

        public IEnumerator PlayTransition(AsyncOperation preloadedScene, TransitionData specificTransition = null, System.Action onStart = null)
        {
            if (!ValidateSetup()) 
            {
                if (preloadedScene != null)
                {
                    preloadedScene.allowSceneActivation = true;
                }
                yield break;
            }

            currentTransition = specificTransition ?? transitionConfig.GetRandomTransition();
            if (currentTransition == null)
            {
                Debug.LogError("No transition available!");
                if (preloadedScene != null)
                {
                    preloadedScene.allowSceneActivation = true;
                }
                yield break;
            }

            transitionUI.SetupCanvas();
            onStart?.Invoke();

            if (preloadedScene != null)
            {
                preloadedScene.allowSceneActivation = true;
                yield return preloadedScene;
                yield return null;
            }

            yield return transitionUI.PlayExitSequence(currentTransition);
        }

        public IEnumerator ExecuteTransition(string targetSceneName)
        {
            if (!ValidateSetup()) yield break;

            currentTransition = transitionConfig.GetRandomTransition();
            if (currentTransition == null) yield break;

            transitionUI.SetupCanvas();
            yield return transitionUI.PlayEntrySequence(currentTransition);
            yield return LoadSceneWithLoadingBar(targetSceneName);
            yield return transitionUI.PlayExitSequence(currentTransition);
            yield return Cleanup();
        }

        public IEnumerator PlayExitTransitionOnly(TransitionData specificTransition = null)
        {
            if (!ValidateSetup()) yield break;

            currentTransition = specificTransition ?? transitionConfig.GetRandomTransition();
            if (currentTransition == null) yield break;

            transitionUI.SetupCanvas();
            yield return transitionUI.PlayExitSequence(currentTransition);
            yield return Cleanup();
        }

        public TransitionData SelectTransitionForContext(string context = "default")
        {
            if (transitionConfig?.availableTransitions == null) return null;
            
            var transitions = transitionConfig.availableTransitions;
            return transitions.Length > 0 ? transitions[0] : null;
        }

        #endregion

        #region Core Logic

        private IEnumerator LoadSceneWithLoadingBar(string targetSceneName)
        {
            string[] excludeScenes = { gameObject.scene.name };
            yield return SceneHelper.UnloadAllScenesExcept(excludeScenes);

            var loadOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            if (loadOp == null)
            {
                Debug.LogError($"Failed to load scene: {targetSceneName}");
                yield break;
            }
            loadOp.allowSceneActivation = false;

            bool useLongLoading = transitionConfig.ShouldUseLongLoading();
            float loadingDuration = useLongLoading ? 
                transitionConfig.longLoadingDuration : 
                UnityEngine.Random.Range(0.5f, 1.5f);

            Debug.Log($"Loading duration: {loadingDuration}s (long: {useLongLoading})");

            yield return transitionUI.ShowLoadingBarAnimation(loadOp, loadingDuration, currentTransition);

            loadOp.allowSceneActivation = true;
            yield return loadOp;

            SceneHelper.SetActiveScene(targetSceneName);
        }

        #endregion

        #region Helper Methods

        private bool ValidateSetup()
        {
            if (transitionConfig == null)
            {
                Debug.LogError("TransitionConfig not assigned!");
                return false;
            }

            if (transitionUI == null)
            {
                Debug.LogError("TransitionUI not found!");
                return false;
            }

            return transitionUI.ValidateSetup();
        }

        private IEnumerator Cleanup()
        {
            transitionUI.Cleanup();
            
            string currentSceneName = gameObject.scene.name;
            if (!string.IsNullOrEmpty(currentSceneName))
            {
                yield return SceneManager.UnloadSceneAsync(currentSceneName);
            }
        }

        #endregion
    }
}