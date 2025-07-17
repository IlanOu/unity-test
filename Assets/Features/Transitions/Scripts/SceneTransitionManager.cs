using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private TransitionConfig transitionConfig;
    
    [Header("UI Elements")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private Image backgroundPanel;
    [SerializeField] private ILoadingBar loadingBar;
    [SerializeField] private Canvas transitionCanvas;
    
    private static SceneTransitionManager instance;
    public static SceneTransitionManager Instance => instance;
    
    private bool isTransitioning = false;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SetupVideoPlayer();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void SetupVideoPlayer()
    {
        if (videoPlayer == null) return;
        
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        
        // Créer une RenderTexture si nécessaire
        if (videoPlayer.targetTexture == null)
        {
            RenderTexture rt = new RenderTexture(1920, 1080, 0);
            videoPlayer.targetTexture = rt;
            if (videoDisplay != null)
                videoDisplay.texture = rt;
        }
    }
    
    public void TransitionToScene(string sceneName)
    {
        if (isTransitioning) return;
        
        StartCoroutine(TransitionCoroutine(sceneName));
    }
    
    private IEnumerator TransitionCoroutine(string sceneName)
    {
        isTransitioning = true;
        
        // Sélectionner une transition aléatoire
        TransitionData transition = transitionConfig.GetRandomTransition();
        if (transition == null)
        {
            Debug.LogError("Aucune transition disponible!");
            yield break;
        }
        
        // Déterminer la durée de chargement
        bool useLongLoading = transitionConfig.ShouldUseLongLoading();
        float loadingDuration = useLongLoading ? transitionConfig.longLoadingDuration : 
                               Random.Range(transition.minTransitionDuration, transition.maxTransitionDuration);
        
        // Activer le canvas de transition
        transitionCanvas.gameObject.SetActive(true);
        
        // Phase 1: Vidéo de sortie
        yield return StartCoroutine(PlayExitVideo(transition));
        
        // Phase 2: Chargement de la scène avec vidéo d'entrée
        yield return StartCoroutine(LoadSceneWithTransition(sceneName, transition, loadingDuration));
        
        // Phase 3: Finaliser la transition
        yield return StartCoroutine(FinishTransition());
        
        isTransitioning = false;
    }
    
    private IEnumerator PlayExitVideo(TransitionData transition)
    {
        if (transition.exitVideo == null) yield break;
        
        SetupBackground(transition);
        
        videoPlayer.clip = transition.exitVideo;
        videoDisplay.gameObject.SetActive(true);
        videoPlayer.Play();
        
        while (videoPlayer.isPlaying)
        {
            yield return null;
        }
    }
    
    private IEnumerator LoadSceneWithTransition(string sceneName, TransitionData transition, float loadingDuration)
    {
        // Démarrer le chargement asynchrone de la scène
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;
        
        if (transition.entryVideo != null)
        {
            videoPlayer.clip = transition.entryVideo;
            videoPlayer.Play();
        }
        
        if (transition.keepLastFrameForLoading && loadingBar != null)
        {
            loadingBar.Show();
        }
        else
        {
            videoDisplay.gameObject.SetActive(false);
            backgroundPanel.gameObject.SetActive(true);
            if (loadingBar != null)
                loadingBar.Show();
        }
        
        float elapsedTime = 0f;
        while (elapsedTime < loadingDuration || asyncLoad.progress < 0.9f)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Min(elapsedTime / loadingDuration, asyncLoad.progress / 0.9f);
            
            if (loadingBar != null)
                loadingBar.UpdateProgress(progress);
            
            yield return null;
        }
        
        // Finaliser le chargement
        if (loadingBar != null)
        {
            loadingBar.UpdateProgress(1f);
            yield return new WaitForSeconds(0.2f); // Petit délai pour voir la barre complète
        }
        
        // Activer la nouvelle scène
        asyncLoad.allowSceneActivation = true;
        
        // Attendre que la scène soit complètement chargée
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
    
    private IEnumerator FinishTransition()
    {
        if (loadingBar != null)
            loadingBar.Hide();
        
        videoPlayer.Stop();
        videoDisplay.gameObject.SetActive(false);
        backgroundPanel.gameObject.SetActive(false);
        transitionCanvas.gameObject.SetActive(false);
        
        yield return null;
    }
    
    private void SetupBackground(TransitionData transition)
    {
        if (backgroundPanel != null)
        {
            backgroundPanel.color = transition.backgroundColor;
            backgroundPanel.gameObject.SetActive(true);
        }
        
        if (loadingBar is SimpleLoadingBar simpleBar)
        {
            simpleBar.SetBackgroundColor(transition.backgroundColor);
        }
    }
}