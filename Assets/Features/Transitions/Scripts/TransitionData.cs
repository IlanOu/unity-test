using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "TransitionData", menuName = "Transitions/Transition Data")]
public class TransitionData : ScriptableObject
{
    [Header("Vidéos de transition")]
    public VideoClip entryVideo;
    public VideoClip exitVideo;
    
    [Header("Configuration")]
    public bool keepLastFrameForLoading = false;
    public Color backgroundColor = Color.black;
    
    [Header("Timing")]
    [Range(0f, 5f)]
    public float minTransitionDuration = 1f;
    [Range(0f, 10f)]
    public float maxTransitionDuration = 3f;
}