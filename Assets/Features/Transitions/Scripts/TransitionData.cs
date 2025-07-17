using Features.LoadingBar;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "TransitionData", menuName = "Transitions/Transition Data")]
public class TransitionData : ScriptableObject
{
    [Header("Vidéos de transition")]
    public VideoClip entryVideo;
    public VideoClip exitVideo;
    
    [Header("Audio")]
    [Tooltip("Son joué au début de la vidéo start")]
    public AudioClip entrySound;
    [Tooltip("Son joué au début de la vidéo end")]
    public AudioClip exitSound;
    [Range(0f, 1f)]
    public float soundVolume = 1f;
    
    [Header("Loading Bar Style")]
    public LoadingAnimationProfile loadingBarProfile;

    [Header("Background Configuration")]
    public Color backgroundColor = Color.black;
    
    [Header("Timing Configuration")]
    [Range(0f, 5f)]
    public float minTransitionDuration = 1f;
    [Range(0f, 10f)]
    public float maxTransitionDuration = 3f;
    
    [Header("Loading Screen Timing")]
    [Tooltip("Temps après le début de la vidéo start pour afficher le loading")]
    [Range(0f, 10f)]
    public float showLoadingDelay = 2f;
    
    [Tooltip("Temps avant la fin de la vidéo end pour cacher le loading")]
    [Range(0f, 10f)]
    public float hideLoadingBeforeEnd = 2f;

    [Header("Loading Bar Animations")]
    [Tooltip("Anime l'apparition de la loading bar")]
    public bool useLoadingBarEntryAnimation = true;
    
    [Tooltip("Anime la disparition de la loading bar")]
    public bool useLoadingBarExitAnimation = true;

    [Header("Advanced")]
    [Tooltip("Utilise la dernière frame de la vidéo start comme background au lieu de la couleur")]
    public bool keepLastFrameForLoading = false;

    private void OnValidate()
    {
        if (loadingBarProfile == null)
            Debug.LogWarning("LoadingBarProfile is not assigned!", this);
        
        if (entryVideo != null && showLoadingDelay > entryVideo.length)
            Debug.LogWarning("Show loading delay is longer than entry video duration!", this);
        
        if (exitVideo != null && hideLoadingBeforeEnd > exitVideo.length)
            Debug.LogWarning("Hide loading delay is longer than exit video duration!", this);
    }
}