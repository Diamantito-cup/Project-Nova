using UnityEngine;
using System.Collections;

/// <summary>
/// TOKYO METROIDVANIA - PanelTransition
/// Transiciones suaves estilo Hyprland:
/// - Slide desde abajo/arriba/izquierda/derecha
/// - Fade + Scale (zoom in/out)
/// - Combinación de ambos
/// Añadir a cada panel que quieras animar.
/// </summary>
public class PanelTransition : MonoBehaviour
{
    public enum TransitionType
    {
        SlideFromBottom,
        SlideFromTop,
        SlideFromLeft,
        SlideFromRight,
        FadeScale,
        FadeOnly
    }

    [Header("Configuración")]
    [SerializeField] private TransitionType openTransition  = TransitionType.SlideFromBottom;
    [SerializeField] private TransitionType closeTransition = TransitionType.SlideFromBottom;
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Slide")]
    [SerializeField] private float slideDistance = 80f;

    [Header("Scale")]
    [SerializeField] private float startScale = 0.85f;

    // Referencias
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Coroutine currentCoroutine;

    private void Awake()
    {
        canvasGroup   = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        originalPosition = rectTransform.anchoredPosition;
    }

    // ─────────────────────────────────────────────────────────────
    //  ABRIR
    // ─────────────────────────────────────────────────────────────

    public void Open()
    {
        gameObject.SetActive(true);
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(AnimateOpen());
    }

    private IEnumerator AnimateOpen()
    {
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;

        Vector2 startPos = GetOffsetPosition(openTransition);
        rectTransform.anchoredPosition = startPos;

        float startScaleVal = openTransition == TransitionType.FadeScale ? startScale : 1f;
        transform.localScale = Vector3.one * startScaleVal;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));

            canvasGroup.alpha = t;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, originalPosition, t);

            if (openTransition == TransitionType.FadeScale)
                transform.localScale = Vector3.one * Mathf.Lerp(startScaleVal, 1f, t);

            yield return null;
        }

        canvasGroup.alpha              = 1f;
        rectTransform.anchoredPosition = originalPosition;
        transform.localScale           = Vector3.one;
        canvasGroup.interactable       = true;
        canvasGroup.blocksRaycasts     = true;
    }

    // ─────────────────────────────────────────────────────────────
    //  CERRAR
    // ─────────────────────────────────────────────────────────────

    public void Close()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(AnimateClose());
    }

    private IEnumerator AnimateClose()
    {
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;

        Vector2 endPos      = GetOffsetPosition(closeTransition);
        float endScaleVal   = closeTransition == TransitionType.FadeScale ? startScale : 1f;
        Vector2 startPos    = rectTransform.anchoredPosition;
        float startScaleVal = transform.localScale.x;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));

            canvasGroup.alpha = 1f - t;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            if (closeTransition == TransitionType.FadeScale)
                transform.localScale = Vector3.one * Mathf.Lerp(startScaleVal, endScaleVal, t);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        rectTransform.anchoredPosition = originalPosition;
        transform.localScale           = Vector3.one;
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPER
    // ─────────────────────────────────────────────────────────────

    private Vector2 GetOffsetPosition(TransitionType type)
    {
        return type switch
        {
            TransitionType.SlideFromBottom => originalPosition + Vector2.down  * slideDistance,
            TransitionType.SlideFromTop    => originalPosition + Vector2.up    * slideDistance,
            TransitionType.SlideFromLeft   => originalPosition + Vector2.left  * slideDistance,
            TransitionType.SlideFromRight  => originalPosition + Vector2.right * slideDistance,
            _                              => originalPosition
        };
    }
}