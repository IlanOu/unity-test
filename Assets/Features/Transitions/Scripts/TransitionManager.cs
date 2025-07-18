using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Features.Transitions
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
                    CreateInstance();
                }
                return _instance;
            }
        }

        private static void CreateInstance()
        {
            GameObject go = new GameObject("[TransitionManager]");
            _instance = go.AddComponent<TransitionManager>();
            DontDestroyOnLoad(go);
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

        public static void TransitionToScene(string targetSceneName)
        {
            if (Instance._isTransitioning)
            {
                Debug.LogWarning("Transition already in progress!");
                return;
            }

            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError("Target scene name cannot be null or empty!");
                return;
            }

            Instance.StartCoroutine(Instance.ExecuteTransition(targetSceneName));
        }

        private IEnumerator ExecuteTransition(string targetSceneName)
        {
            _isTransitioning = true;

            // Charger TransitionScene
            var transitionLoadOp = SceneManager.LoadSceneAsync("TransitionScene", LoadSceneMode.Additive);
            yield return transitionLoadOp;
            yield return null; // Frame pour l'initialisation

            // Trouver et exécuter la transition
            var transitionController = FindObjectOfType<TransitionController>();
            if (transitionController == null)
            {
                Debug.LogError("TransitionController not found in TransitionScene!");
                _isTransitioning = false;
                yield break;
            }

            yield return transitionController.ExecuteTransition(targetSceneName);
            _isTransitioning = false;
        }
    }
}