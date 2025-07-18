using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Features.LoadingBar;
using TMPro;

namespace Features.Transitions
{
    public class InitialLoadingScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private JuicyLoadingBar loadingBar;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private LoadingAnimationProfile loadingProfile;
        [SerializeField] private CanvasGroup mainCanvasGroup;
        
        [Header("Transition Components")]
        [SerializeField] private TransitionController transitionController;
        [SerializeField] private TransitionData specificTransitionForLoading;
        
        [Header("Configuration")]
        [SerializeField] private string nextSceneName = "MainMenu";
        [SerializeField] private float minimumLoadingTime = 2f;
        [SerializeField] private bool useRealProgress = true;

        private void Start()
        {
            StartCoroutine(InitializeGame());
        }

        private IEnumerator InitializeGame()
        {
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError("Next scene name is not configured!");
                yield break;
            }

            // Démarrer la loading bar
            if (useRealProgress)
            {
                loadingBar.StartRealProgressLoading(loadingProfile, true, false, null);
            }
            else
            {
                loadingBar.ShowInstantly();
            }
            
            UpdateStatus("Loading...");
            
            // Charger la scène en arrière-plan
            var sceneLoadOperation = SceneManager.LoadSceneAsync(nextSceneName);
            if (sceneLoadOperation == null)
            {
                Debug.LogError($"Failed to load scene: {nextSceneName}");
                yield break;
            }
            sceneLoadOperation.allowSceneActivation = false;
            
            // Précharger les séquences
            yield return PreloadSequences(sceneLoadOperation);
            
            // Temps minimum
            yield return new WaitForSeconds(minimumLoadingTime);
            
            // Attendre que la scène soit chargée
            yield return new WaitUntil(() => sceneLoadOperation.progress >= 0.9f);
            
            // Finaliser la loading bar
            if (useRealProgress)
            {
                loadingBar.CompleteRealProgress();
                yield return new WaitForSeconds(0.5f);
            }
            
            // Jouer la transition
            if (transitionController != null)
            {
                yield return transitionController.PlayTransition(sceneLoadOperation, specificTransitionForLoading, () => {
                    if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;
                });
            }
            else
            {
                sceneLoadOperation.allowSceneActivation = true;
            }
            
            // Décharger cette scène
            yield return new WaitForSeconds(0.5f);
            yield return SceneManager.UnloadSceneAsync(gameObject.scene.name);
        }

        private IEnumerator PreloadSequences(AsyncOperation sceneOperation)
        {
            var cache = SequenceCache.Instance;
            yield return cache.PreloadAllSequences(
                onProgressUpdate: (progress) => {
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