using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class JuicyButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Hover Animation")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private Ease animationEase = Ease.OutBack;
    
    [Header("Click Animation")]
    [SerializeField] private float clickScale = 0.95f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private float soundVolume = 1f;
    
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Button button;
    private AudioSource audioSource;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        button = GetComponent<Button>();
        originalScale = rectTransform.localScale;
        
        // Crée un AudioSource si nécessaire
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (hoverSound != null || clickSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable)
        {
            rectTransform.DOScale(originalScale * hoverScale, animationDuration).SetEase(animationEase);
            PlaySound(hoverSound);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.DOScale(originalScale, animationDuration).SetEase(animationEase);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button.interactable)
        {
            rectTransform.DOScale(originalScale * clickScale, 0.1f);
            PlaySound(clickSound);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (button.interactable)
        {
            rectTransform.DOScale(originalScale * hoverScale, 0.1f);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    private void OnDestroy()
    {
        rectTransform.DOKill();
    }
}