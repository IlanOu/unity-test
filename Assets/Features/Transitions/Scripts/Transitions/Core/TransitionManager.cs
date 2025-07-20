using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Features.Transitions.Core
{
    public class TransitionManager : MonoBehaviour
    {
        private static TransitionManager _instance;
        private bool _isTransitioning = false;

        public static TransitionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[TransitionManager]");
                    _instance = go.AddComponent<TransitionManager>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Lance une transition vers la scène spécifiée.
        /// </summary>
        public static void TransitionToScene(string targetSceneName, System.Action onFailure = null)
        {
            if (Instance._isTransitioning)
            {
                Debug.LogWarning("TransitionManager: Transition already in progress. Request ignored.");
                onFailure?.Invoke();
                return;
            }
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError("TransitionManager: Target scene name is null or empty. Aborting.", Instance);
                onFailure?.Invoke();
                return;
            }

            Scene currentScene = SceneManager.GetActiveScene();
            Instance.StartCoroutine(Instance.TransitionFlow(currentScene.name, targetSceneName, onFailure));
        }

        private IEnumerator TransitionFlow(string currentSceneName, string targetSceneName, System.Action onFailure)
        {
            _isTransitioning = true;

            // Charge la scène contenant le TransitionController par-dessus la scène actuelle.
            yield return SceneManager.LoadSceneAsync("TransitionScene", LoadSceneMode.Additive);
            yield return null; // Attend une frame pour garantir l'initialisation.

            var controller = FindObjectOfType<TransitionController>();
            if (controller != null)
            {
                // Délègue tout le travail au TransitionController.
                yield return controller.ExecuteTransition(targetSceneName, currentSceneName);
            }
            else
            {
                Debug.LogError("TransitionController not found in 'TransitionScene'! Aborting.", this);
                onFailure?.Invoke();
                // Nettoyage en cas d'erreur.
                yield return SceneManager.UnloadSceneAsync("TransitionScene");
            }

            _isTransitioning = false;
        }
    }
}