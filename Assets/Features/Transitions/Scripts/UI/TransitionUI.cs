using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Features.LoadingBar;
using Features.Transitions.Sequences;
using Features.Transitions.Utils;
using System;

namespace Features.Transitions.UI
{
    [RequireComponent(typeof(SequencePlayer))]
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
        }

        public void SetExternalLoadingCanvas(Canvas canvas, CanvasGroup canvasGroup = null)
        {
            externalLoadingCanvas = canvas;
            externalCanvasGroup = canvasGroup;
        }

        public void SetupCanvas()
        {
            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(true);
                transitionCanvas.sortingOrder = 1000;
            }

            ResetUI();
        }

        public void SetupCanvasWithoutReset()
        {
            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(true);
                transitionCanvas.sortingOrder = 1000;
            }
        }

        public void ResetUI()
        {
            if (sequenceDisplay != null) sequenceDisplay.gameObject.SetActive(false);
            if (backgroundPanel != null) backgroundPanel.gameObject.SetActive(false);
            if (loadingBarComponent != null) loadingBarComponent.gameObject.SetActive(false);
            loadingScreenVisible = false;
            CleanupLastFrame();
        }

        public IEnumerator PlayEntrySequence(TransitionData transitionData)
        {
            if (transitionData == null || string.IsNullOrEmpty(transitionData.entrySequenceName))
            {
                ShowLoadingScreen(transitionData);
                yield break;
            }

            AudioHelper.PlayTransitionSound(GetComponent<AudioSource>(), transitionData.entrySound,
                transitionData.soundVolume);

            Coroutine loadingDelayCoroutine = null;
            if (transitionData.showLoadingDelay > 0 && !transitionData.keepLastFrameForLoading)
            {
                loadingDelayCoroutine =
                    StartCoroutine(ShowLoadingScreenAfterDelay(transitionData.showLoadingDelay, transitionData));
            }

            // --- NOUVELLE LOGIQUE POUR keepLastFrameForLoading ---
            Action onLastFrameCallback = null;
            if (transitionData.keepLastFrameForLoading)
            {
                onLastFrameCallback = () =>
                {
                    CaptureLastFrame();
                    // On cache le lecteur de séquence pour révéler ce qu'il y a derrière (le background panel)
                    if (sequenceDisplay != null)
                    {
                        sequenceDisplay.gameObject.SetActive(false);
                    }

                    // On affiche immédiatement l'écran de chargement, qui va utiliser la frame capturée.
                    ShowLoadingScreen(transitionData);
                };
            }

            yield return sequencePlayer.PlaySequence(
                transitionData.entrySequenceName,
                transitionData.frameRate,
                sequenceDisplay,
                onLastFrameCallback
            );

            if (loadingDelayCoroutine != null) StopCoroutine(loadingDelayCoroutine);

            // Si on n'utilise pas keepLastFrame, on affiche le loading screen à la fin de la séquence normale.
            if (!loadingScreenVisible)
            {
                ShowLoadingScreen(transitionData);
            }
        }

        public IEnumerator PlayExitSequence(TransitionData transitionData)
        {
            if (transitionData == null || string.IsNullOrEmpty(transitionData.exitSequenceName))
            {
                HideExternalLoadingCanvas(transitionData);
                yield return new WaitForSeconds(0.5f);
                yield break;
            }

            // Avant de jouer la séquence de sortie, on s'assure que l'ancien fond est nettoyé
            CleanupLastFrame();
            if (backgroundPanel != null) backgroundPanel.sprite = null;

            AudioHelper.PlayTransitionSound(GetComponent<AudioSource>(), transitionData.exitSound,
                transitionData.soundVolume);

            if (sequenceDisplay != null)
            {
                sequenceDisplay.gameObject.SetActive(true);
                if (!transitionData.useLoadingBarExitAnimation)
                {
                    if (backgroundPanel != null) backgroundPanel.gameObject.SetActive(false);
                    if (loadingBarComponent != null) loadingBarComponent.gameObject.SetActive(false);
                }
            }

            Sprite[] sequence = SequenceProvider.Instance.GetSequence(transitionData.exitSequenceName);
            int frameCount = sequence?.Length ?? 0;
            float sequenceDuration = (transitionData.frameRate > 0 && frameCount > 0)
                ? frameCount / transitionData.frameRate
                : 0;
            float hideTime = sequenceDuration - transitionData.hideLoadingBeforeEnd;

            if (hideTime > 0)
            {
                StartCoroutine(HideExternalCanvasAfterDelay(hideTime, transitionData));
            }
            else
            {
                HideExternalLoadingCanvas(transitionData);
            }

            if (sequencePlayer != null)
            {
                yield return sequencePlayer.PlaySequence(
                    transitionData.exitSequenceName,
                    transitionData.frameRate,
                    sequenceDisplay
                );
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
            loadingBar.StartLoading(duration, transitionData.loadingBarProfile,
                transitionData.useLoadingBarEntryAnimation, transitionData.useLoadingBarExitAnimation,
                () => { animationComplete = true; });
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
                var loadingBarCanvas = loadingBarComponent.GetComponent<Canvas>();
                if (loadingBarCanvas != null && transitionCanvas != null)
                {
                    loadingBarCanvas.sortingOrder = transitionCanvas.sortingOrder + 1;
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

        private void HideExternalLoadingCanvas(TransitionData transitionData)
        {
            if (externalLoadingCanvas != null && externalLoadingCanvas != transitionCanvas)
            {
                externalLoadingCanvas.enabled = false;
            }
            else if (externalCanvasGroup != null)
            {
                externalCanvasGroup.alpha = 0f;
                externalCanvasGroup.interactable = false;
                externalCanvasGroup.blocksRaycasts = false;
            }
            else
            {
                HideLoadingScreen(transitionData);
            }
        }

        private IEnumerator ShowLoadingScreenAfterDelay(float delay, TransitionData transitionData)
        {
            yield return new WaitForSeconds(delay);
            ShowLoadingScreen(transitionData);
        }

        private IEnumerator HideExternalCanvasAfterDelay(float delay, TransitionData transitionData)
        {
            yield return new WaitForSeconds(delay);
            HideExternalLoadingCanvas(transitionData);
        }

        private void CaptureLastFrame()
        {
            if (sequenceDisplay?.sprite?.texture == null)
            {
                Debug.LogWarning("CaptureLastFrame called, but sequenceDisplay has no valid sprite.");
                return;
            }

            var sourceTexture = sequenceDisplay.sprite.texture;
            RenderTexture rt = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0);
            Graphics.Blit(sourceTexture, rt);

            Texture2D readableTexture =
                new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);
            RenderTexture.active = rt;
            readableTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            readableTexture.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            CleanupLastFrame();
            lastFrameSprite = Sprite.Create(readableTexture,
                new Rect(0, 0, readableTexture.width, readableTexture.height), new Vector2(0.5f, 0.5f));
            lastFrameSprite.name = "LastFrameCopy";
        }

        private void CleanupLastFrame()
        {
            if (lastFrameSprite != null)
            {
                if (lastFrameSprite.texture != null) Destroy(lastFrameSprite.texture);
                Destroy(lastFrameSprite);
                lastFrameSprite = null;
            }
        }

        public void Cleanup()
        {
            CleanupLastFrame();
            if (transitionCanvas != null) transitionCanvas.gameObject.SetActive(false);
        }
    }
}