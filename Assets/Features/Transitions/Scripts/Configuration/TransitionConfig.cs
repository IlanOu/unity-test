using UnityEngine;

namespace Features.Transitions.Configuration
{
    [CreateAssetMenu(fileName = "TransitionConfig", menuName = "Transitions/Transition Config")]
    public class TransitionConfig : ScriptableObject
    {
        private static TransitionConfig _instance;
        public static TransitionConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<TransitionConfig>("TransitionConfig");
                    if (_instance == null)
                    {
                        Debug.LogError("TransitionConfig not found in Resources folder!");
                    }
                }
                return _instance;
            }
        }

        [Header("Transitions disponibles")]
        public TransitionData[] availableTransitions;
    
        [Header("Probabilités")]
        [Range(0f, 1f)]
        public float longLoadingChance = 0.33f;
        public float longLoadingDuration = 3f;
        
        public TransitionData GetRandomTransition()
        {
            if (availableTransitions == null || availableTransitions.Length == 0)
            {
                Debug.LogError("No transitions available in TransitionConfig!");
                return null;
            }
            
            int randomIndex = Random.Range(0, availableTransitions.Length);
            return availableTransitions[randomIndex];
        }
    
        public bool ShouldUseLongLoading()
        {
            return Random.value < longLoadingChance;
        }

        private void OnValidate()
        {
            if (availableTransitions == null || availableTransitions.Length == 0)
            {
                Debug.LogWarning("No transitions configured!", this);
            }
        }
    }
}