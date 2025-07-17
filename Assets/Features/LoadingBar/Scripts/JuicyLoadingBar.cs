using System;
using DG.Tweening;
using Features.Transitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.LoadingBar
{
    public class JuicyLoadingBar : MonoBehaviour, ILoadingBar
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform containerTransform;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI percentageText;
        [SerializeField] private Image backgroundImage;
    
        private Sequence loadingSequence;
        private float currentProgress = 0f;

        /// <summary>
        /// Starts the loading animation
        /// </summary>
        public void StartLoading(float duration, LoadingAnimationProfile profile, Action onComplete)
        {
            StartLoading(duration, profile, true, true, onComplete);
        }

        /// <summary>
        /// Starts the loading animation
        /// </summary>
        public void StartLoading(float duration, LoadingAnimationProfile profile, bool useEntryAnimation, bool useExitAnimation, Action onComplete)
        {
            if (profile == null)
            {
                onComplete?.Invoke();
                return;
            }
        
            if (loadingSequence != null && loadingSequence.IsActive()) 
                loadingSequence.Kill();
        
            currentProgress = 0f;
        
            if (useEntryAnimation)
            {
                canvasGroup.alpha = 0f;
                containerTransform.localScale = Vector3.zero;
            }
            else
            {
                canvasGroup.alpha = 1f;
                containerTransform.localScale = Vector3.one;
            }
        
            loadingSequence = DOTween.Sequence();
        
            if (useEntryAnimation)
            {
                loadingSequence.Append(CreateEntryAnimation(profile));
            }
        
            loadingSequence.Append(CreateProgressAnimation(duration, profile));
        
            if (profile.useSuccessAnimation)
            {
                loadingSequence.Append(containerTransform.DOPunchScale(Vector3.one * profile.successPunchScale, profile.successPunchDuration));
            }
        
            loadingSequence.AppendInterval(profile.completeHoldDuration);
        
            if (useExitAnimation)
            {
                loadingSequence.Append(CreateExitAnimation(profile));
            }
        
            loadingSequence.OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Instantly shows the loading bar
        /// </summary>
        public void ShowInstantly()
        {
            if (loadingSequence != null && loadingSequence.IsActive()) 
                loadingSequence.Kill();
        
            canvasGroup.alpha = 1f;
            containerTransform.localScale = Vector3.one;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            currentProgress = 0f;
            UpdateVisuals(0f, null);
        }

        /// <summary>
        /// Instantly hides the loading bar
        /// </summary>
        public void HideInstantly()
        {
            if (loadingSequence != null && loadingSequence.IsActive()) 
                loadingSequence.Kill();
        
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Hides the loading bar
        /// </summary>
        public void Hide()
        {
            loadingSequence?.Kill();
            HideInstantly();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Creates the entry animation
        /// </summary>
        private Tween CreateEntryAnimation(LoadingAnimationProfile profile)
        {
            var seq = DOTween.Sequence();
            seq.Join(canvasGroup.DOFade(1f, profile.entryDuration))
                .Join(containerTransform.DOScale(1f, profile.entryDuration).SetEase(profile.entryEase));
            return seq;
        }

        /// <summary>
        /// Creates the progress animation
        /// </summary>
        private Tween CreateProgressAnimation(float duration, LoadingAnimationProfile profile)
        {
            return DOTween.To(() => currentProgress, x => currentProgress = x, 1f, duration)
                .SetEase(profile.progressEase)
                .OnUpdate(() => UpdateVisuals(currentProgress, profile));
        }
    
        /// <summary>
        /// Creates the exit animation
        /// </summary>
        private Tween CreateExitAnimation(LoadingAnimationProfile profile)
        {
            var seq = DOTween.Sequence();
            seq.Join(canvasGroup.DOFade(0f, profile.exitDuration))
                .Join(containerTransform.DOScale(0f, profile.exitDuration).SetEase(profile.exitEase));
            return seq;
        }

        /// <summary>
        /// Updates the visuals of the loading bar
        /// </summary>
        private void UpdateVisuals(float progress, LoadingAnimationProfile profile)
        {
            if (progressBar != null) progressBar.value = progress;
            if (percentageText != null) percentageText.text = $"{Mathf.CeilToInt(progress * 100)}%";
            if (fillImage != null && profile?.progressGradient != null) fillImage.color = profile.progressGradient.Evaluate(progress);
        }

        private void OnDestroy()
        {
            loadingSequence?.Kill();
        }
    }
}