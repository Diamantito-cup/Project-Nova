using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// TOKYO METROIDVANIA - OptionsMenuManager
/// Menú de opciones completo:
/// - Volumen música y efectos
/// - Resolución de pantalla
/// - Modo ventana / pantalla completa
/// - Brillo
/// - Idioma
/// - Opciones de video (calidad, vsync, fps)
/// </summary>
public class OptionsMenuManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Panel")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private CanvasGroup optionsCanvasGroup;

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeText;
    [SerializeField] private TMP_Text sfxVolumeText;

    [Header("Pantalla")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle vsyncToggle;
    [SerializeField] private TMP_Dropdown fpsDropdown;

    [Header("Brillo")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TMP_Text brightnessText;
    [SerializeField] private Image brightnessOverlay;  // panel negro con alpha para brillo

    [Header("Idioma")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    // ─────────────────────────────────────────────────────────────
    //  PRIVADO
    // ─────────────────────────────────────────────────────────────

    private Resolution[] resolutions;

    private readonly string[] languages = { "Español", "English", "日本語", "Português" };
    private readonly int[] fpsOptions   = { 30, 60, 120, 144, 240, -1 }; // -1 = sin límite

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    private void Start()
    {
        InitializeResolutions();
        InitializeQuality();
        InitializeFPS();
        InitializeLanguage();
        LoadSettings();

        // if (optionsPanel != null)
           // optionsPanel.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    //  INICIALIZACIÓN
    // ─────────────────────────────────────────────────────────────

    private void InitializeResolutions()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height} @ {resolutions[i].refreshRateRatio.numerator}Hz";
            options.Add(option);

            if (resolutions[i].width  == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                currentIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void InitializeQuality()
    {
        if (qualityDropdown == null) return;

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
    }

    private void InitializeFPS()
    {
        if (fpsDropdown == null) return;

        fpsDropdown.ClearOptions();
        fpsDropdown.AddOptions(new List<string> { "30", "60", "120", "144", "240", "Sin límite" });
        fpsDropdown.value = 1; // 60 por defecto
    }

    private void InitializeLanguage()
    {
        if (languageDropdown == null) return;

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new List<string>(languages));
        languageDropdown.value = PlayerPrefs.GetInt("Language", 0);
    }

    // ─────────────────────────────────────────────────────────────
    //  ABRIR / CERRAR
    // ─────────────────────────────────────────────────────────────

    public void OpenOptions()
{
    optionsPanel?.GetComponent<PanelTransition>()?.Open();
}

    public void CloseOptions()
    {
        SaveSettings();
        optionsPanel?.GetComponent<PanelTransition>()?.Close();
    }

    // ─────────────────────────────────────────────────────────────
    //  AUDIO
    // ─────────────────────────────────────────────────────────────

    public void OnMusicVolumeChanged(float value)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);

        if (musicVolumeText != null)
            musicVolumeText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    public void OnSFXVolumeChanged(float value)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);

        if (sfxVolumeText != null)
            sfxVolumeText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    // ─────────────────────────────────────────────────────────────
    //  PANTALLA
    // ─────────────────────────────────────────────────────────────

    public void OnResolutionChanged(int index)
    {
        if (resolutions == null || index >= resolutions.Length) return;

        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    public void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index);
    }

    public void OnVSyncChanged(bool enabled)
    {
        QualitySettings.vSyncCount = enabled ? 1 : 0;
    }

    public void OnFPSChanged(int index)
    {
        Application.targetFrameRate = fpsOptions[index];
    }

    // ─────────────────────────────────────────────────────────────
    //  BRILLO
    // ─────────────────────────────────────────────────────────────

    public void OnBrightnessChanged(float value)
    {
        if (brightnessOverlay != null)
        {
            // value 0-1: 0 = oscuro, 1 = normal
            Color c = brightnessOverlay.color;
            c.a = 1f - value;
            brightnessOverlay.color = c;
        }

        if (brightnessText != null)
            brightnessText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    // ─────────────────────────────────────────────────────────────
    //  IDIOMA
    // ─────────────────────────────────────────────────────────────

    public void OnLanguageChanged(int index)
    {
        PlayerPrefs.SetInt("Language", index);
        Debug.Log($"[Opciones] Idioma cambiado a: {languages[index]}");
        // Aquí integrar con sistema de localización
    }

    // ─────────────────────────────────────────────────────────────
    //  GUARDAR / CARGAR
    // ─────────────────────────────────────────────────────────────

    public void SaveSettings()
    {
        if (musicVolumeSlider != null)
            PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        if (sfxVolumeSlider != null)
            PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
        if (brightnessSlider != null)
            PlayerPrefs.SetFloat("Brightness", brightnessSlider.value);
        if (fullscreenToggle != null)
            PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
        if (vsyncToggle != null)
            PlayerPrefs.SetInt("VSync", vsyncToggle.isOn ? 1 : 0);
        if (languageDropdown != null)
            PlayerPrefs.SetInt("Language", languageDropdown.value);

        PlayerPrefs.Save();
        Debug.Log("[Opciones] Configuración guardada.");
    }

    private void LoadSettings()
    {
        // Audio
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float sfxVol   = PlayerPrefs.GetFloat("SFXVolume",   0.75f);
        if (musicVolumeSlider != null) { musicVolumeSlider.value = musicVol; OnMusicVolumeChanged(musicVol); }
        if (sfxVolumeSlider   != null) { sfxVolumeSlider.value   = sfxVol;   OnSFXVolumeChanged(sfxVol); }

        // Brillo
        float brightness = PlayerPrefs.GetFloat("Brightness", 1f);
        if (brightnessSlider != null) { brightnessSlider.value = brightness; OnBrightnessChanged(brightness); }

        // Pantalla
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        if (fullscreenToggle != null) { fullscreenToggle.isOn = fullscreen; Screen.fullScreen = fullscreen; }

        bool vsync = PlayerPrefs.GetInt("VSync", 0) == 1;
        if (vsyncToggle != null) { vsyncToggle.isOn = vsync; QualitySettings.vSyncCount = vsync ? 1 : 0; }
    }

    // ─────────────────────────────────────────────────────────────
    //  RESET
    // ─────────────────────────────────────────────────────────────

    public void ResetToDefaults()
    {
        if (musicVolumeSlider  != null) musicVolumeSlider.value  = 0.75f;
        if (sfxVolumeSlider    != null) sfxVolumeSlider.value    = 0.75f;
        if (brightnessSlider   != null) brightnessSlider.value   = 1f;
        if (fullscreenToggle   != null) fullscreenToggle.isOn    = true;
        if (vsyncToggle        != null) vsyncToggle.isOn         = false;
        if (qualityDropdown    != null) qualityDropdown.value    = 2;
        if (languageDropdown   != null) languageDropdown.value   = 0;

        OnMusicVolumeChanged(0.75f);
        OnSFXVolumeChanged(0.75f);
        OnBrightnessChanged(1f);
        SaveSettings();

        Debug.Log("[Opciones] Configuración reseteada.");
    }
}