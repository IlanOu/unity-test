using System;

namespace Features.LoadingBar
{
    /// <summary>
    /// Définit le contrat pour tout composant pouvant agir comme une barre de chargement de transition.
    /// </summary>
    public interface ILoadingBar
    {
        void StartLoading(float duration, LoadingAnimationProfile profile, Action onComplete);
        void StartLoading(float duration, LoadingAnimationProfile profile, bool useEntryAnimation, bool useExitAnimation, Action onComplete);
        void StartRealProgressLoading(LoadingAnimationProfile profile, bool useEntryAnimation, bool useExitAnimation, Action onComplete);
        void UpdateRealProgress(float progress);
        void CompleteRealProgress();
        void ShowInstantly();
        void HideInstantly();
        void Hide();
    }
}