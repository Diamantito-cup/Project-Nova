using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// TOKYO METROIDVANIA - MainMenuManager
/// Menú cinemático estilo Tokyo Neon:
/// - Fondo con parallax de Tokio
/// - Título con efecto de aparición
/// - Botones: Jugar, Continuar, Opciones, Salir
/// - Transición con fade al iniciar el juego
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Escenas")]
    [SerializeField] private string gameSceneName = "SampleScene";

    [Header("UI Referencias")]
    [SerializeField] private CanvasGroup titleGroup;        // grupo del título
    [SerializeField] private CanvasGroup buttonsGroup;      // grupo de botones
    [SerializeField] private CanvasGroup fadePanel;         // panel negro para fade
    [SerializeField] private GameObject titleGroupObj;
    [SerializeField] private GameObject buttonsGroupObj;

    [Header("Opciones")]
    [SerializeField] private OptionsMenuManager optionsMenu;
    [SerializeField] private GameObject optionsPanel;

    [Header("Animación de Entrada")]
    [SerializeField] private float titleFadeInDuration  = 1.5f;
    [SerializeField] private float titleWaitDuration    = 0.5f;
    [SerializeField] private float buttonsFadeInDuration = 1.0f;

    [Header("Parallax del Fondo")]
    [SerializeField] private Transform[] parallaxLayers;    // capas del fondo
    [SerializeField] private float[] parallaxSpeeds;        // velocidad de cada capa
    [SerializeField] private float autoScrollSpeed = 0.5f;  // velocidad de scroll automático

    [Header("Título")]
    [SerializeField] private Transform titleTransform;      // transform del título para animación
    [SerializeField] private float titleBobSpeed  = 1.5f;  // velocidad del movimiento flotante
    [SerializeField] private float titleBobAmount = 10f;   // cantidad de movimiento en píxeles

    [Header("Slots")]
    [SerializeField] private GameObject saveSlotsPanel;

    // Estado
    private bool isTransitioning = false;
    private Vector3 titleOriginalPos;

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    private void Start()
    {
        // Ocultar todo al inicio
        if (titleGroup   != null) titleGroup.alpha   = 1f;
        if (buttonsGroup != null) buttonsGroup.alpha  = 1f;
        if (fadePanel    != null) fadePanel.alpha     = 1f;

        if (titleTransform != null)
            titleOriginalPos = titleTransform.localPosition;

        StartCoroutine(IntroSequence());
    }

    private void Update()
    {
        HandleParallaxScroll();
        HandleTitleBob();

        // Presionar cualquier tecla para saltar la intro
        if (UnityEngine.InputSystem.Keyboard.current != null && 
    UnityEngine.InputSystem.Keyboard.current.anyKey.wasPressedThisFrame && 
    !isTransitioning)
            SkipIntro();
    }

    // ─────────────────────────────────────────────────────────────
    //  SECUENCIA DE INTRO
    // ─────────────────────────────────────────────────────────────

    private IEnumerator IntroSequence()
    {
        // Fade out del panel negro
        // yield return StartCoroutine(FadeCanvasGroup(fadePanel, 1f, 0f, 1.0f));

        // Fade in del título
        // yield return StartCoroutine(FadeCanvasGroup(titleGroup, 0f, 1f, titleFadeInDuration));

        // yield return new WaitForSeconds(titleWaitDuration);

        // Fade in de los botones
        //yield return StartCoroutine(FadeCanvasGroup(buttonsGroup, 0f, 1f, buttonsFadeInDuration));
        yield break;
    }

    private void SkipIntro()
    {
        StopAllCoroutines();
        if (titleGroup   != null) titleGroup.alpha   = 1f;
        if (buttonsGroup != null) buttonsGroup.alpha  = 1f;
        if (fadePanel    != null) fadePanel.alpha     = 0f;
    }

    // ─────────────────────────────────────────────────────────────
    //  PARALLAX AUTO-SCROLL
    // ─────────────────────────────────────────────────────────────

    private void HandleParallaxScroll()
    {
        if (parallaxLayers == null) return;

        for (int i = 0; i < parallaxLayers.Length; i++)
        {
            if (parallaxLayers[i] == null) continue;

            float speed = (parallaxSpeeds != null && i < parallaxSpeeds.Length)
                ? parallaxSpeeds[i]
                : autoScrollSpeed * (i + 1) * 0.3f;

            parallaxLayers[i].position += Vector3.left * speed * Time.deltaTime;

            // Loop infinito
            if (parallaxLayers[i].position.x < -20f)
            {
                Vector3 pos = parallaxLayers[i].position;
                pos.x += 40f;
                parallaxLayers[i].position = pos;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  TÍTULO FLOTANTE
    // ─────────────────────────────────────────────────────────────

    private void HandleTitleBob()
    {
        if (titleTransform == null) return;

        float newY = titleOriginalPos.y +
            Mathf.Sin(Time.time * titleBobSpeed) * titleBobAmount;

        titleTransform.localPosition = new Vector3(
            titleOriginalPos.x, newY, titleOriginalPos.z);
    }

    // ─────────────────────────────────────────────────────────────
    //  BOTONES
    // ─────────────────────────────────────────────────────────────

    public void OnPlayButton()
{

    Debug.Log("OnSaveSlotsClose llamado!");
    if (isTransitioning) return;
    if (titleGroupObj != null) titleGroupObj.SetActive(false);
    if (buttonsGroupObj != null) buttonsGroupObj.SetActive(false);
    saveSlotsPanel?.GetComponent<PanelTransition>()?.Open();
    Debug.Log("Abriendo slots desde MainMenuManager");
}

public void OnSaveSlotsClose()
{
    if (titleGroupObj != null) titleGroupObj.SetActive(true);
    if (buttonsGroupObj != null) buttonsGroupObj.SetActive(true);
    saveSlotsPanel?.GetComponent<PanelTransition>()?.Close();
}
    public void OnContinueButton()
    {
        if (isTransitioning) return;
        if (SaveSystem.Instance != null && SaveSystem.Instance.HasSaveData())
            StartCoroutine(LoadGameScene(true));
        else
            Debug.Log("[Menu] No hay datos guardados.");
    }

    public void OnOptionsButton()
{
    Debug.Log("OnOptionsButton llamado!");
    if (isTransitioning) return;
    if (titleGroupObj != null) titleGroupObj.SetActive(false);
    if (buttonsGroupObj != null) buttonsGroupObj.SetActive(false);
    optionsMenu?.OpenOptions();
}

    public void OnQuitButton()
    {
        if (isTransitioning) return;
        StartCoroutine(QuitRoutine());
    }

    // ─────────────────────────────────────────────────────────────
    //  TRANSICIÓN AL JUEGO
    // ─────────────────────────────────────────────────────────────

    private IEnumerator LoadGameScene(bool loadSave)
    {
        isTransitioning = true;

        // Fade in del panel negro
        yield return StartCoroutine(FadeCanvasGroup(fadePanel, 0f, 1f, 0.8f));

        // Cargar escena del juego
        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator QuitRoutine()
    {
        isTransitioning = true;
        yield return StartCoroutine(FadeCanvasGroup(fadePanel, 0f, 1f, 0.5f));
        Application.Quit();
        Debug.Log("[Menu] Saliendo del juego...");
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPER - FADE CANVAS GROUP
    // ─────────────────────────────────────────────────────────────

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        float elapsed = 0f;
        group.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha = to;
    }
}