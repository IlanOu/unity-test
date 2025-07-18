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
        
        [Header("Transition Selection")]
        [SerializeField] private TransitionData specificTransitionForLoading;
        [Tooltip("Si vide, utilise une transition aléatoire")]
        
        [Header("Configuration")]
        [SerializeField] private string nextSceneName = "MainMenu";
        [SerializeField] private float minimumLoadingTime = 2f;
        [SerializeField] private bool useRealProgress = true;
        [SerializeField] private float delayBeforeTransition = 0.5f;
        
        private bool preloadingComplete = false;
        private bool minimumTimeReached = false;

        private void Start()
        {
            StartCoroutine(InitializeGame());
        }

        private IEnumerator InitializeGame()
        {
            // Afficher la loading bar
            if (useRealProgress)
            {
                loadingBar.StartRealProgressLoading(loadingProfile, true, false, () => {
                    Debug.Log("Loading bar animation complete");
                });
            }
            else
            {
                loadingBar.ShowInstantly();
            }
            
            UpdateStatus("Initializing...");
            yield return new WaitForSeconds(0.5f);
            
            // Démarrer le préchargement
            var preloadCoroutine = StartCoroutine(PreloadSequencesWithRealProgress());
            var timerCoroutine = StartCoroutine(MinimumTimeTimer());
            
            // Attendre que tout soit terminé
            yield return preloadCoroutine;
            yield return timerCoroutine;
            
            // Compléter la loading bar
            if (useRealProgress)
            {
                loadingBar.CompleteRealProgress();
                yield return new WaitForSeconds(loadingProfile.completeHoldDuration + loadingProfile.exitDuration);
            }
            
            UpdateStatus("Loading complete!");
            yield return new WaitForSeconds(delayBeforeTransition);
            
            // Jouer la transition avec la transition choisie
            if (transitionController != null)
            {
                UpdateStatus("Starting game...");
                
                // Choisir quelle transition utiliser
                TransitionData transitionToUse = ChooseTransitionForLoading();
                
                yield return transitionController.PlayExitTransitionToScene(nextSceneName, transitionToUse, () => {
                    // Cacher immédiatement la loading screen quand la transition commence
                    if (mainCanvasGroup != null)
                        mainCanvasGroup.alpha = 0f;
                });
            }
            else
            {
                Debug.LogWarning("No TransitionController assigned, loading scene directly");
                SceneManager.LoadScene(nextSceneName);
            }
        }

        /// <summary>
        /// Choisit quelle transition utiliser pour sortir de la loading screen
        /// </summary>
        private TransitionData ChooseTransitionForLoading()
        {
            Debug.Log("=== ChooseTransitionForLoading ===");

            // Si une transition spécifique est assignée dans l'inspecteur, l'utiliser
            if (specificTransitionForLoading != null)
            {
                Debug.Log($"Using specific transition for loading: {specificTransitionForLoading.name}");
                Debug.Log($"Exit sequence: {specificTransitionForLoading.exitSequenceName}");
                return specificTransitionForLoading;
            }

            // Demander au TransitionController de sélectionner une transition pour le contexte "initial"
            if (transitionController != null)
            {
                var chosenTransition = transitionController.SelectTransitionForContext("initial");
                if (chosenTransition != null)
                {
                    Debug.Log($"TransitionController selected transition: {chosenTransition.name}");
                    Debug.Log($"Exit sequence: {chosenTransition.exitSequenceName}");
                    return chosenTransition;
                }
            }

            Debug.Log("No transition selected, will use TransitionController default logic");
            return null; // Le TransitionController utilisera sa logique par défaut
        }

        private IEnumerator PreloadSequencesWithRealProgress()
        {
            UpdateStatus("Loading transition sequences...");
            
            var cache = SequenceCache.Instance;
            
            yield return cache.PreloadAllSequences(
                onProgressUpdate: (progress) => {
                    if (useRealProgress)
                    {
                        loadingBar.UpdateRealProgress(progress);
                    }
                },
                onStatusUpdate: (status) => {
                    UpdateStatus(status);
                }
            );
            
            preloadingComplete = true;
            UpdateStatus("Sequences loaded successfully!");
        }

        private IEnumerator MinimumTimeTimer()
        {
            yield return new WaitForSeconds(minimumLoadingTime);
            minimumTimeReached = true;
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"Loading: {message}");
        }
    }
}