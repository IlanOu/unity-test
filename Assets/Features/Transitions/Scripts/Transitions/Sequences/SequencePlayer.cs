using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using Features.Transitions.Configuration;

namespace Features.Transitions.Sequences
{
    public class SequencePlayer : MonoBehaviour
    {
        private TransitionSettings settings;

        private void Awake()
        {
            settings = new TransitionSettings();
        }

        public IEnumerator PlaySequence(string sequenceName, float frameRate, Image displayImage,
            Action onLastFrame = null)
        {
            settings.LogDebug($"=== SequencePlayer.PlaySequence START ===");
            settings.LogDebug($"Sequence: {sequenceName}, FrameRate: {frameRate}");

            if (string.IsNullOrEmpty(sequenceName))
            {
                settings.LogDebug("Sequence name is empty, skipping playback");
                yield break;
            }

            if (displayImage == null)
            {
                Debug.LogError("Display image is null!");
                yield break;
            }

            settings.LogDebug($"Display image active: {displayImage.gameObject.activeInHierarchy}");

            if (!SequenceCache.Instance.IsSequenceCached(sequenceName))
            {
                Debug.LogError($"Sequence {sequenceName} not cached!");
                yield break;
            }

            var cachedSequence = SequenceCache.Instance.GetSequence(sequenceName);

            if (cachedSequence?.Sprites == null || cachedSequence.Sprites.Length == 0)
            {
                Debug.LogError($"Failed to get cached sequence: {sequenceName}");
                yield break;
            }

            settings.LogDebug($"Playing cached sequence with {cachedSequence.Sprites.Length} frames");

            yield return PlayCachedSequence(cachedSequence, frameRate, displayImage, onLastFrame);

            settings.LogDebug("=== SequencePlayer.PlaySequence END ===");
        }

        private IEnumerator PlayCachedSequence(CachedSequence cachedSequence, float frameRate, Image displayImage,
            Action onLastFrame)
        {
            settings.LogDebug("=== PlayCachedSequence START ===");

            float frameDuration = 1f / frameRate;
            settings.LogDebug($"Frame duration: {frameDuration}s");

            displayImage.gameObject.SetActive(true);
            displayImage.color = Color.white;

            settings.LogDebug($"Starting sequence playback: {cachedSequence.Sprites.Length} frames");

            for (int i = 0; i < cachedSequence.Sprites.Length; i++)
            {
                settings.LogDebug($"Processing frame {i}");

                if (cachedSequence.Sprites[i] != null)
                {
                    displayImage.sprite = cachedSequence.Sprites[i];
                    settings.LogDebug($"Set sprite for frame {i}");

                    if (i == cachedSequence.Sprites.Length - 1 && onLastFrame != null)
                    {
                        settings.LogDebug("Last frame with callback, invoking and breaking");
                        onLastFrame.Invoke();
                        displayImage.gameObject.SetActive(false);
                        yield break;
                    }

                    settings.LogDebug($"Waiting {frameDuration}s for next frame");
                    yield return new WaitForSeconds(frameDuration);
                }
                else
                {
                    Debug.LogWarning($"Frame {i} is null!");
                }
            }

            settings.LogDebug("Sequence completed normally");
            if (onLastFrame == null)
            {
                displayImage.gameObject.SetActive(false);
                settings.LogDebug("Display image deactivated");
            }

            settings.LogDebug("=== PlayCachedSequence END ===");
        }
    }
}