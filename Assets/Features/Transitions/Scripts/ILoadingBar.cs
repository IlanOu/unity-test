using System;
using Features.LoadingBar;

namespace Features.Transitions
{
    public interface ILoadingBar
    {
        void StartLoading(float duration, LoadingAnimationProfile profile, Action onComplete);
        void StartLoading(float duration, LoadingAnimationProfile profile, bool useEntryAnimation, bool useExitAnimation, Action onComplete);
        void ShowInstantly();
        void HideInstantly();
        void Hide();
    }
}