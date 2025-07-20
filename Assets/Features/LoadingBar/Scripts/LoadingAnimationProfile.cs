using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace Features.LoadingBar
{
    [CreateAssetMenu(fileName = "LoadingAnimationProfile", menuName = "UI/Loading Animation Profile")]
    public class LoadingAnimationProfile : ScriptableObject
    {
        [Header("Entry & Exit")]
        public float entryDuration = 0.5f;
        public Ease entryEase = Ease.OutBack;
        public float exitDuration = 0.4f;
        public Ease exitEase = Ease.InBack;
        public float completeHoldDuration = 0.2f;

        [Header("Progress Update")]
        public Ease progressEase = Ease.OutQuart;

        [Header("Visual Effects")]
        public Gradient progressGradient;
        
        [Header("Success Animation")]
        public bool useBounceAnimation = true;
        public float bounceScale = 0.2f;
        public float bounceDuration = 0.4f;
    }
}