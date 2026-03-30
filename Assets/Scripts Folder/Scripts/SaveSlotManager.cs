using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.IO;
using System.Collections;

/// <summary>
/// TOKYO METROIDVANIA - SaveSlotManager
/// Sistema de 3 slots de guardado estilo Hollow Knight:
/// - Cada slot muestra: nombre, zona actual, tiempo jugado, % completado
/// - Autoguardado al pasar por puntos de guardado
/// - Los datos se guardan en archivos JSON
/// </summary>

// ─────────────────────────────────────────────────────────────────────────────
//  DATOS DE GUARDADO
// ─────────────────────────────────────────────────────────────────────────────
[Serializable]
public class SaveData
{
    public bool isEmpty = true;
    public string playerName = "Kagami";
    public string currentZone = "SHIBUYA NEXUS";
    public float playerX, playerY;
    public int currentHealth;
    public int maxHealth;
    public int currentSoul;
    public float playTimeSeconds;
    public float completionPercent;
    public string lastSaveDate;
    public int geoCollected;        // moneda del juego
    public string[] unlockedAreas;  // zonas desbloqueadas
}

// ─────────────────────────────────────────────────────────────────────────────
//  SAVE SLOT MANAGER
// ─────────────────────────────────────────────────────────────────────────────
public class SaveSlotManager : MonoBehaviour
{
    public static SaveSlotManager Instance { get; private set; }

    [Header("Panel de Slots")]
    [SerializeField] private GameObject saveSlotsPanel;
    [SerializeField] private SaveSlotUI[] slotUIs;          // 3 slots de UI
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Confirmación")]
    [SerializeField] private GameObject confirmDeletePanel;
    [SerializeField] private TMP_Text confirmDeleteText;

    [Header("Escenas")]
    [SerializeField] private string gameSceneName = "SampleScene";

    [Header("Autoguardado")]
    [SerializeField] private GameObject autoSaveIndicator;  // ícono de guardado
    [SerializeField] private float autoSaveIndicatorDuration = 2f;

    // Estado
    private SaveData[] saveSlots = new SaveData[3];
    private int slotToDelete = -1;
    private int activeSlot = -1;
    private float sessionStartTime;

    // Paths de guardado
    private string SavePath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
     //   DontDestroyOnLoad(gameObject);
    }

    private void Start()
{
    Debug.Log("SaveSlotManager Start! Panel asignado: " + (saveSlotsPanel != null ? saveSlotsPanel.name : "NULL"));
    LoadAllSlots();
    // if (saveSlotsPanel != null) 
      //  saveSlotsPanel.SetActive(false);
}
    
    // ─────────────────────────────────────────────────────────────
    //  CARGAR TODOS LOS SLOTS
    // ─────────────────────────────────────────────────────────────

    private void LoadAllSlots()
    {
        for (int i = 0; i < 3; i++)
        {
            saveSlots[i] = LoadSlot(i);
            if (slotUIs != null && i < slotUIs.Length)
                slotUIs[i]?.UpdateUI(saveSlots[i], i);
        }
    }

    private SaveData LoadSlot(int slot)
    {
        string path = SavePath(slot);
        if (!File.Exists(path))
            return new SaveData { isEmpty = true };

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch
        {
            return new SaveData { isEmpty = true };
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  ABRIR / CERRAR PANEL
    // ─────────────────────────────────────────────────────────────

    public void OpenSaveSlots()
{
    // Buscar el panel si la referencia es null
    if (saveSlotsPanel == null)
    saveSlotsPanel = GameObject.Find("Canvas")
        ?.transform.Find("SaveSlotsPanel")?.gameObject;
    
    Debug.Log("OpenSaveSlots llamado! Panel: " + saveSlotsPanel);
    LoadAllSlots();
    if (saveSlotsPanel != null) 
        saveSlotsPanel.SetActive(true);
}

    public void CloseSaveSlots()
    {
        if (saveSlotsPanel != null) saveSlotsPanel.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    //  SELECCIONAR SLOT (Jugar / Continuar)
    // ─────────────────────────────────────────────────────────────

    public void OnSlotSelected(int slotIndex)
    {
        activeSlot = slotIndex;
        sessionStartTime = Time.time;

        if (saveSlots[slotIndex].isEmpty)
        {
            // Nueva partida
            saveSlots[slotIndex] = new SaveData
            {
                isEmpty          = false,
                playerName       = "Kagami",
                currentZone      = "SHIBUYA NEXUS",
                currentHealth    = 5,
                maxHealth        = 5,
                currentSoul      = 0,
                playTimeSeconds  = 0f,
                completionPercent = 0f,
                lastSaveDate     = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                geoCollected     = 0
            };
            SaveSlotToFile(slotIndex);
        }

        // Cargar la escena del juego
        StartCoroutine(LoadGameScene(slotIndex));
    }

    private IEnumerator LoadGameScene(int slot)
    {
        // Fade out
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene(gameSceneName);
    }

    // ─────────────────────────────────────────────────────────────
    //  GUARDAR PARTIDA (llamado por SavePoint)
    // ─────────────────────────────────────────────────────────────

    public void SaveCurrentGame(Vector3 playerPos, string zoneName)
    {
        if (activeSlot < 0) return;

        // Actualizar datos
        var player = FindObjectOfType<PlayerHealth>();
        saveSlots[activeSlot].isEmpty         = false;
        saveSlots[activeSlot].playerX         = playerPos.x;
        saveSlots[activeSlot].playerY         = playerPos.y;
        saveSlots[activeSlot].currentZone     = zoneName;
        saveSlots[activeSlot].playTimeSeconds += Time.time - sessionStartTime;
        saveSlots[activeSlot].lastSaveDate    = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        if (player != null)
        {
            saveSlots[activeSlot].currentHealth = player.CurrentHealth;
            saveSlots[activeSlot].maxHealth     = player.MaxHealth;
            saveSlots[activeSlot].currentSoul   = player.CurrentSoul;
        }

        sessionStartTime = Time.time; // resetear timer de sesión

        SaveSlotToFile(activeSlot);
        StartCoroutine(ShowAutoSaveIndicator());

        Debug.Log($"[Save] Partida guardada en slot {activeSlot} - Zona: {zoneName}");
    }

    private void SaveSlotToFile(int slot)
    {
        try
        {
            string json = JsonUtility.ToJson(saveSlots[slot], true);
            File.WriteAllText(SavePath(slot), json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] Error guardando slot {slot}: {e.Message}");
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  ELIMINAR SLOT
    // ─────────────────────────────────────────────────────────────

    public void OnDeleteSlot(int slotIndex)
    {
        slotToDelete = slotIndex;
        if (confirmDeletePanel != null)
        {
            confirmDeletePanel.SetActive(true);
            if (confirmDeleteText != null)
                confirmDeleteText.text = $"¿Eliminar partida {slotIndex + 1}?\nEsta acción no se puede deshacer.";
        }
    }

    public void ConfirmDelete()
    {
        if (slotToDelete < 0) return;

        string path = SavePath(slotToDelete);
        if (File.Exists(path)) File.Delete(path);

        saveSlots[slotToDelete] = new SaveData { isEmpty = true };
        slotUIs[slotToDelete]?.UpdateUI(saveSlots[slotToDelete], slotToDelete);

        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
        slotToDelete = -1;

        Debug.Log($"[Save] Slot {slotToDelete + 1} eliminado.");
    }

    public void CancelDelete()
    {
        slotToDelete = -1;
        if (confirmDeletePanel != null) confirmDeletePanel.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    //  INDICADOR DE AUTOGUARDADO
    // ─────────────────────────────────────────────────────────────

    private IEnumerator ShowAutoSaveIndicator()
    {
        if (autoSaveIndicator == null) yield break;

        autoSaveIndicator.SetActive(true);
        yield return new WaitForSeconds(autoSaveIndicatorDuration);
        autoSaveIndicator.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    //  GETTERS
    // ─────────────────────────────────────────────────────────────

    public SaveData GetSlotData(int slot) => saveSlots[slot];
    public int ActiveSlot => activeSlot;
    public bool HasAnySave() =>
        saveSlots[0]?.isEmpty == false ||
        saveSlots[1]?.isEmpty == false ||
        saveSlots[2]?.isEmpty == false;
}