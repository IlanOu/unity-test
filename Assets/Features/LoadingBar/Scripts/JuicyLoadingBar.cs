using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Features.LoadingBar
{
    [RequireComponent(typeof(AudioSource))]
    public class JuicyLoadingBar : MonoBehaviour, ILoadingBar
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform containerTransform;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI percentageText;
        
        [Header("Audio")]
        [SerializeField] private AudioClip entrySound;
        [SerializeField] private AudioClip progressSound;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip exitSound;
        [Range(0f, 1f)]
        [SerializeField] private float soundVolume = 1f;
    
        private Sequence activeSequence;
        private float currentProgress = 0f;
        private AudioSource audioSource;
        private bool isTrackingRealProgress = false;
        private LoadingAnimationProfile currentProfile;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Surcharge pour un appel simple avec des animations par défaut.
        public void StartLoading(float duration, LoadingAnimationProfile profile, Action onComplete)
        {
            StartLoading(duration, profile, true, true, onComplete);
        }

        public void StartLoading(float duration, LoadingAnimationProfile profile, bool useEntryAnimation, bool useExitAnimation, Action onComplete)
        {
            StartLoadingInternal(duration, profile, useEntryAnimation, useExitAnimation, false, onComplete);
        }

        public void StartRealProgressLoading(LoadingAnimationProfile profile, bool useEntryAnimation, bool useExitAnimation, Action onComplete)
        {
            StartLoadingInternal(0f, profile, useEntryAnimation, useExitAnimation, true, onComplete);
        }
        
        public void UpdateRealProgress(float progress)
        {
            if (!isTrackingRealProgress) return;
            currentProgress = Mathf.Clamp01(progress);
            UpdateVisuals(currentProgress, currentProfile);
        }
        
        public void CompleteRealProgress()
        {
            if (!isTrackingRealProgress) return;
            
            activeSequence.Kill();
            currentProgress = 1f;
            UpdateVisuals(currentProgress, currentProfile);

            activeSequence = DOTween.Sequence();
            
            if (currentProfile.useSuccessAnimation)
            {
                activeSequence.Append(CreateSuccessAnimation(currentProfile));
            }
            activeSequence.AppendInterval(currentProfile.completeHoldDuration);
            activeSequence.Append(CreateExitAnimation(currentProfile));
            activeSequence.SetUpdate(true); // Important pour l'UI
        }
        
        public void ShowInstantly()
        {
            activeSequence.Kill();
            canvasGroup.alpha = 1f;
            containerTransform.localScale = Vector3.one;
            currentProgress = 0f;
            UpdateVisuals(0f, null);
        }

        public void HideInstantly()
        {
            activeSequence.Kill();
            canvasGroup.alpha = 0f;
        }

        public void Hide()
        {
            HideInstantly();
            gameObject.SetActive(false);
        }
        
        private void StartLoadingInternal(float duration, LoadingAnimationProfile profile, bool useEntryAnimation, bool useExitAnimation, bool realProgress, Action onComplete)
        {
            if (profile == null)
            {
                Debug.LogWarning("LoadingBar started with a null profile. Completing immediately.", this);
                onComplete?.Invoke();
                return;
            }
        
            activeSequence.Kill();
        
            currentProgress = 0f;
            isTrackingRealProgress = realProgress;
            currentProfile = profile;
            
            gameObject.SetActive(true);
            canvasGroup.alpha = useEntryAnimation ? 0f : 1f;
            containerTransform.localScale = useEntryAnimation ? Vector3.zero : Vector3.one;
            
            activeSequence = DOTween.Sequence();
            // Assure que les animations de l'UI fonctionnent même si le jeu est en pause (Time.timeScale = 0).
            activeSequence.SetUpdate(true);
        
            if (useEntryAnimation)
            {
                activeSequence.Append(CreateEntryAnimation(profile));
            }
        
            if (!realProgress)
            {
                // Mode "fake" : l'animation de progression est jouée automatiquement.
                activeSequence.Append(CreateProgressAnimation(duration, profile));
                
                if (profile.useSuccessAnimation)
                {
                    activeSequence.Append(CreateSuccessAnimation(profile));
                }
                
                activeSequence.AppendInterval(profile.completeHoldDuration);
                
                if (useExitAnimation)
                {
                    activeSequence.Append(CreateExitAnimation(profile));
                }
            }
            // Que le mode soit réel ou fake, on appelle onComplete à la fin de la séquence.
            activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        private Tween CreateEntryAnimation(LoadingAnimationProfile profile)
        {
            PlaySound(entrySound);
            return DOTween.Sequence()
                .Join(canvasGroup.DOFade(1f, profile.entryDuration))
                .Join(containerTransform.DOScale(1f, profile.entryDuration).SetEase(profile.entryEase));
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
            DOVirtual.DelayedCall(profile.successPunchDuration * 0.5f, () => PlaySound(successSound)).SetUpdate(true);
            return containerTransform.DOPunchScale(Vector3.one * profile.successPunchScale, profile.successPunchDuration);
        }
    
        private Tween CreateExitAnimation(LoadingAnimationProfile profile)
        {
            PlaySound(exitSound);
            return DOTween.Sequence()
                .Join(canvasGroup.DOFade(0f, profile.exitDuration))
                .Join(containerTransform.DOScale(0f, profile.exitDuration).SetEase(profile.exitEase));
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
        
        private void OnDestroy()
        {
            // Tue toute séquence DOTween en cours pour éviter les erreurs à la destruction de l'objet.
            activeSequence.Kill();
        }
    }
}