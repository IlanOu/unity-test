using Features.LoadingBar;
using UnityEngine;

[CreateAssetMenu(fileName = "TransitionData", menuName = "Transitions/Transition Data")]
public class TransitionData : ScriptableObject
{
    [Header("Vidéos de transition (dans StreamingAssets/Videos/)")]
    [Tooltip("Nom du fichier vidéo d'entrée (ex: transition_01_start.webm)")]
    public string entryVideoFileName;
    [Tooltip("Nom du fichier vidéo de sortie (ex: transition_01_end.webm)")]
    public string exitVideoFileName;
    
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
    [Range(0f, 10f)]
    public float showLoadingDelay = 2f;
    [Range(0f, 10f)]
    public float hideLoadingBeforeEnd = 2f;

    [Header("Loading Bar Animations")]
    public bool useLoadingBarEntryAnimation = true;
    public bool useLoadingBarExitAnimation = true;

    [Header("Advanced")]
    [Tooltip("Utilise la dernière frame de la vidéo start comme background au lieu de la couleur")]
    public bool keepLastFrameForLoading = false;

    private void OnValidate()
    {
        if (loadingBarProfile == null)
            Debug.LogWarning("LoadingBarProfile is not assigned!", this);
    }
}