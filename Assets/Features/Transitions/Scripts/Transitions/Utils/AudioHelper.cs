using UnityEngine;

namespace Features.Transitions.Utils
{
    public static class AudioHelper
    {
        public static void PlayTransitionSound(AudioSource audioSource, AudioClip clip, float volume = 1f)
        {
            if (audioSource == null || clip == null) return;
            
            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
        
        public static AudioSource EnsureAudioSource(GameObject gameObject)
        {
            var audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                ConfigureAudioSource(audioSource);
            }
            return audioSource;
        }
        
        public static void ConfigureAudioSource(AudioSource audioSource)
        {
            if (audioSource == null) return;
            
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.volume = 1f;
        }
        
        public static void StopAllSounds(AudioSource audioSource)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}