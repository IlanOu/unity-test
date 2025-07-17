using System.Collections;
using System.Collections.Generic;
using Features.LoadingBar;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Features.Transitions
{
    public class TransitionController : MonoBehaviour
    {
        [Header("Configuration")] [SerializeField]
        private TransitionConfig transitionConfig;

        [Header("UI Elements")] [SerializeField]
        private Canvas transitionCanvas;

        [SerializeField] private Image backgroundPanel;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage videoDisplay;

        [Header("Audio")] [SerializeField] private AudioSource audioSource;

        [Header("Loading Bar")] [SerializeField]
        private MonoBehaviour loadingBarComponent;

        private ILoadingBar loadingBar;

        private TransitionData currentTransition;
        private bool loadingScreenVisible = false;

        private void Awake()
        {
            loadingBar = loadingBarComponent as ILoadingBar;
            if (loadingBar == null && loadingBarComponent != null)
            {
                Debug.LogError("Loading bar component doesn't implement ILoadingBar interface!");
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        private void OnValidate()
        {
            if (transitionConfig == null)
                Debug.LogWarning("TransitionConfig is not assigned!");

            if (videoPlayer == null)
                Debug.LogWarning("VideoPlayer is not assigned!");

            if (transitionCanvas == null)
                Debug.LogWarning("TransitionCanvas is not assigned!");
        }

        private void OnDestroy()
        {
            if (videoPlayer?.targetTexture != null)
            {
                videoPlayer.targetTexture.Release();
                Destroy(videoPlayer.targetTexture);
            }

            if (backgroundPanel != null && backgroundPanel.sprite != null &&
                backgroundPanel.sprite.name.Contains("LastFrame"))
            {
                Destroy(backgroundPanel.sprite.texture);
                Destroy(backgroundPanel.sprite);
            }
        }

        public IEnumerator ExecuteTransition(string targetSceneName)
        {
            InitializeUI();
            SetupVideoPlayer();

            transitionCanvas.gameObject.SetActive(true);
            yield return null;

            currentTransition = transitionConfig.GetRandomTransition();
            if (currentTransition == null)
            {
                Debug.LogError("No transition data available!");
                yield return Cleanup();
                yield break;
            }

            yield return PlayEntryVideoWithLoading();

            yield return LoadingPhase(targetSceneName);

            yield return PlayExitVideoWithLoading();

            yield return Cleanup();
        }

        private void InitializeUI()
        {
            transitionCanvas.sortingOrder = 1000;
            transitionCanvas.gameObject.SetActive(true);

            videoDisplay.gameObject.SetActive(false);
            backgroundPanel.gameObject.SetActive(false);
            if (loadingBarComponent != null)
                loadingBarComponent.gameObject.SetActive(false);

            loadingScreenVisible = false;
        }

        private IEnumerator PlayEntryVideoWithLoading()
        {
            if (string.IsNullOrEmpty(currentTransition.entryVideoFileName)) yield break;

            transitionCanvas.gameObject.SetActive(true);
            yield return null;
            
            PlayTransitionSound(currentTransition.entrySound);
            
            var videoCoroutine = StartCoroutine(PlayVideoWithLastFrame(true));
            var loadingTimerCoroutine = StartCoroutine(ShowLoadingAfterDelay(currentTransition.showLoadingDelay));
            
            yield return videoCoroutine;
            
            if (loadingTimerCoroutine != null)
                StopCoroutine(loadingTimerCoroutine);
            
            if (!loadingScreenVisible)
            {
                ShowLoadingScreen();
            }
        }

        private IEnumerator ShowLoadingAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (!loadingScreenVisible)
            {
                ShowLoadingScreen();
            }
        }

        private IEnumerator LoadingPhase(string targetSceneName)
        {
            yield return UnloadPreviousScenes();

            var loadOperation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                Debug.LogError($"Failed to load scene: {targetSceneName}");
                yield return Cleanup();
                yield break;
            }

            loadOperation.allowSceneActivation = false;

            yield return ShowLoadingAnimation();

            yield return new WaitUntil(() => loadOperation.progress >= 0.9f);

            loadOperation.allowSceneActivation = true;
            yield return new WaitUntil(() => loadOperation.isDone);

            var newScene = SceneManager.GetSceneByName(targetSceneName);
            if (newScene.IsValid())
            {
                SceneManager.SetActiveScene(newScene);
            }
        }

        private IEnumerator PlayExitVideoWithLoading()
        {
            if (string.IsNullOrEmpty(currentTransition.exitVideoFileName))
            {
                HideLoadingScreen();
                yield break;
            }
            
            PlayTransitionSound(currentTransition.exitSound);
            
            var videoCoroutine = StartCoroutine(PlayVideoWithLastFrame(false));
            var hideTimerCoroutine = StartCoroutine(HideLoadingAfterDelay(currentTransition.hideLoadingBeforeEnd));
            
            yield return videoCoroutine;
            
            if (hideTimerCoroutine != null)
                StopCoroutine(hideTimerCoroutine);
            
            if (loadingScreenVisible)
            {
                HideLoadingScreen();
            }
        }

        private void PlayTransitionSound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, currentTransition.soundVolume);
            }
        }

        private IEnumerator HideLoadingAfterDelay(float delayFromEnd)
        {
            float estimatedVideoDuration = 3f;
            float waitTime = estimatedVideoDuration - delayFromEnd;

            if (waitTime > 0)
            {
                yield return new WaitForSeconds(waitTime);
            }

            if (loadingScreenVisible)
            {
                HideLoadingScreen();
            }
        }

        private IEnumerator PlayVideoWithLastFrame(bool captureLastFrame)
        {
            string filename = captureLastFrame ? currentTransition.entryVideoFileName : currentTransition.exitVideoFileName;
            if (string.IsNullOrEmpty(filename)) yield break;
            
            string url = Application.streamingAssetsPath + "/Videos/" + filename;
            yield return PlayVideoFromURL(url, captureLastFrame);
        }

        private IEnumerator PlayVideoFromURL(string url, bool captureLastFrame)
        {
            transitionCanvas.gameObject.SetActive(true);
            videoDisplay.gameObject.SetActive(true);
            videoDisplay.transform.SetAsLastSibling();
            
            videoDisplay.color = new Color(1, 1, 1, 1);
            
            yield return null;
            
            videoPlayer.url = url;
            videoPlayer.Prepare();
            
            Debug.Log($"Loading video from: {url}");
            
            float timeout = 10f;
            float timer = 0f;
            
            while (!videoPlayer.isPrepared && timer < timeout)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            
            if (!videoPlayer.isPrepared)
            {
                Debug.LogWarning($"Video failed to load: {url}");
                videoDisplay.gameObject.SetActive(false);
                yield break;
            }
            
            yield return null;
            
            videoPlayer.Play();
            
            while (videoPlayer.isPlaying)
                yield return null;
            
            if (captureLastFrame && currentTransition.keepLastFrameForLoading)
            {
                CaptureLastFrame();
            }
            
            videoDisplay.gameObject.SetActive(false);
        }

        private void CaptureLastFrame()
        {
            if (videoPlayer.targetTexture == null) return;

            RenderTexture currentRT = RenderTexture.active;
            RenderTexture.active = videoPlayer.targetTexture;

            Texture2D lastFrameTexture2D =
                new Texture2D(videoPlayer.targetTexture.width, videoPlayer.targetTexture.height, TextureFormat.ARGB32, false);
            lastFrameTexture2D.ReadPixels(
                new Rect(0, 0, videoPlayer.targetTexture.width, videoPlayer.targetTexture.height),
                0, 0);
            lastFrameTexture2D.Apply();

            RenderTexture.active = currentRT;

            Sprite lastFrameSprite = Sprite.Create(lastFrameTexture2D,
                new Rect(0, 0, lastFrameTexture2D.width, lastFrameTexture2D.height),
                new Vector2(0.5f, 0.5f));

            if (lastFrameSprite != null)
            {
                if (backgroundPanel.sprite != null && backgroundPanel.sprite.name.Contains("LastFrame"))
                {
                    Destroy(backgroundPanel.sprite.texture);
                    Destroy(backgroundPanel.sprite);
                }

                lastFrameSprite.name = "LastFrame";
                backgroundPanel.sprite = lastFrameSprite;
            }
        }

        private void ShowLoadingScreen()
        {
            if (loadingScreenVisible) return;

            if (currentTransition.keepLastFrameForLoading && backgroundPanel.sprite != null &&
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

            if (loadingBarComponent != null)
            {
                loadingBarComponent.gameObject.SetActive(true);

                if (!currentTransition.useLoadingBarEntryAnimation)
                {
                    loadingBar?.ShowInstantly();
                }
            }

            loadingScreenVisible = true;
        }

        private IEnumerator ShowLoadingAnimation()
        {
            if (loadingBar == null)
            {
                float duration = transitionConfig.ShouldUseLongLoading()
                    ? transitionConfig.longLoadingDuration
                    : Random.Range(0.1f, 0.3f);

                yield return new WaitForSeconds(duration);
                yield break;
            }

            bool animationComplete = false;

            float loadingDuration = transitionConfig.ShouldUseLongLoading()
                ? transitionConfig.longLoadingDuration
                : Random.Range(0.1f, 0.3f);

            loadingBar.StartLoading(
                loadingDuration,
                currentTransition.loadingBarProfile,
                currentTransition.useLoadingBarEntryAnimation,
                currentTransition.useLoadingBarExitAnimation,
                () => { animationComplete = true; }
            );

            yield return new WaitUntil(() => animationComplete);
        }

        private void HideLoadingScreen()
        {
            if (!loadingScreenVisible) return;

            if (loadingBarComponent != null)
            {
                if (!currentTransition.useLoadingBarExitAnimation)
                {
                    loadingBar?.HideInstantly();
                }

                loadingBarComponent.gameObject.SetActive(false);
            }

            backgroundPanel.gameObject.SetActive(false);
            loadingScreenVisible = false;
        }

        private IEnumerator Cleanup()
        {
            HideLoadingScreen();

            if (backgroundPanel.sprite != null && backgroundPanel.sprite.name.Contains("LastFrame"))
            {
                Destroy(backgroundPanel.sprite.texture);
                Destroy(backgroundPanel.sprite);
                backgroundPanel.sprite = null;
            }

            if (transitionCanvas != null)
                transitionCanvas.gameObject.SetActive(false);

            yield return SceneManager.UnloadSceneAsync(gameObject.scene);
        }

        private IEnumerator UnloadPreviousScenes()
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
                yield return SceneManager.UnloadSceneAsync(sceneName);
            }
        }

        private void SetupVideoPlayer()
        {
            if (videoPlayer == null) return;

            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.SetDirectAudioMute(0, true);
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.aspectRatio = VideoAspectRatio.NoScaling;

            if (videoPlayer.targetTexture == null)
            {
                RenderTexture rt = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
                rt.useMipMap = false;
                rt.autoGenerateMips = false;
                videoPlayer.targetTexture = rt;
                videoDisplay.texture = rt;
            }
        }
    }
}