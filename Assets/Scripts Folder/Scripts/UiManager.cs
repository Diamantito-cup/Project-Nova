using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// TOKYO METROIDVANIA - UIManager
/// HUD: corazones de vida, barra de alma (soul), fade in/out.
/// Estética neon cyberpunk-Tokio.
/// </summary>
public class UIManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  SINGLETON
    // ─────────────────────────────────────────────────────────────

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Vida (Health)")]
    [SerializeField] private Transform heartsContainer;
    [SerializeField] private GameObject heartFullPrefab;
    [SerializeField] private GameObject heartEmptyPrefab;
    [SerializeField] private Color heartNeonColor = new Color(0.2f, 1f, 0.8f); // cyan neon

    [Header("Alma (Soul)")]
    [SerializeField] private Image soulBarFill;
    [SerializeField] private float soulBarSmoothSpeed = 5f;
    private float targetSoulFill;

    [Header("Fade")]
    [SerializeField] private Image fadePanel;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Boss Health Bar")]
    [SerializeField] private GameObject bossHudRoot;
    [SerializeField] private Image bossHealthFill;
    [SerializeField] private Text bossNameText;

    [Header("Notificaciones")]
    [SerializeField] private Text notificationText;
    [SerializeField] private float notificationDuration = 2.5f;

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────────────────────

    private List<Image> heartImages = new List<Image>();
    private int maxHealth;
    private Coroutine notificationRoutine;

    // ─────────────────────────────────────────────────────────────
    //  INICIALIZACIÓN
    // ─────────────────────────────────────────────────────────────

    public void Initialize(int max, int current, int soul, int maxSoul)
    {
        maxHealth = max;
        BuildHearts(max);
        UpdateHealth(current, max);
        UpdateSoul(soul, maxSoul);

        if (bossHudRoot != null) bossHudRoot.SetActive(false);
        if (fadePanel != null) SetFadeAlpha(0f);
    }

    // ─────────────────────────────────────────────────────────────
    //  CORAZONES
    // ─────────────────────────────────────────────────────────────

    private void BuildHearts(int max)
    {
        if (heartsContainer == null) return;

        // Limpiar corazones existentes
        foreach (Transform child in heartsContainer)
            Destroy(child.gameObject);
        heartImages.Clear();

        for (int i = 0; i < max; i++)
        {
            GameObject h = Instantiate(heartFullPrefab, heartsContainer);
            Image img = h.GetComponent<Image>();
            if (img != null)
            {
                img.color = heartNeonColor;
                heartImages.Add(img);
            }
        }
    }

    public void UpdateHealth(int current, int max)
    {
        if (heartImages.Count != max)
        {
            BuildHearts(max);
        }

        for (int i = 0; i < heartImages.Count; i++)
        {
            // Corazón lleno = neon cyan, vacío = gris oscuro
            heartImages[i].color = i < current
                ? heartNeonColor
                : new Color(0.15f, 0.15f, 0.15f, 0.8f);

            // Escalar el corazón al perder vida (pequeño pulso)
            if (i == current - 1)
                StartCoroutine(PulseHeart(heartImages[i]));
        }
    }

    private IEnumerator PulseHeart(Image heart)
    {
        float elapsed = 0f;
        float duration = 0.2f;
        Vector3 originalScale = heart.transform.localScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
            heart.transform.localScale = originalScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }
        heart.transform.localScale = originalScale;
    }

    // ─────────────────────────────────────────────────────────────
    //  BARRA DE ALMA
    // ─────────────────────────────────────────────────────────────

    public void UpdateSoul(int current, int max)
    {
        if (max <= 0) return;
        targetSoulFill = (float)current / max;
    }

    private void Update()
    {
        // Suavizar la barra de alma
        if (soulBarFill != null)
        {
            soulBarFill.fillAmount = Mathf.Lerp(
                soulBarFill.fillAmount,
                targetSoulFill,
                Time.deltaTime * soulBarSmoothSpeed);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  BOSS HUD
    // ─────────────────────────────────────────────────────────────

    public void ShowBossHUD(string bossName)
    {
        if (bossHudRoot == null) return;
        bossHudRoot.SetActive(true);
        if (bossNameText != null) bossNameText.text = bossName;
        if (bossHealthFill != null) bossHealthFill.fillAmount = 1f;
    }

    public void UpdateBossHealth(float normalizedHealth)
    {
        if (bossHealthFill != null)
            bossHealthFill.fillAmount = Mathf.Clamp01(normalizedHealth);
    }

    public void HideBossHUD()
    {
        if (bossHudRoot != null) bossHudRoot.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    //  FADE
    // ─────────────────────────────────────────────────────────────

    public void FadeIn()  => StartCoroutine(FadeRoutine(0f, 1f));
    public void FadeOut() => StartCoroutine(FadeRoutine(1f, 0f));

    private IEnumerator FadeRoutine(float from, float to)
    {
        if (fadePanel == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(from, to, elapsed / fadeDuration));
            yield return null;
        }
        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float a)
    {
        if (fadePanel == null) return;
        Color c = fadePanel.color;
        c.a = a;
        fadePanel.color = c;
    }

    // ─────────────────────────────────────────────────────────────
    //  NOTIFICACIONES (ej: "ZONA NUEVA DESCUBIERTA")
    // ─────────────────────────────────────────────────────────────

    public void ShowNotification(string message)
    {
        if (notificationRoutine != null) StopCoroutine(notificationRoutine);
        notificationRoutine = StartCoroutine(NotificationRoutine(message));
    }

    private IEnumerator NotificationRoutine(string message)
    {
        if (notificationText == null) yield break;

        notificationText.text = message;
        notificationText.gameObject.SetActive(true);

        // Fade in
        Color c = notificationText.color;
        c.a = 0f;
        notificationText.color = c;

        float fadeTime = 0.4f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / fadeTime);
            notificationText.color = c;
            yield return null;
        }

        yield return new WaitForSeconds(notificationDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            notificationText.color = c;
            yield return null;
        }

        notificationText.gameObject.SetActive(false);
    }
}