using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Features.LoadingBar;
using Features.Transitions.Core;
using Features.Transitions.UI;
using Features.Transitions.Sequences;
using TMPro;

namespace Features.Transitions.UI
{
    public class InitialLoadingScreen : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private JuicyLoadingBar loadingBar;

        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private LoadingAnimationProfile loadingProfile;
        [SerializeField] private CanvasGroup mainCanvasGroup;

        [Header("Transition Components")] [SerializeField]
        private TransitionController transitionController;

        [SerializeField] private TransitionData specificTransitionForLoading;

        [Header("Transition Settings")] [SerializeField]
        private bool useEntrySequence = false;

        [Header("Configuration")] [SerializeField]
        private string nextSceneName = "MainMenu";

        [SerializeField] private float minimumLoadingTime = 2f;
        [SerializeField] private bool useRealProgress = true;

        private void Start()
        {
            StartCoroutine(InitializeGame());
        }

        private IEnumerator InitializeGame()
        {
            Debug.Log("=== INITIAL LOADING START ===");

            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError("Next scene name is not configured!");
                yield break;
            }

            if (useRealProgress)
            {
                loadingBar.StartRealProgressLoading(loadingProfile, true, false, null);
            }
            else
            {
                loadingBar.ShowInstantly();
            }

            UpdateStatus("Loading...");

            Debug.Log($"Loading scene: {nextSceneName} in additive mode");
            var sceneLoadOperation = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
            if (sceneLoadOperation == null)
            {
                Debug.LogError($"Failed to load scene: {nextSceneName}");
                yield break;
            }

            sceneLoadOperation.allowSceneActivation = false;

            yield return PreloadSequences(sceneLoadOperation);
            yield return new WaitForSeconds(minimumLoadingTime);
            yield return new WaitUntil(() => sceneLoadOperation.progress >= 0.9f);

            if (useRealProgress)
            {
                loadingBar.CompleteRealProgress();
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("Activating new scene...");
            sceneLoadOperation.allowSceneActivation = true;
            yield return sceneLoadOperation;

            var newScene = SceneManager.GetSceneByName(nextSceneName);
            if (newScene.IsValid())
            {
                SceneManager.SetActiveScene(newScene);
                Debug.Log($"Active scene set to: {newScene.name}");
            }
            
            if (transitionController != null && specificTransitionForLoading != null)
            {
                string sequenceName = useEntrySequence
                    ? specificTransitionForLoading.entrySequenceName
                    : specificTransitionForLoading.exitSequenceName;

                Debug.Log($"Using sequence: {sequenceName} (entry: {useEntrySequence})");
                Debug.Log($"Transition data: {specificTransitionForLoading.name}");

                bool isCached = SequenceCache.Instance.IsSequenceCached(sequenceName);
                Debug.Log($"Sequence {sequenceName} cached: {isCached}");

                if (isCached)
                {
                    var cachedSequence = SequenceCache.Instance.GetSequence(sequenceName);
                    Debug.Log($"Cached sequence frames: {cachedSequence?.FrameCount}");

                    var transitionUI = transitionController.GetComponent<TransitionUI>();
                    if (transitionUI != null)
                    {
                        Debug.Log("Setting up TransitionUI canvas...");
                        transitionUI.SetupCanvasWithoutReset();

                        Debug.Log("Starting transition with canvas management...");
                        yield return PlayTransitionWithCanvasManagement(transitionUI, sequenceName);
                        Debug.Log("Transition completed!");
                    }
                    else
                    {
                        Debug.LogError("TransitionUI not found on TransitionController!");
                        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;
                        yield return new WaitForSeconds(1f);
                    }
                }
                else
                {
                    Debug.LogWarning("Transition sequence not cached, hiding canvas immediately");
                    if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;
                    yield return new WaitForSeconds(1f);
                }
            }
            else
            {
                Debug.LogWarning(
                    $"Missing components - TransitionController: {transitionController != null}, TransitionData: {specificTransitionForLoading != null}");
                yield return new WaitForSeconds(0.5f);
                if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;
                yield return new WaitForSeconds(1f);
            }

            Debug.Log("About to unload InitialLoadingScreen scene...");
            string currentSceneName = gameObject.scene.name;
            Debug.Log($"Unloading scene: {currentSceneName}");
            yield return SceneManager.UnloadSceneAsync(currentSceneName);
            Debug.Log("=== INITIAL LOADING END ===");
        }

        private IEnumerator PlayTransitionWithCanvasManagement(TransitionUI transitionUI, string sequenceName)
        {
            Debug.Log("=== TRANSITION WITH CANVAS MANAGEMENT START ===");
            
            Canvas loadingCanvas = null;
            if (mainCanvasGroup != null)
            {
                loadingCanvas = mainCanvasGroup.GetComponentInParent<Canvas>();
                if (loadingCanvas == null)
                {
                    loadingCanvas = mainCanvasGroup.GetComponent<Canvas>();
                }

                Debug.Log($"Found loading canvas: {loadingCanvas?.name}");
            }
            
            transitionUI.SetExternalLoadingCanvas(loadingCanvas, mainCanvasGroup);

            Debug.Log("Starting transition - TransitionUI will handle canvas timing");
            
            if (useEntrySequence)
            {
                yield return transitionUI.PlayEntrySequence(specificTransitionForLoading);
            }
            else
            {
                yield return transitionUI.PlayExitSequence(specificTransitionForLoading);
            }

            Debug.Log("Transition completed!");
            yield return new WaitForSeconds(0.5f);
            Debug.Log("=== TRANSITION WITH CANVAS MANAGEMENT END ===");
        }

        private IEnumerator PreloadSequences(AsyncOperation sceneOperation)
        {
            var cache = SequenceCache.Instance;
            yield return cache.PreloadAllSequences(
                onProgressUpdate: (progress) =>
                {
                    if (useRealProgress)
                    {
                        float sceneProgress = sceneOperation.progress;
                        float combinedProgress = (progress * 0.7f) + (sceneProgress * 0.3f);
                        loadingBar.UpdateRealProgress(combinedProgress);
                    }
                },
                onStatusUpdate: UpdateStatus
            );
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}