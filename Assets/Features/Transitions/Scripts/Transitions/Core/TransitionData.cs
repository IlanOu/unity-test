using Features.LoadingBar;
using UnityEngine;

namespace Features.Transitions
{
    [CreateAssetMenu(fileName = "TransitionData", menuName = "Transitions/Transition Data")]
    public class TransitionData : ScriptableObject
    {
        [Header("Séquences PNG (dans StreamingAssets/Sequences/)")]
        [Tooltip("Nom du dossier de séquence d'entrée (ex: transition_01_start)")]
        public string entrySequenceName;
        [Tooltip("Nom du dossier de séquence de sortie (ex: transition_01_end)")]
        public string exitSequenceName;
        
        [Header("Timing des séquences")]
        [Range(15f, 120f)]
        public float frameRate = 30f;
        
        [Header("Audio")]
        [Tooltip("Son joué au début de la séquence start")]
        public AudioClip entrySound;
        [Tooltip("Son joué au début de la séquence end")]
        public AudioClip exitSound;
        [Range(0f, 1f)]
        public float soundVolume = 1f;
        
        [Header("Loading Bar Style")]
        public LoadingAnimationProfile loadingBarProfile;

        [Header("Background Configuration")]
        public Color backgroundColor = Color.black;
        
        [Header("Loading Screen Timing")]
        [Range(0f, 10f)]
        public float showLoadingDelay = 2f;
        [Range(0f, 10f)]
        public float hideLoadingBeforeEnd = 2f;

        [Header("Loading Bar Animations")]
        public bool useLoadingBarEntryAnimation = true;
        public bool useLoadingBarExitAnimation = true;

        [Header("Advanced")]
        [Tooltip("Utilise la dernière frame de la séquence start comme background du loading")]
        public bool keepLastFrameForLoading = false;

        private void OnValidate()
        {
            if (loadingBarProfile == null)
                Debug.LogWarning("LoadingBarProfile is not assigned!", this);
        }
    }
}