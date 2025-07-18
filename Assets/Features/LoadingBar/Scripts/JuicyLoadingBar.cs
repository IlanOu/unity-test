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
        
        [Header("Audio")]
        [SerializeField] private AudioClip entrySound;
        [SerializeField] private AudioClip progressSound;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip exitSound;
        [SerializeField] private float soundVolume = 1f;
    
        private Sequence loadingSequence;
        private float currentProgress = 0f;
        private AudioSource audioSource;
        private bool isRealProgress = false;
        private LoadingAnimationProfile currentProfile;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && HasAnySounds())
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        /// <summary>
        /// Starts the loading animation with fake timing
        /// </summary>
        public void StartLoading(float duration, LoadingAnimationProfile profile, Action onComplete)
        {
            StartLoading(duration, profile, true, true, onComplete);
        }

        /// <summary>
        /// Starts the loading animation with fake timing
        /// </summary>
        public void StartLoading(float duration, LoadingAnimationProfile profile, bool useEntryAnimation, bool useExitAnimation, Action onComplete)
        {
            StartLoadingInternal(duration, profile, useEntryAnimation, useExitAnimation, false, onComplete);
        }

        /// <summary>
        /// Starts the loading animation with real progress tracking
        /// </summary>
        public void StartRealProgressLoading(LoadingAnimationProfile profile, bool useEntryAnimation, bool useExitAnimation, Action onComplete)
        {
            StartLoadingInternal(0f, profile, useEntryAnimation, useExitAnimation, true, onComplete);
        }

        /// <summary>
        /// Updates the real progress (0 to 1)
        /// </summary>
        public void UpdateRealProgress(float progress)
        {
            if (!isRealProgress) return;
            
            progress = Mathf.Clamp01(progress);
            currentProgress = progress;
            UpdateVisuals(currentProgress, currentProfile);
        }

        /// <summary>
        /// Completes the real progress loading
        /// </summary>
        public void CompleteRealProgress()
        {
            if (!isRealProgress) return;
            
            currentProgress = 1f;
            UpdateVisuals(currentProgress, currentProfile);
            
            // Jouer l'animation de succès et de sortie
            if (loadingSequence != null && loadingSequence.IsActive()) 
                loadingSequence.Kill();
                
            loadingSequence = DOTween.Sequence();
            
            if (currentProfile.useSuccessAnimation)
            {
                loadingSequence.Append(CreateSuccessAnimation(currentProfile));
            }
            
            loadingSequence.AppendInterval(currentProfile.completeHoldDuration);
            loadingSequence.Append(CreateExitAnimation(currentProfile));
        }

        private void StartLoadingInternal(float duration, LoadingAnimationProfile profile, bool useEntryAnimation, bool useExitAnimation, bool realProgress, Action onComplete)
        {
            if (profile == null)
            {
                onComplete?.Invoke();
                return;
            }
        
            if (loadingSequence != null && loadingSequence.IsActive()) 
                loadingSequence.Kill();
        
            currentProgress = 0f;
            isRealProgress = realProgress;
            currentProfile = profile;
        
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
        
            if (!realProgress)
            {
                // Mode fake : animation automatique
                loadingSequence.Append(CreateProgressAnimation(duration, profile));
                
                if (profile.useSuccessAnimation)
                {
                    loadingSequence.Append(CreateSuccessAnimation(profile));
                }
                
                loadingSequence.AppendInterval(profile.completeHoldDuration);
                
                if (useExitAnimation)
                {
                    loadingSequence.Append(CreateExitAnimation(profile));
                }
                
                loadingSequence.OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                // Mode réel : on attend les updates manuelles
                UpdateVisuals(0f, profile);
                loadingSequence.OnComplete(() => onComplete?.Invoke());
            }
        }

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

        public void HideInstantly()
        {
            if (loadingSequence != null && loadingSequence.IsActive()) 
                loadingSequence.Kill();
        
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void Hide()
        {
            loadingSequence?.Kill();
            HideInstantly();
            gameObject.SetActive(false);
        }

        private Tween CreateEntryAnimation(LoadingAnimationProfile profile)
        {
            PlaySound(entrySound);
            
            var seq = DOTween.Sequence();
            seq.Join(canvasGroup.DOFade(1f, profile.entryDuration))
                .Join(containerTransform.DOScale(1f, profile.entryDuration).SetEase(profile.entryEase));
            return seq;
        }

        private Tween CreateProgressAnimation(float duration, LoadingAnimationProfile profile)
        {
            PlaySound(progressSound);
            
            return DOTween.To(() => currentProgress, x => currentProgress = x, 1f, duration)
                .SetEase(profile.progressEase)
                .OnUpdate(() => UpdateVisuals(currentProgress, profile));
        }
        
        private Tween CreateSuccessAnimation(LoadingAnimationProfile profile)
        {
            float soundDelay = profile.successPunchDuration * 0.5f;
            DOVirtual.DelayedCall(soundDelay, () => PlaySound(successSound));
    
            return containerTransform.DOPunchScale(Vector3.one * profile.successPunchScale, profile.successPunchDuration);
        }
    
        private Tween CreateExitAnimation(LoadingAnimationProfile profile)
        {
            PlaySound(exitSound);
            
            var seq = DOTween.Sequence();
            seq.Join(canvasGroup.DOFade(0f, profile.exitDuration))
                .Join(containerTransform.DOScale(0f, profile.exitDuration).SetEase(profile.exitEase));
            return seq;
        }

        private void UpdateVisuals(float progress, LoadingAnimationProfile profile)
        {
            if (progressBar != null) progressBar.value = progress;
            if (percentageText != null) percentageText.text = $"{Mathf.CeilToInt(progress * 100)}%";
            if (fillImage != null && profile?.progressGradient != null) fillImage.color = profile.progressGradient.Evaluate(progress);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, soundVolume);
            }
        }

        private bool HasAnySounds()
        {
            return entrySound != null || progressSound != null || successSound != null || exitSound != null;
        }

        private void OnDestroy()
        {
            loadingSequence?.Kill();
        }
    }
}