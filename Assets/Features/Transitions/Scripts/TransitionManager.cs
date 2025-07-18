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

        public static void TransitionToScene(string targetSceneName)
        {
            if (Instance._isTransitioning || string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogWarning("Transition already in progress or invalid scene name!");
                return;
            }

            Instance.StartCoroutine(Instance.ExecuteTransition(targetSceneName));
        }

        private IEnumerator ExecuteTransition(string targetSceneName)
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
            }

            _isTransitioning = false;
        }
    }
}