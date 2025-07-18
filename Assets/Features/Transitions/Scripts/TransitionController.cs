using System.Collections;
using System.Collections.Generic;
using Features.LoadingBar;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Features.Transitions
{
    public class TransitionController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TransitionConfig transitionConfig;

        [Header("UI Elements")]
        [SerializeField] private Canvas transitionCanvas;

        [SerializeField] private Image backgroundPanel;
        [SerializeField] private Image sequenceDisplay;

        [Header("Audio (optionnel)")]
        [SerializeField] private AudioSource audioSource;

        [Header("Loading Bar")]
        [SerializeField] private MonoBehaviour loadingBarComponent;

        private ILoadingBar loadingBar;
        private TransitionData currentTransition;
        private bool loadingScreenVisible = false;
        private SequencePlayer sequencePlayer;
        private Coroutine loadingDelayCoroutine;

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            loadingBar = loadingBarComponent as ILoadingBar;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                audioSource.playOnAwake = false;
            }

            sequencePlayer = GetComponent<SequencePlayer>(); // Changé ici
            if (sequencePlayer == null)
                sequencePlayer = gameObject.AddComponent<SequencePlayer>(); // Et ici
        }

        private void OnValidate()
        {
            if (transitionConfig == null)
                Debug.LogWarning("TransitionConfig is not assigned!");
            if (sequenceDisplay == null)
                Debug.LogWarning("SequenceDisplay is not assigned!");
            if (transitionCanvas == null)
                Debug.LogWarning("TransitionCanvas is not assigned!");
        }

        private void OnDestroy()
        {
            CleanupLastFrame();
        }

        #region Public API

        /// <summary>
        /// Transition complète : entrée + chargement + sortie
        /// </summary>
        public IEnumerator ExecuteTransition(string targetSceneName)
        {
            if (!ValidateSetup()) yield break;

            InitializeUI();
            SelectRandomTransition();

            yield return PlayEntrySequence();
            yield return LoadingPhase(targetSceneName);
            yield return PlayExitSequence();
            yield return Cleanup();
        }

        /// <summary>
        /// Transition de sortie avec chargement (pour TransitionManager)
        /// </summary>
        public IEnumerator ExecuteExitSequenceOnly(string targetSceneName)
        {
            if (!ValidateSetup()) yield break;

            InitializeUI();
            SelectRandomTransition();

            yield return LoadingPhase(targetSceneName);
            yield return PlayExitSequence();
            yield return Cleanup();
        }

        /// <summary>
        /// Animation de sortie avec chargement synchrone, en utilisant une transition spécifique
        /// </summary>
        public IEnumerator PlayExitTransitionToScene(string targetSceneName, TransitionData specificTransition = null, System.Action onTransitionStart = null)
        {
            Debug.Log("=== PlayExitTransitionToScene START ===");

            if (!ValidateSetup())
            {
                Debug.LogError("Setup validation failed!");
                SceneManager.LoadScene(targetSceneName);
                yield break;
            }

            Debug.Log("Setup validation passed");

            // Utiliser la transition spécifiée ou une aléatoire
            if (specificTransition != null)
            {
                currentTransition = specificTransition;
                Debug.Log($"Using specific transition: {specificTransition.name}");
            }
            else
            {
                SelectRandomTransition();
                Debug.Log($"Using random transition: {currentTransition?.name}");
            }

            if (currentTransition == null)
            {
                Debug.LogError("No transition available!");
                SceneManager.LoadScene(targetSceneName);
                yield break;
            }

            Debug.Log($"Selected transition: {currentTransition.name}");
            Debug.Log($"Exit sequence: {currentTransition.exitSequenceName}");
            Debug.Log($"Frame rate: {currentTransition.frameRate}");

            Debug.Log("Calling InitializeComponents...");
            InitializeComponents();
            Debug.Log("InitializeComponents completed");

            Debug.Log("Calling ActivateTransitionCanvas...");
            ActivateTransitionCanvas();
            Debug.Log("ActivateTransitionCanvas completed");

            Debug.Log("Calling onTransitionStart callback...");
            onTransitionStart?.Invoke();
            Debug.Log("onTransitionStart callback completed");

            // Démarrer le chargement de la scène en arrière-plan AVANT l'animation
            Debug.Log($"Starting to load scene: {targetSceneName}");
            var sceneLoadOperation = SceneManager.LoadSceneAsync(targetSceneName);
            sceneLoadOperation.allowSceneActivation = false;
            Debug.Log("Scene loading started");

            // Jouer l'animation de sortie pendant que la scène charge
            Debug.Log("Starting PlayExitSequenceStandalone...");
            yield return PlayExitSequenceStandalone();
            Debug.Log("PlayExitSequenceStandalone completed");

            // S'assurer que la scène est complètement chargée avant de l'activer
            Debug.Log("Waiting for scene to load...");
            yield return new WaitUntil(() => sceneLoadOperation.progress >= 0.9f);

            Debug.Log("Scene loaded, activating...");
            sceneLoadOperation.allowSceneActivation = true;
            yield return sceneLoadOperation;

            Debug.Log($"Scene {targetSceneName} loaded and activated successfully");
            Debug.Log("=== PlayExitTransitionToScene END ===");
        }

        /// <summary>
        /// Initialise pour utilisation externe (dans LoadingScene)
        /// </summary>
        public void InitializeForExternalUse()
        {
            if (transitionConfig == null)
            {
                Debug.LogError("TransitionConfig not assigned!");
                return;
            }

            SelectRandomTransition();
            InitializeComponents();
        }

        /// <summary>
        /// Retourne une transition spécifique par nom
        /// </summary>
        public TransitionData GetTransitionByName(string transitionName)
        {
            if (transitionConfig == null || transitionConfig.availableTransitions == null)
                return null;

            foreach (var transition in transitionConfig.availableTransitions)
            {
                if (transition != null && transition.name == transitionName)
                    return transition;
            }

            Debug.LogWarning($"Transition '{transitionName}' not found in config");
            return null;
        }

        /// <summary>
        /// Retourne toutes les transitions disponibles
        /// </summary>
        public TransitionData[] GetAvailableTransitions()
        {
            return transitionConfig?.availableTransitions ?? new TransitionData[0];
        }

        #endregion

        #region Core Transition Logic

        private void SelectRandomTransition()
        {
            currentTransition = transitionConfig.GetRandomTransition();
            if (currentTransition == null)
            {
                Debug.LogError("No transition data available!");
            }
        }

        private IEnumerator PlayEntrySequence()
        {
            if (string.IsNullOrEmpty(currentTransition.entrySequenceName))
            {
                ShowLoadingScreen();
                yield break;
            }

            PlayTransitionSound(currentTransition.entrySound);
            loadingDelayCoroutine = StartCoroutine(ShowLoadingAfterDelay(currentTransition.showLoadingDelay));

            yield return sequencePlayer.PlaySequence(
            currentTransition.entrySequenceName,
            currentTransition.frameRate,
            sequenceDisplay,
            currentTransition.keepLastFrameForLoading ? CaptureLastFrame : null);

            StopLoadingDelayCoroutine();
            if (!loadingScreenVisible)
                ShowLoadingScreen();
        }

        private IEnumerator PlayExitSequence()
        {
            if (string.IsNullOrEmpty(currentTransition.exitSequenceName))
            {
                HideLoadingScreen();
                yield break;
            }

            PlayTransitionSound(currentTransition.exitSound);

            float sequenceDuration = GetSequenceDuration(currentTransition.exitSequenceName);
            float hideLoadingTime = sequenceDuration - currentTransition.hideLoadingBeforeEnd;

            Coroutine hideLoadingCoroutine = null;
            if (hideLoadingTime > 0)
            {
                hideLoadingCoroutine = StartCoroutine(HideLoadingAfterDelay(hideLoadingTime));
            }
            else
            {
                HideLoadingScreen();
            }

            yield return sequencePlayer.PlaySequence(
            currentTransition.exitSequenceName,
            currentTransition.frameRate,
            sequenceDisplay);

            if (hideLoadingCoroutine != null)
                StopCoroutine(hideLoadingCoroutine);

            if (loadingScreenVisible)
                HideLoadingScreen();
        }

        private IEnumerator PlayExitSequenceStandalone()
        {
            Debug.Log("=== PlayExitSequenceStandalone START ===");
    
            if (string.IsNullOrEmpty(currentTransition.exitSequenceName))
            {
                Debug.LogWarning("No exit sequence defined");
                yield return new WaitForSeconds(1f);
                yield break;
            }

            Debug.Log($"Playing exit sequence: {currentTransition.exitSequenceName}");

            Debug.Log("Playing transition sound...");
            PlayTransitionSound(currentTransition.exitSound);

            if (sequenceDisplay != null)
            {
                Debug.Log("Activating sequence display");
                sequenceDisplay.gameObject.SetActive(true);
                sequenceDisplay.transform.SetAsLastSibling();
        
                Debug.Log($"SequenceDisplay active: {sequenceDisplay.gameObject.activeInHierarchy}");
                Debug.Log($"SequenceDisplay parent: {sequenceDisplay.transform.parent?.name}");
                Debug.Log($"SequenceDisplay canvas: {sequenceDisplay.GetComponentInParent<Canvas>()?.name}");
            }
            else
            {
                Debug.LogError("SequenceDisplay is null!");
                yield break;
            }

            Debug.Log("Starting sequence playback...");
            yield return sequencePlayer.PlaySequence(
            currentTransition.exitSequenceName,
            currentTransition.frameRate,
            sequenceDisplay);
    
            Debug.Log("=== PlayExitSequenceStandalone END ===");
        }

        #endregion

        #region Scene Management

        private IEnumerator LoadingPhase(string targetSceneName)
        {
            yield return UnloadPreviousScenes();

            var loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                Debug.LogError($"Failed to load scene: {targetSceneName}");
                yield break;
            }

            loadOperation.allowSceneActivation = false;
            yield return ShowLoadingAnimation();
            yield return new WaitUntil(() => loadOperation.progress >= 0.9f);

            loadOperation.allowSceneActivation = true;
            yield return new WaitUntil(() => loadOperation.isDone);

            var newScene = SceneManager.GetSceneByName(targetSceneName);
            if (newScene.IsValid())
                SceneManager.SetActiveScene(newScene);
        }

        private IEnumerator UnloadPreviousScenes()
        {
            List<string> scenesToUnload = new List<string>();
            string currentSceneName = gameObject.scene.name;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name != currentSceneName && scene.name != "DontDestroyOnLoad")
                    scenesToUnload.Add(scene.name);
            }

            foreach (string sceneName in scenesToUnload)
                yield return SceneManager.UnloadSceneAsync(sceneName);
        }

        #endregion

        #region UI Management

        private void InitializeUI()
        {
            ActivateTransitionCanvas();
            ResetUIElements();
        }

        private void ActivateTransitionCanvas()
        {
            Debug.Log("=== ActivateTransitionCanvas START ===");
    
            if (transitionCanvas != null)
            {
                transitionCanvas.sortingOrder = 2000;
                transitionCanvas.gameObject.SetActive(true);
        
                Debug.Log($"Transition canvas activated");
                Debug.Log($"Canvas name: {transitionCanvas.name}");
                Debug.Log($"Canvas sorting order: {transitionCanvas.sortingOrder}");
                Debug.Log($"Canvas active: {transitionCanvas.gameObject.activeInHierarchy}");
                Debug.Log($"Canvas enabled: {transitionCanvas.enabled}");
            }
            else
            {
                Debug.LogError("Transition canvas is null!");
            }
    
            Debug.Log("=== ActivateTransitionCanvas END ===");
        }

        private void ResetUIElements()
        {
            if (sequenceDisplay != null)
                sequenceDisplay.gameObject.SetActive(false);
            if (backgroundPanel != null)
                backgroundPanel.gameObject.SetActive(false);
            if (loadingBarComponent != null)
                loadingBarComponent.gameObject.SetActive(false);

            loadingScreenVisible = false;
        }

        private void ShowLoadingScreen()
        {
            if (loadingScreenVisible) return;

            ConfigureBackground();
            ActivateBackground();
            ConfigureLoadingBar();

            loadingScreenVisible = true;
        }

        private void ConfigureBackground()
        {
            if (backgroundPanel == null) return;

            if (currentTransition.keepLastFrameForLoading &&
                backgroundPanel.sprite != null &&
                backgroundPanel.sprite.name.Contains("LastFrame"))
            {
                backgroundPanel.color = Color.white;
            }
            else
            {
                backgroundPanel.sprite = null;
                backgroundPanel.color = currentTransition.backgroundColor;
            }
        }

        private void ActivateBackground()
        {
            if (backgroundPanel != null)
                backgroundPanel.gameObject.SetActive(true);
        }

        private void ConfigureLoadingBar()
        {
            if (loadingBarComponent == null) return;

            loadingBarComponent.gameObject.SetActive(true);
            loadingBarComponent.transform.SetAsLastSibling();

            if (loadingBarComponent is MonoBehaviour loadingBarMono)
            {
                var loadingBarCanvas = loadingBarMono.GetComponent<Canvas>();
                if (loadingBarCanvas != null && transitionCanvas != null)
                {
                    loadingBarCanvas.sortingOrder = transitionCanvas.sortingOrder + 1;
                }

                var loadingBarImages = loadingBarMono.GetComponentsInChildren<Image>();
                foreach (var img in loadingBarImages)
                {
                    img.gameObject.SetActive(true);
                }
            }

            if (!currentTransition.useLoadingBarEntryAnimation)
                loadingBar?.ShowInstantly();
        }

        private void HideLoadingScreen()
        {
            if (!loadingScreenVisible) return;

            if (loadingBarComponent != null)
            {
                if (!currentTransition.useLoadingBarExitAnimation)
                    loadingBar?.HideInstantly();
                loadingBarComponent.gameObject.SetActive(false);
            }

            if (backgroundPanel != null)
                backgroundPanel.gameObject.SetActive(false);

            loadingScreenVisible = false;
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
            return true;
        }

        private float GetSequenceDuration(string sequenceName)
        {
            try
            {
                string sequencePath = System.IO.Path.Combine(Application.streamingAssetsPath, "Sequences", sequenceName, "frames");
                if (System.IO.Directory.Exists(sequencePath))
                {
                    string[] pngFiles = System.IO.Directory.GetFiles(sequencePath, "*.png");
                    return pngFiles.Length / currentTransition.frameRate;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not calculate sequence duration for {sequenceName}: {e.Message}");
            }
            return 3f;
        }

        private void PlayTransitionSound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip, currentTransition.soundVolume);
        }

        private void CaptureLastFrame()
        {
            if (sequenceDisplay?.sprite?.texture == null) return;

            var lastFrameTexture = sequenceDisplay.sprite.texture;
            var copyTexture = new Texture2D(lastFrameTexture.width, lastFrameTexture.height, lastFrameTexture.format, false);
            copyTexture.SetPixels(lastFrameTexture.GetPixels());
            copyTexture.Apply();

            var lastFrameSprite = Sprite.Create(copyTexture,
            new Rect(0, 0, copyTexture.width, copyTexture.height),
            new Vector2(0.5f, 0.5f));

            lastFrameSprite.name = "LastFrame";

            CleanupLastFrame();
            if (backgroundPanel != null)
                backgroundPanel.sprite = lastFrameSprite;
        }

        private void CleanupLastFrame()
        {
            if (backgroundPanel?.sprite != null && backgroundPanel.sprite.name.Contains("LastFrame"))
            {
                Destroy(backgroundPanel.sprite.texture);
                Destroy(backgroundPanel.sprite);
                backgroundPanel.sprite = null;
            }
        }

        private void StopLoadingDelayCoroutine()
        {
            if (loadingDelayCoroutine != null)
            {
                StopCoroutine(loadingDelayCoroutine);
                loadingDelayCoroutine = null;
            }
        }

        private IEnumerator ShowLoadingAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!loadingScreenVisible)
                ShowLoadingScreen();
            loadingDelayCoroutine = null;
        }

        private IEnumerator HideLoadingAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (loadingScreenVisible)
                HideLoadingScreen();
        }

        private IEnumerator ShowLoadingAnimation()
        {
            if (loadingBar == null)
            {
                float duration = transitionConfig.ShouldUseLongLoading() ? transitionConfig.longLoadingDuration : Random.Range(0.1f, 0.3f);
                yield return new WaitForSeconds(duration);
                yield break;
            }

            bool animationComplete = false;
            float loadingDuration = transitionConfig.ShouldUseLongLoading() ? transitionConfig.longLoadingDuration : Random.Range(0.1f, 0.3f);

            loadingBar.StartLoading(loadingDuration,
            currentTransition.loadingBarProfile,
            currentTransition.useLoadingBarEntryAnimation,
            currentTransition.useLoadingBarExitAnimation,
            () => { animationComplete = true; });

            yield return new WaitUntil(() => animationComplete);
        }

        private IEnumerator Cleanup()
        {
            HideLoadingScreen();
            CleanupLastFrame();

            if (transitionCanvas != null)
                transitionCanvas.gameObject.SetActive(false);

            yield return SceneManager.UnloadSceneAsync(gameObject.scene);
        }

        #endregion
        
        /// <summary>
        /// Sélectionne une transition selon des critères spécifiques
        /// Peut être étendu pour inclure de la logique métier
        /// </summary>
        public TransitionData SelectTransitionForContext(string context = "default")
        {
            if (transitionConfig == null || transitionConfig.availableTransitions == null)
                return null;

            switch (context.ToLower())
            {
                case "initial":
                case "startup":
                    // Logique pour la transition initiale
                    return SelectInitialTransition();
            
                case "menu":
                    // Logique pour les transitions de menu
                    return SelectMenuTransition();
            
                case "gameplay":
                    // Logique pour les transitions de gameplay
                    return SelectGameplayTransition();
            
                default:
                    // Transition aléatoire par défaut
                    return transitionConfig.GetRandomTransition();
            }
        }

        private TransitionData SelectInitialTransition()
        {
            // Exemple : utiliser la première transition disponible pour le démarrage
            var transitions = transitionConfig.availableTransitions;
            return transitions.Length > 0 ? transitions[0] : null;
        }

        private TransitionData SelectMenuTransition()
        {
            // Exemple : filtrer les transitions appropriées pour les menus
            var transitions = transitionConfig.availableTransitions;
            // Ici on pourrait filtrer par tags, noms, ou autres critères
            return transitions.Length > 0 ? transitions[Random.Range(0, transitions.Length)] : null;
        }

        private TransitionData SelectGameplayTransition()
        {
            // Exemple : logique pour les transitions de gameplay
            var transitions = transitionConfig.availableTransitions;
            return transitions.Length > 0 ? transitions[Random.Range(0, transitions.Length)] : null;
        }
    }
}
