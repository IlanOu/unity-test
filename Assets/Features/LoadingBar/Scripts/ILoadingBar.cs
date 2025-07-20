using System;

namespace Features.LoadingBar
{
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