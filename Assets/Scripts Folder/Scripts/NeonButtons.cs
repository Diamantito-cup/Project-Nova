using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

/// <summary>
/// TOKYO METROIDVANIA - NeonButton
/// Botón con efectos hover neon cyberpunk:
/// - Brillo al pasar el mouse
/// - Escala al hacer hover
/// - Parpadeo neon al clickear
/// - Sonido de hover/click
/// Añadir a cada botón del menú junto con el componente Button normal.
/// </summary>
public class NeonButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Colores Neon")]
    [SerializeField] private Color normalColor   = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color hoverColor    = new Color(0f, 1f, 0.9f, 1f);    // cyan neon
    [SerializeField] private Color clickColor    = new Color(1f, 0f, 0.8f, 1f);    // magenta neon

    [Header("Animación")]
    [SerializeField] private float hoverScaleMultiplier = 1.08f;
    [SerializeField] private float animationSpeed = 8f;
    [SerializeField] private float flickerSpeed   = 20f;

    [Header("Sonido")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private float soundVolume = 0.5f;

    [Header("Glow")]
    [SerializeField] private Image glowImage;   // imagen de resplandor detrás del botón
    [SerializeField] private float glowMaxAlpha = 0.6f;

    // Referencias
    private TMP_Text buttonText;
    private Image buttonImage;
    private RectTransform rectTransform;
    private AudioSource audioSource;

    // Estado
    private bool isHovered = false;
    private bool isClicked = false;
    private Vector3 originalScale;
    private Color targetColor;
    private float targetGlowAlpha;

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        buttonText = GetComponentInChildren<TMP_Text>();
        buttonImage    = GetComponent<Image>();
        rectTransform  = GetComponent<RectTransform>();
        audioSource    = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        originalScale = rectTransform.localScale;
        targetColor   = normalColor;

        // Ocultar glow al inicio
        if (glowImage != null)
        {
            Color c = glowImage.color;
            c.a = 0f;
            glowImage.color = c;
        }
    }

    private void Update()
    {
        // Suavizar color del texto
        if (buttonText != null)
            buttonText.color = Color.Lerp(buttonText.color, targetColor, Time.deltaTime * animationSpeed);

        // Suavizar escala
        Vector3 targetScale = isHovered
            ? originalScale * hoverScaleMultiplier
            : originalScale;
        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale, targetScale, Time.deltaTime * animationSpeed);

        // Suavizar glow
        if (glowImage != null)
        {
            Color c = glowImage.color;
            c.a = Mathf.Lerp(c.a, targetGlowAlpha, Time.deltaTime * animationSpeed);
            glowImage.color = c;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  EVENTOS DE POINTER
    // ─────────────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetColor = hoverColor;
        targetGlowAlpha = glowMaxAlpha;

        // Sonido hover
        PlaySound(hoverSound);

        // Cambiar color del borde/imagen
        if (buttonImage != null)
            buttonImage.color = new Color(hoverColor.r, hoverColor.g, hoverColor.b, 0.15f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetColor = normalColor;
        targetGlowAlpha = 0f;

        if (buttonImage != null)
            buttonImage.color = new Color(normalColor.r, normalColor.g, normalColor.b, 0f);
    }

    public void OnPointerClick(PointerEventData eventData)
{
    PlaySound(clickSound);
    if (gameObject.activeInHierarchy)
        StartCoroutine(ClickFlicker());
}

    // ─────────────────────────────────────────────────────────────
    //  EFECTOS
    // ─────────────────────────────────────────────────────────────

    private IEnumerator ClickFlicker()
    {
        isClicked = true;

        // Parpadeo magenta neon al clickear
        for (int i = 0; i < 3; i++)
        {
            if (buttonText != null) buttonText.color = clickColor;
            yield return new WaitForSecondsRealtime(0.05f);
            if (buttonText != null) buttonText.color = hoverColor;
            yield return new WaitForSecondsRealtime(0.05f);
        }

        isClicked = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, soundVolume);
    }

    // ─────────────────────────────────────────────────────────────
    //  MÉTODO PÚBLICO para animar desde código
    // ─────────────────────────────────────────────────────────────

    public void ForceHover() => OnPointerEnter(null);
    public void ForceExit()  => OnPointerExit(null);
}