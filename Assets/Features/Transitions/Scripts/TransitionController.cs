using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Features.LoadingBar;

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

        [Header("Loading Bar")]
        [SerializeField] private MonoBehaviour loadingBarComponent;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        private ILoadingBar loadingBar;
        private TransitionData currentTransition;
        private SequencePlayer sequencePlayer;
        private bool loadingScreenVisible = false;

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
            }
            
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
            }

            sequencePlayer = GetComponent<SequencePlayer>();
            if (sequencePlayer == null)
            {
                sequencePlayer = gameObject.AddComponent<SequencePlayer>();
            }
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

            SetupCanvas();
            onStart?.Invoke();

            if (preloadedScene != null)
            {
                preloadedScene.allowSceneActivation = true;
                yield return preloadedScene;
                yield return null;
            }

            yield return PlayExitSequence();
        }

        public IEnumerator ExecuteTransition(string targetSceneName)
        {
            if (!ValidateSetup()) yield break;

            currentTransition = transitionConfig.GetRandomTransition();
            if (currentTransition == null) yield break;

            SetupCanvas();
            yield return PlayEntrySequence();
            yield return LoadSceneWithLoadingBar(targetSceneName);
            yield return PlayExitSequence();
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

        private IEnumerator PlayEntrySequence()
        {
            if (currentTransition == null)
            {
                Debug.LogError("CurrentTransition is null in PlayEntrySequence!");
                yield break;
            }

            if (string.IsNullOrEmpty(currentTransition.entrySequenceName))
            {
                ShowLoadingScreen();
                yield break;
            }

            PlaySound(currentTransition.entrySound);
            
            Coroutine loadingDelayCoroutine = null;
            if (currentTransition.showLoadingDelay > 0)
            {
                loadingDelayCoroutine = StartCoroutine(ShowLoadingScreenAfterDelay(currentTransition.showLoadingDelay));
            }

            if (sequencePlayer != null)
            {
                yield return sequencePlayer.PlaySequence(
                    currentTransition.entrySequenceName,
                    currentTransition.frameRate,
                    sequenceDisplay,
                    currentTransition.keepLastFrameForLoading ? CaptureLastFrame : null);
            }

            if (loadingDelayCoroutine != null)
            {
                StopCoroutine(loadingDelayCoroutine);
            }

            if (!loadingScreenVisible)
            {
                ShowLoadingScreen();
            }
        }

        private IEnumerator PlayExitSequence()
        {
            if (currentTransition == null)
            {
                Debug.LogError("CurrentTransition is null in PlayExitSequence!");
                yield return new WaitForSeconds(1f);
                yield break;
            }

            if (string.IsNullOrEmpty(currentTransition.exitSequenceName))
            {
                HideLoadingScreen();
                yield return new WaitForSeconds(1f);
                yield break;
            }

            PlaySound(currentTransition.exitSound);
            
            if (sequenceDisplay != null)
            {
                sequenceDisplay.gameObject.SetActive(true);
                sequenceDisplay.transform.SetAsLastSibling();
            }

            float sequenceDuration = GetSequenceDuration(currentTransition.exitSequenceName);
            float hideTime = sequenceDuration - currentTransition.hideLoadingBeforeEnd;

            Coroutine hideCoroutine = null;
            if (currentTransition.hideLoadingBeforeEnd > 0)
            {
                if (hideTime > 0)
                {
                    hideCoroutine = StartCoroutine(HideLoadingScreenAfterDelay(hideTime));
                }
                else
                {
                    HideLoadingScreen();
                }
            }

            if (sequencePlayer != null)
            {
                yield return sequencePlayer.PlaySequence(
                    currentTransition.exitSequenceName,
                    currentTransition.frameRate,
                    sequenceDisplay);
            }

            if (hideCoroutine != null) StopCoroutine(hideCoroutine);
            
            if (loadingScreenVisible)
            {
                HideLoadingScreen();
            }
        }

        private IEnumerator LoadSceneWithLoadingBar(string targetSceneName)
        {
            yield return UnloadAllScenesExceptTransition();

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

            yield return ShowLoadingBarAnimation(loadOp, loadingDuration);

            loadOp.allowSceneActivation = true;
            yield return loadOp;

            var newScene = SceneManager.GetSceneByName(targetSceneName);
            if (newScene.IsValid()) SceneManager.SetActiveScene(newScene);
        }

        private IEnumerator ShowLoadingBarAnimation(AsyncOperation loadOperation, float duration)
        {
            if (loadingBar == null)
            {
                yield return new WaitForSeconds(duration);
                yield return new WaitUntil(() => loadOperation.progress >= 0.9f);
                yield break;
            }

            bool animationComplete = false;

            loadingBar.StartLoading(
                duration,
                currentTransition.loadingBarProfile,
                currentTransition.useLoadingBarEntryAnimation,
                currentTransition.useLoadingBarExitAnimation,
                () => { animationComplete = true; }
            );

            yield return new WaitUntil(() => animationComplete);
            yield return new WaitUntil(() => loadOperation.progress >= 0.9f);
        }

        private IEnumerator UnloadAllScenesExceptTransition()
        {
            List<string> scenesToUnload = new List<string>();
            string transitionSceneName = gameObject.scene.name;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && 
                    scene.name != transitionSceneName && 
                    scene.name != "DontDestroyOnLoad")
                {
                    scenesToUnload.Add(scene.name);
                }
            }

            foreach (string sceneName in scenesToUnload)
            {
                Debug.Log($"Unloading scene: {sceneName}");
                yield return SceneManager.UnloadSceneAsync(sceneName);
            }

            System.GC.Collect();
            yield return Resources.UnloadUnusedAssets();
        }

        #endregion

        #region UI Management

        private void SetupCanvas()
        {
            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(true);
                transitionCanvas.sortingOrder = 1000;
            }
            
            ResetUI();
        }

        private void ResetUI()
        {
            if (sequenceDisplay != null) sequenceDisplay.gameObject.SetActive(false);
            if (backgroundPanel != null) backgroundPanel.gameObject.SetActive(false);
            if (loadingBarComponent != null) loadingBarComponent.gameObject.SetActive(false);
            loadingScreenVisible = false;
        }

        private void ShowLoadingScreen()
        {
            if (loadingScreenVisible) return;

            if (backgroundPanel != null && currentTransition != null)
            {
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
                backgroundPanel.gameObject.SetActive(true);
            }

            if (loadingBarComponent != null)
            {
                loadingBarComponent.gameObject.SetActive(true);
                loadingBarComponent.transform.SetAsLastSibling();
                
                if (loadingBarComponent is MonoBehaviour loadingBarMono)
                {
                    var loadingBarCanvas = loadingBarMono.GetComponent<Canvas>();
                    if (loadingBarCanvas != null && transitionCanvas != null)
                    {
                        loadingBarCanvas.sortingOrder = transitionCanvas.sortingOrder + 1;
                    }
                }

                if (!currentTransition.useLoadingBarEntryAnimation && loadingBar != null)
                {
                    loadingBar.ShowInstantly();
                }
            }

            loadingScreenVisible = true;
        }

        private void HideLoadingScreen()
        {
            if (!loadingScreenVisible) return;

            if (loadingBarComponent != null)
            {
                if (!currentTransition.useLoadingBarExitAnimation && loadingBar != null)
                {
                    loadingBar.HideInstantly();
                }
                loadingBarComponent.gameObject.SetActive(false);
            }

            if (backgroundPanel != null)
            {
                backgroundPanel.gameObject.SetActive(false);
            }

            loadingScreenVisible = false;
        }

        private IEnumerator ShowLoadingScreenAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ShowLoadingScreen();
        }

        private IEnumerator HideLoadingScreenAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideLoadingScreen();
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

        #endregion

        #region Helper Methods

        private bool ValidateSetup()
        {
            if (transitionConfig == null)
            {
                Debug.LogError("TransitionConfig not assigned!");
                return false;
            }

            if (transitionCanvas == null)
            {
                Debug.LogError("TransitionCanvas not assigned!");
                return false;
            }

            if (sequenceDisplay == null)
            {
                Debug.LogError("SequenceDisplay not assigned!");
                return false;
            }

            return true;
        }

        private float GetSequenceDuration(string sequenceName)
        {
            if (currentTransition == null) return 3f;

            try
            {
                string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Sequences", sequenceName, "frames");
                if (System.IO.Directory.Exists(path))
                {
                    string[] files = System.IO.Directory.GetFiles(path, "*.png");
                    return files.Length / currentTransition.frameRate;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not calculate duration for {sequenceName}: {e.Message}");
            }
            return 3f;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null && currentTransition != null)
            {
                audioSource.PlayOneShot(clip, currentTransition.soundVolume);
            }
        }

        private IEnumerator Cleanup()
        {
            CleanupLastFrame();
            if (transitionCanvas != null)
                transitionCanvas.gameObject.SetActive(false);
            
            string currentSceneName = gameObject.scene.name;
            if (!string.IsNullOrEmpty(currentSceneName))
            {
                yield return SceneManager.UnloadSceneAsync(currentSceneName);
            }
        }

        #endregion
    }
}