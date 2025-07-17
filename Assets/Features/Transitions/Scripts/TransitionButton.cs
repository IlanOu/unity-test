using UnityEngine;
using UnityEngine.UI;

public class TransitionButton : MonoBehaviour
{
    [SerializeField] private string targetSceneName;
    [SerializeField] private Button button;
    
    private void Start()
    {
        if (button == null)
            button = GetComponent<Button>();
        
        if (button != null)
            button.onClick.AddListener(OnButtonClick);
    }
    
    private void OnButtonClick()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(targetSceneName);
        }
        else
        {
            Debug.LogError("SceneTransitionManager non trouvé!");
        }
    }
    
    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnButtonClick);
    }
}