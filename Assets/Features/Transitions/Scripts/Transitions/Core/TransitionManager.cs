using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Features.Transitions.Core;

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
                if (_instance == null) CreateInstance();
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

        public static void TransitionToScene(string targetSceneName, System.Action onFailure = null)
        {
            if (Instance._isTransitioning)
            {
                Debug.LogWarning("Transition already in progress!");
                onFailure?.Invoke();
                return;
            }

            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError("Invalid scene name!");
                onFailure?.Invoke();
                return;
            }

            if (!Utils.SceneHelper.IsSceneValid(targetSceneName))
            {
                Debug.LogError($"Scene {targetSceneName} cannot be loaded!");
                onFailure?.Invoke();
                return;
            }

            Instance.StartCoroutine(Instance.ExecuteTransition(targetSceneName, onFailure));
        }

        private IEnumerator ExecuteTransition(string targetSceneName, System.Action onFailure)
        {
            _isTransitioning = true;

            var transitionLoadOp = SceneManager.LoadSceneAsync("TransitionScene", LoadSceneMode.Additive);
            yield return transitionLoadOp;
            yield return null;

            var controller = FindObjectOfType<TransitionController>();
            if (controller != null)
            {
                yield return controller.ExecuteTransition(targetSceneName);
            }
            else
            {
                Debug.LogError("TransitionController not found!");
                onFailure?.Invoke();
            }

            _isTransitioning = false;
        }
    }
}