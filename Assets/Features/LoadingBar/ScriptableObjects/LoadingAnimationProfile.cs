using DG.Tweening;
using UnityEngine;

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
        public bool useSuccessAnimation = true;
        public float successPunchScale = 0.2f;
        public float successPunchDuration = 0.4f;
    }
}