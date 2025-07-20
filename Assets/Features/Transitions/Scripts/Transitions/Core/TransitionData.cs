using UnityEngine;
using Features.LoadingBar;

namespace Features.Transitions
{
    [CreateAssetMenu(fileName = "NewTransitionData", menuName = "Transitions/Transition Data")]
    public class TransitionData : ScriptableObject
    {
        [Header("Sequence Labels (Addressables)")]
        [Tooltip("Le label Addressable pour la séquence d'entrée (ex: transition_01_start)")]
        public string entrySequenceName;
        [Tooltip("Le label Addressable pour la séquence de sortie (ex: transition_01_end)")]
        public string exitSequenceName;
        
        [Header("Timing")]
        [Range(1, 120)]
        public float frameRate = 30f;
        
        [Header("Audio")]
        public AudioClip entrySound;
        public AudioClip exitSound;
        [Range(0f, 1f)]
        public float soundVolume = 1f;
        
        [Header("Loading Screen")]
        public LoadingAnimationProfile loadingBarProfile;
        public Color backgroundColor = Color.black;
        
        [Tooltip("Délai avant l'apparition de l'écran de chargement (après la fin de la séquence d'entrée).")]
        [Range(0f, 5f)]
        public float showLoadingDelay = 0f;
        
        [Tooltip("Temps avant la fin de la séquence de sortie où l'écran de chargement disparaît.")]
        [Range(0f, 5f)]
        public float hideLoadingBeforeEnd = 0.5f;

        [Header("Animations de la Barre de Chargement")]
        public bool useLoadingBarEntryAnimation = true;
        public bool useLoadingBarExitAnimation = true;

        [Header("Fonctionnalités Avancées")]
        [Tooltip("Si coché, la dernière image de la séquence d'entrée sera utilisée comme fond d'écran pendant le chargement.")]
        public bool keepLastFrameForLoading = false;
    }
}