using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace Features.Transitions
{
    public class SequencePlayer : MonoBehaviour
    {
        public IEnumerator PlaySequence(string sequenceName, float frameRate, Image displayImage, Action onLastFrame = null)
        {
            Debug.Log($"=== SequencePlayer.PlaySequence START ===");
            Debug.Log($"Sequence: {sequenceName}, FrameRate: {frameRate}");
    
            if (string.IsNullOrEmpty(sequenceName))
            {
                Debug.LogWarning("Sequence name is empty, skipping playback");
                yield break;
            }

            if (displayImage == null)
            {
                Debug.LogError("Display image is null!");
                yield break;
            }

            Debug.Log($"Display image active: {displayImage.gameObject.activeInHierarchy}");

            // Vérifier le cache
            if (!SequenceCache.Instance.IsSequenceCached(sequenceName))
            {
                Debug.LogError($"Sequence {sequenceName} not cached!");
                yield break;
            }

            var cachedSequence = SequenceCache.Instance.GetSequence(sequenceName);
            if (cachedSequence?.sprites == null || cachedSequence.sprites.Length == 0)
            {
                Debug.LogError($"Failed to get cached sequence: {sequenceName}");
                yield break;
            }

            Debug.Log($"Playing cached sequence with {cachedSequence.sprites.Length} frames");

            yield return PlayCachedSequence(cachedSequence, frameRate, displayImage, onLastFrame);
    
            Debug.Log("=== SequencePlayer.PlaySequence END ===");
        }

        private IEnumerator PlayCachedSequence(CachedSequence cachedSequence, float frameRate, Image displayImage, Action onLastFrame)
        {
            Debug.Log("=== PlayCachedSequence START ===");
    
            float frameDuration = 1f / frameRate;
            Debug.Log($"Frame duration: {frameDuration}s");
    
            displayImage.gameObject.SetActive(true);
            displayImage.transform.SetAsLastSibling();
            displayImage.color = Color.white;

            Debug.Log($"Starting sequence playback: {cachedSequence.sprites.Length} frames");

            for (int i = 0; i < cachedSequence.sprites.Length; i++)
            {
                Debug.Log($"Processing frame {i}");
        
                if (cachedSequence.sprites[i] != null)
                {
                    displayImage.sprite = cachedSequence.sprites[i];
                    Debug.Log($"Set sprite for frame {i}");

                    if (i == cachedSequence.sprites.Length - 1 && onLastFrame != null)
                    {
                        Debug.Log("Last frame with callback, invoking and breaking");
                        onLastFrame.Invoke();
                        yield break;
                    }

                    Debug.Log($"Waiting {frameDuration}s for next frame");
                    yield return new WaitForSeconds(frameDuration);
                }
                else
                {
                    Debug.LogWarning($"Frame {i} is null!");
                }
            }

            Debug.Log("Sequence completed normally");
            if (onLastFrame == null)
            {
                displayImage.gameObject.SetActive(false);
                Debug.Log("Display image deactivated");
            }
    
            Debug.Log("=== PlayCachedSequence END ===");
        }
    }
}