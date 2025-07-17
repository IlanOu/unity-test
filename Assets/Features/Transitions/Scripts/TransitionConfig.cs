using UnityEngine;

[CreateAssetMenu(fileName = "TransitionConfig", menuName = "Transitions/Transition Config")]
public class TransitionConfig : ScriptableObject
{
    [Header("Transitions disponibles")]
    public TransitionData[] availableTransitions;
    
    [Header("Probabilités")]
    [Range(0f, 1f)]
    public float longLoadingChance = 0.33f; // 1 chance sur 3
    public float longLoadingDuration = 3f;
    
    public TransitionData GetRandomTransition()
    {
        if (availableTransitions == null || availableTransitions.Length == 0)
            return null;
            
        int randomIndex = Random.Range(0, availableTransitions.Length);
        return availableTransitions[randomIndex];
    }
    
    public bool ShouldUseLongLoading()
    {
        return Random.value < longLoadingChance;
    }
}