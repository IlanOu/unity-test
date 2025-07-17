using UnityEngine;
using UnityEngine.UI;

public class SimpleLoadingBar : MonoBehaviour, ILoadingBar
{
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Image backgroundImage;
    
    public void Show()
    {
        loadingPanel.SetActive(true);
    }
    
    public void Hide()
    {
        loadingPanel.SetActive(false);
    }
    
    public void UpdateProgress(float progress)
    {
        if (progressSlider != null)
            progressSlider.value = progress;
    }
    
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }
    
    public void SetBackgroundColor(Color color)
    {
        if (backgroundImage != null)
            backgroundImage.color = color;
    }
}