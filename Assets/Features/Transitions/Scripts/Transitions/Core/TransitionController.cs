using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Features.Transitions.Configuration;
using Features.Transitions.UI;
using Features.Transitions.Utils;
using Features.Transitions.Sequences;

namespace Features.Transitions.Core
{
    [RequireComponent(typeof(TransitionUI))]
    public class TransitionController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TransitionConfig transitionConfig;
        
        [Header("Components")]
        [SerializeField] private TransitionUI transitionUI;

        private void Start()
        {
            InitializeComponents();

            // S'assure que le fournisseur est prêt à recevoir des demandes de chargement.
            if(SequenceProvider.Instance != null)
            {
                StartCoroutine(SequenceProvider.Instance.Initialize());
            }
            else
            {
                Debug.LogError("FATAL: SequenceProvider is not available. Ensure it's in the scene and check Script Execution Order.", this);
            }
        }

        private void InitializeComponents()
        {
            if (transitionUI == null)
            {
                transitionUI = GetComponent<TransitionUI>();
            }
            AudioHelper.EnsureAudioSource(gameObject);
        }

        /// <summary>
        /// Orchestre une transition complète : animation d'entrée, changement de scène, animation de sortie.
        /// </summary>
        public IEnumerator ExecuteTransition(string targetSceneName, string sourceSceneName, TransitionData specificTransition = null)
        {
            if (!ValidateSetup()) yield break;

            TransitionData transition = specificTransition ?? transitionConfig.GetRandomTransition();
            if (transition == null)
            {
                Debug.LogError("No TransitionData found. Switching scenes directly.");
                if(!string.IsNullOrEmpty(sourceSceneName) && SceneManager.GetSceneByName(sourceSceneName).isLoaded) 
                    yield return SceneManager.UnloadSceneAsync(sourceSceneName);
                yield return SceneManager.LoadSceneAsync(targetSceneName);
                yield break;
            }

            // -- Séquence d'Entrée --
            if (!string.IsNullOrEmpty(transition.entrySequenceName))
            {
                yield return SequenceProvider.Instance.PreloadSequence(transition.entrySequenceName);
            }
            
            transitionUI.SetupCanvas();
            yield return transitionUI.PlayEntrySequence(transition);

            // -- Changement de Scène --
            yield return LoadAndUnloadScenes(targetSceneName, sourceSceneName, transition);
            
            if (!string.IsNullOrEmpty(transition.entrySequenceName))
            {
                SequenceProvider.Instance.UnloadSequence(transition.entrySequenceName);
            }

            // -- Séquence de Sortie --
            if (!string.IsNullOrEmpty(transition.exitSequenceName))
            {
                yield return SequenceProvider.Instance.PreloadSequence(transition.exitSequenceName);
            }

            yield return transitionUI.PlayExitSequence(transition);

            if (!string.IsNullOrEmpty(transition.exitSequenceName))
            {
                SequenceProvider.Instance.UnloadSequence(transition.exitSequenceName);
            }
            
            // -- Nettoyage Final --
            if(SceneManager.GetSceneByName("TransitionScene").isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync("TransitionScene");
            }
        }
        
        private IEnumerator LoadAndUnloadScenes(string targetSceneName, string sourceSceneName, TransitionData transition)
        {
            // Décharge d'abord l'ancienne scène pour éviter tout conflit.
            if (!string.IsNullOrEmpty(sourceSceneName) && SceneManager.GetSceneByName(sourceSceneName).isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(sourceSceneName);
            }
            
            yield return Resources.UnloadUnusedAssets();

            // Charge ensuite la nouvelle scène.
            var loadOp = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            loadOp.allowSceneActivation = false;

            float loadingDuration = (transitionConfig != null && transitionConfig.ShouldUseLongLoading()) 
                ? transitionConfig.longLoadingDuration 
                : Random.Range(0.5f, 1.5f);

            yield return transitionUI.ShowLoadingBarAnimation(loadOp, loadingDuration, transition);
            
            loadOp.allowSceneActivation = true;
            yield return loadOp;

            Scene newScene = SceneManager.GetSceneByName(targetSceneName);
            if (newScene.IsValid())
            {
                SceneManager.SetActiveScene(newScene);
            }
        }

        private bool ValidateSetup()
        {
            if (transitionConfig == null) { Debug.LogError("TransitionConfig not assigned in Inspector!", this); return false; }
            if (transitionUI == null) { Debug.LogError("TransitionUI not assigned in Inspector!", this); return false; }
            return true;
        }
    }
}