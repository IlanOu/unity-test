using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Features.Transitions.Sequences
{
    public class SequencePlayer : MonoBehaviour
    {
        public IEnumerator PlaySequence(string sequenceLabel, float frameRate, Image displayImage, Action onLastFrame = null)
        {
            if (displayImage == null)
            {
                Debug.LogError("[SequencePlayer] Display image is null. Aborting playback.", this);
                yield break;
            }

            // Récupère la séquence pré-chargée depuis le fournisseur.
            var sequenceSprites = SequenceProvider.Instance.GetSequence(sequenceLabel);

            if (sequenceSprites == null || sequenceSprites.Length == 0)
            {
                Debug.LogError($"[SequencePlayer] Sequence '{sequenceLabel}' is not preloaded or is empty. Aborting playback.", this);
                displayImage.gameObject.SetActive(false);
                yield break;
            }

            yield return PlayFrames(sequenceSprites, frameRate, displayImage, onLastFrame);
        }

        private IEnumerator PlayFrames(Sprite[] frames, float frameRate, Image displayImage, Action onLastFrame)
        {
            float frameDuration = (frameRate > 0) ? 1f / frameRate : 0f;
            displayImage.gameObject.SetActive(true);
            displayImage.color = Color.white;

            for (int i = 0; i < frames.Length; i++)
            {
                Sprite currentFrame = frames[i];
                if (currentFrame != null)
                {
                    displayImage.sprite = currentFrame;
                }
                
                // Si c'est la dernière frame et qu'un callback est fourni, on l'exécute et on arrête la coroutine.
                if (i == frames.Length - 1 && onLastFrame != null)
                {
                    onLastFrame.Invoke();
                    yield break;
                }

                if (frameDuration > 0)
                {
                    yield return new WaitForSeconds(frameDuration);
                }
                else // Évite une boucle infinie si le framerate est 0.
                {
                    yield return null;
                }
            }
            
            // Ce code n'est atteint que si la boucle se termine sans callback, on cache simplement l'image.
            displayImage.gameObject.SetActive(false);
        }
    }
}