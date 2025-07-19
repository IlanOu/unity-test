using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Features.LoadingBar;
using Features.Transitions.Sequences;
using Features.Transitions.Utils;

namespace Features.Transitions.UI
{
    public class TransitionUI : MonoBehaviour
    {
        [Header("UI Elements")] [SerializeField]
        private Canvas transitionCanvas;

        [SerializeField] private Image backgroundPanel;
        [SerializeField] private Image sequenceDisplay;
        [SerializeField] private MonoBehaviour loadingBarComponent;

        private ILoadingBar loadingBar;
        private SequencePlayer sequencePlayer;
        private bool loadingScreenVisible = false;
        private Sprite lastFrameSprite;
        
        private Canvas externalLoadingCanvas;
        private CanvasGroup externalCanvasGroup;

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            loadingBar = loadingBarComponent as ILoadingBar;

            sequencePlayer = GetComponent<SequencePlayer>();
            if (sequencePlayer == null)
            {
                sequencePlayer = gameObject.AddComponent<SequencePlayer>();
            }
        }

        public void SetExternalLoadingCanvas(Canvas canvas, CanvasGroup canvasGroup = null)
        {
            Debug.Log("=== SetExternalLoadingCanvas START ===");
            Debug.Log($"Received canvas: {canvas?.name}");
            Debug.Log($"Received canvasGroup: {canvasGroup?.name}");
    
            externalLoadingCanvas = canvas;
            externalCanvasGroup = canvasGroup;
    
            if (canvas != null)
            {
                Debug.Log($"External loading canvas set: {canvas.name} (enabled: {canvas.enabled})");
            }
            else if (canvasGroup != null)
            {
                Debug.Log($"External canvas group set: {canvasGroup.name} (active: {canvasGroup.gameObject.activeInHierarchy})");
            }
            else
            {
                Debug.LogWarning("Both canvas and canvasGroup are null!");
            }
            Debug.Log("=== SetExternalLoadingCanvas END ===");
        }

        public void SetupCanvas()
        {
            Debug.Log("=== SetupCanvas START ===");
            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(true);
                transitionCanvas.sortingOrder = 1000;
                Debug.Log("TransitionCanvas activated");
            }

            Debug.Log("Calling ResetUI...");
            ResetUI();
            Debug.Log("=== SetupCanvas END ===");
        }

        public void ResetUI()
        {
            Debug.Log("=== ResetUI START ===");
            if (sequenceDisplay != null)
            {
                sequenceDisplay.gameObject.SetActive(false);
                Debug.Log("SequenceDisplay deactivated");
            }

            if (backgroundPanel != null)
            {
                backgroundPanel.gameObject.SetActive(false);
                Debug.Log("BackgroundPanel deactivated");
            }

            if (loadingBarComponent != null)
            {
                loadingBarComponent.gameObject.SetActive(false);
                Debug.Log("LoadingBarComponent deactivated");
            }

            loadingScreenVisible = false;
            CleanupLastFrame();
            Debug.Log("=== ResetUI END ===");
        }

        public void SetupCanvasWithoutReset()
        {
            Debug.Log("=== SetupCanvasWithoutReset ===");
            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(true);
                transitionCanvas.sortingOrder = 1000;
            }
        }

        public IEnumerator PlayEntrySequence(TransitionData transitionData)
        {
            Debug.Log("=== TransitionUI.PlayEntrySequence START ===");

            if (transitionData == null || string.IsNullOrEmpty(transitionData.entrySequenceName))
            {
                ShowLoadingScreen(transitionData);
                yield break;
            }

            AudioHelper.PlayTransitionSound(GetComponent<AudioSource>(), transitionData.entrySound,
                transitionData.soundVolume);

            Coroutine loadingDelayCoroutine = null;
            if (transitionData.showLoadingDelay > 0)
            {
                loadingDelayCoroutine =
                    StartCoroutine(ShowLoadingScreenAfterDelay(transitionData.showLoadingDelay, transitionData));
            }

            if (sequencePlayer != null)
            {
                yield return sequencePlayer.PlaySequence(
                    transitionData.entrySequenceName,
                    transitionData.frameRate,
                    sequenceDisplay,
                    transitionData.keepLastFrameForLoading ? (() => CaptureLastFrame()) : null);
            }

            if (loadingDelayCoroutine != null)
            {
                StopCoroutine(loadingDelayCoroutine);
            }

            if (!loadingScreenVisible)
            {
                ShowLoadingScreen(transitionData);
            }

            Debug.Log("=== TransitionUI.PlayEntrySequence END ===");
        }

        public IEnumerator PlayExitSequence(TransitionData transitionData)
        {
            Debug.Log("=== TransitionUI.PlayExitSequence START ===");

            if (transitionData == null || string.IsNullOrEmpty(transitionData.exitSequenceName))
            {
                HideExternalLoadingCanvas(transitionData);
                yield return new WaitForSeconds(1f);
                yield break;
            }

            AudioHelper.PlayTransitionSound(GetComponent<AudioSource>(), transitionData.exitSound,
                transitionData.soundVolume);

            if (sequenceDisplay != null)
            {
                sequenceDisplay.gameObject.SetActive(true);
                if (!transitionData.useLoadingBarExitAnimation)
                {
                    backgroundPanel.gameObject.SetActive(false);
                    loadingBarComponent.gameObject.SetActive(false);
                }
            }

            float sequenceDuration =
                CalculateSequenceDuration(transitionData.exitSequenceName, transitionData.frameRate);
            float hideTime = sequenceDuration - transitionData.hideLoadingBeforeEnd;

            Debug.Log($"Sequence duration: {sequenceDuration}s, will hide external canvas at: {hideTime}s");

            Coroutine sequenceCoroutine = null;
            
            Coroutine loadingDelayCoroutine = null;
            if (hideTime > 0)
            {
                loadingDelayCoroutine = StartCoroutine(HideLoadingScreenAfterDelay(hideTime, transitionData));
            }
            // HideLoadingScreen(transitionData);
            
            
            if (sequencePlayer != null)
            {
                yield return sequencePlayer.PlaySequence(
                    transitionData.exitSequenceName,
                    transitionData.frameRate,
                    sequenceDisplay);
            }

            if (loadingDelayCoroutine != null)
            {
                StopCoroutine(loadingDelayCoroutine);
            }
            
            Coroutine hideTimerCoroutine = null;
            
            Debug.Log("=== TransitionUI.PlayExitSequence END ===");
        }
        
       
        
        private void HideExternalLoadingCanvas(TransitionData transitionData)
        {
            if (externalLoadingCanvas != null && externalLoadingCanvas != transitionCanvas)
            {
                externalLoadingCanvas.enabled = false;
            }
            else if (externalCanvasGroup != null)
            {
                externalCanvasGroup.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning(
                    "No external canvas to hide - both externalLoadingCanvas and externalCanvasGroup are null");
                HideLoadingScreen(transitionData);
            }
        }
        
        public IEnumerator ShowLoadingBarAnimation(UnityEngine.AsyncOperation loadOperation, float duration,
            TransitionData transitionData)
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
                transitionData.loadingBarProfile,
                transitionData.useLoadingBarEntryAnimation,
                transitionData.useLoadingBarExitAnimation,
                () => { animationComplete = true; }
            );

            yield return new WaitUntil(() => animationComplete);
            yield return new WaitUntil(() => loadOperation.progress >= 0.9f);
        }

        private void ShowLoadingScreen(TransitionData transitionData)
        {
            if (loadingScreenVisible || transitionData == null) return;

            if (backgroundPanel != null)
            {
                if (transitionData.keepLastFrameForLoading && lastFrameSprite != null)
                {
                    backgroundPanel.sprite = lastFrameSprite;
                    backgroundPanel.color = Color.white;
                }
                else
                {
                    backgroundPanel.sprite = null;
                    backgroundPanel.color = transitionData.backgroundColor;
                }

                backgroundPanel.gameObject.SetActive(true);
            }

            if (loadingBarComponent != null)
            {
                loadingBarComponent.gameObject.SetActive(true);

                if (loadingBarComponent is MonoBehaviour loadingBarMono)
                {
                    var loadingBarCanvas = loadingBarMono.GetComponent<Canvas>();
                    if (loadingBarCanvas != null && transitionCanvas != null)
                    {
                        loadingBarCanvas.sortingOrder = transitionCanvas.sortingOrder + 1;
                    }
                }

                if (!transitionData.useLoadingBarEntryAnimation && loadingBar != null)
                {
                    loadingBar.ShowInstantly();
                }
            }

            loadingScreenVisible = true;
        }

        private void HideLoadingScreen(TransitionData transitionData)
        {
            if (!loadingScreenVisible || transitionData == null) return;

            if (loadingBarComponent != null)
            {
                if (!transitionData.useLoadingBarExitAnimation && loadingBar != null)
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

        private IEnumerator ShowLoadingScreenAfterDelay(float delay, TransitionData transitionData)
        {
            yield return new WaitForSeconds(delay);
            ShowLoadingScreen(transitionData);
        }

        private IEnumerator HideLoadingScreenAfterDelay(float delay, TransitionData transitionData)
        {
            yield return new WaitForSeconds(delay);
            // HideLoadingScreen(transitionData);
            HideExternalLoadingCanvas(transitionData);
        }

        private void CaptureLastFrame()
        {
            if (sequenceDisplay?.sprite?.texture == null) return;

            var lastFrameTexture = sequenceDisplay.sprite.texture;
            var copyTexture = new Texture2D(lastFrameTexture.width, lastFrameTexture.height, lastFrameTexture.format,
                false);
            copyTexture.SetPixels(lastFrameTexture.GetPixels());
            copyTexture.Apply();

            CleanupLastFrame();

            lastFrameSprite = Sprite.Create(copyTexture,
                new Rect(0, 0, copyTexture.width, copyTexture.height),
                new Vector2(0.5f, 0.5f));

            lastFrameSprite.name = "LastFrame";
        }

        private void CleanupLastFrame()
        {
            if (lastFrameSprite != null)
            {
                if (lastFrameSprite.texture != null)
                {
                    Destroy(lastFrameSprite.texture);
                }

                Destroy(lastFrameSprite);
                lastFrameSprite = null;
            }
        }

        private float CalculateSequenceDuration(string sequenceName, float frameRate)
        {
            try
            {
                string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Sequences", sequenceName,
                    "frames");
                if (System.IO.Directory.Exists(path))
                {
                    string[] files = System.IO.Directory.GetFiles(path, "*.png");
                    return files.Length / frameRate;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not calculate duration for {sequenceName}: {e.Message}");
            }

            return 3f;
        }

        public void Cleanup()
        {
            CleanupLastFrame();
            if (transitionCanvas != null)
                transitionCanvas.gameObject.SetActive(false);
        }

        public bool ValidateSetup()
        {
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
    }
}