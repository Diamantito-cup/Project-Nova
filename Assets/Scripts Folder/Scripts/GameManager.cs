using UnityEngine;

/// <summary>
/// TOKYO METROIDVANIA - GameManager
/// Punto de entrada principal. Conecta todos los sistemas al inicio.
/// Colócalo en un GameObject vacío en la escena principal.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  SINGLETON
    // ─────────────────────────────────────────────────────────────

    public static GameManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Referencias de Escena")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private RoomManager roomManager;

    [Header("Configuración")]
    [SerializeField] private bool loadSaveOnStart = true;
    [SerializeField] private string startZone = "shibuya";

    // ─────────────────────────────────────────────────────────────
    //  ESTADO
    // ─────────────────────────────────────────────────────────────

    public enum GameState { Playing, Paused, Dead, Cutscene }
    private GameState currentState = GameState.Playing;

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }

    private void Start()
    {
        InitializeSystems();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    // ─────────────────────────────────────────────────────────────
    //  INICIALIZACIÓN
    // ─────────────────────────────────────────────────────────────

    private void InitializeSystems()
    {
        // Auto-resolver referencias si no están asignadas
        if (playerHealth    == null) playerHealth    = FindObjectOfType<PlayerHealth>();
        if (playerController == null) playerController = FindObjectOfType<PlayerController>();
        if (uiManager       == null) uiManager       = FindObjectOfType<UIManager>();
        if (cameraController == null) cameraController = FindObjectOfType<CameraController>();
        if (roomManager     == null) roomManager     = FindObjectOfType<RoomManager>();

        // Conectar eventos del jugador a la UI
        if (playerHealth != null && uiManager != null)
        {
            playerHealth.OnHealthChanged.AddListener(uiManager.UpdateHealth);
            playerHealth.OnSoulChanged.AddListener(soul =>
                uiManager.UpdateSoul(soul, 99));
            playerHealth.OnPlayerDeath.AddListener(OnPlayerDied);

            uiManager.Initialize(
                playerHealth.MaxHealth,
                playerHealth.CurrentHealth,
                playerHealth.CurrentSoul, 99);
        }

        // Cargar save o empezar desde zona inicial
        if (loadSaveOnStart && SaveSystem.Instance != null && SaveSystem.Instance.HasSaveData())
        {
            Vector3 savedPos = SaveSystem.Instance.LoadPlayerPosition();
            string savedZone = SaveSystem.Instance.LoadLastZone();
            if (playerController != null)
                playerController.transform.position = savedPos;
            roomManager?.EnterZone(savedZone);
        }
        else
        {
            roomManager?.EnterZone(startZone);
        }

        Debug.Log("[GameManager] Todos los sistemas inicializados. ¡Bienvenido a Tokio!");
    }

    // ─────────────────────────────────────────────────────────────
    //  ESTADO DEL JUEGO
    // ─────────────────────────────────────────────────────────────

    public void TogglePause()
    {
        if (currentState == GameState.Playing)
        {
            currentState = GameState.Paused;
            Time.timeScale = 0f;
            // UIManager.Instance?.ShowPauseMenu();
        }
        else if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            Time.timeScale = 1f;
            // UIManager.Instance?.HidePauseMenu();
        }
    }

    private void OnPlayerDied()
    {
        currentState = GameState.Dead;
        Debug.Log("[GameManager] Jugador muerto. Esperando respawn...");
    }

    public void OnPlayerRespawned()
    {
        currentState = GameState.Playing;
    }

    public GameState CurrentState => currentState;
    public bool IsPlaying => currentState == GameState.Playing;
}