using UnityEngine;
using UnityEngine.UI;

namespace Features.Transitions
{
    public class TransitionButton : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string targetSceneName;
    
        [Header("Settings")]
        [SerializeField] private float buttonCooldown = 2f;
    
        private Button button;
    
        private void Awake()
        {
            button = GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError("TransitionButton requires a Button component!", this);
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogWarning("Target scene name is not set!", this);
            }
        }

        private void OnEnable()
        {
            button?.onClick.AddListener(OnButtonClick);
        }

        private void OnDisable()
        {
            button?.onClick.RemoveListener(OnButtonClick);
        }
    
        private void OnButtonClick()
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError("Target scene name is not set!", this);
                return;
            }

            if (button != null) // Désactive le bouton pour éviter le spam
            {
                button.interactable = false; 
                Invoke(nameof(ReEnableButton), buttonCooldown);
            }
        
            TransitionManager.TransitionToScene(targetSceneName);
        }
    
        private void ReEnableButton()
        {
            if (button != null)
                button.interactable = true;
        }
    }
}