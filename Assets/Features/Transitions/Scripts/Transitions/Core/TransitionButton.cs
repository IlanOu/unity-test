using UnityEngine;
using UnityEngine.UI;
using Features.Transitions.Core;

namespace Features.Transitions.UI
{
    [RequireComponent(typeof(Button))]
    public class TransitionButton : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Le nom exact de la scène à charger.")]
        [SerializeField] private string targetSceneName;
    
        [Header("Sécurité")]
        [Tooltip("Temps en secondes avant que le bouton ne soit réactivé après un clic.")]
        [SerializeField] private float buttonCooldown = 2f;
    
        private Button button;
    
        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(OnButtonClick);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    
        private void OnButtonClick()
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogError("Target scene name is not set on this button!", this);
                return;
            }

            // Désactive le bouton pour éviter les clics multiples.
            button.interactable = false; 
            Invoke(nameof(ReEnableButton), buttonCooldown);
        
            // Lance la transition via le manager global.
            TransitionManager.TransitionToScene(targetSceneName, () => {
                // Ce callback est appelé si la transition échoue, pour réactiver le bouton.
                ReEnableButton();
            });
        }
    
        private void ReEnableButton()
        {
            // Annule l'invocation au cas où le callback d'échec l'aurait déjà fait.
            CancelInvoke(nameof(ReEnableButton));
            if (this != null && button != null)
            {
                button.interactable = true;
            }
        }
    }
}