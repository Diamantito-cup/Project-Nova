using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// TOKYO METROIDVANIA - RoomManager
/// Maneja las zonas del mundo semi-futurista de Tokio.
/// Zonas: Shibuya (hub), Akihabara (tech-labs), Shinjuku (underground), 
///        Asakusa (ruinas antiguas+tech), Odaiba (isla robot), Kabukicho (boss zone)
/// </summary>
public class RoomManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  SINGLETON
    // ─────────────────────────────────────────────────────────────

    public static RoomManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────────────────────────
    //  ZONA
    // ─────────────────────────────────────────────────────────────

    [System.Serializable]
    public class Zone
    {
        public string zoneName;
        public string zoneDisplayName;
        [TextArea] public string zoneDescription;
        public Color zoneAmbientColor = Color.white;
        public Color zoneFogColor;
        public float cameraZoom = 7f;
        public AudioClip zoneMusic;
        public bool isDiscovered;
        public bool isBossZone;
    }

    [Header("Zonas del Mundo de Tokio")]
    [SerializeField] private List<Zone> zones = new List<Zone>
    {
        new Zone { zoneName = "shibuya",    zoneDisplayName = "SHIBUYA NEXUS",       cameraZoom = 7f  },
        new Zone { zoneName = "akihabara",  zoneDisplayName = "AKIHABARA LABS",      cameraZoom = 6f  },
        new Zone { zoneName = "shinjuku",   zoneDisplayName = "SHINJUKU UNDERGROUND",cameraZoom = 6.5f},
        new Zone { zoneName = "asakusa",    zoneDisplayName = "ASAKUSA RUINS",       cameraZoom = 8f  },
        new Zone { zoneName = "odaiba",     zoneDisplayName = "ODAIBA CORE",         cameraZoom = 9f  },
        new Zone { zoneName = "kabukicho",  zoneDisplayName = "KABUKICHO BOSS ARENA",cameraZoom = 10f, isBossZone = true }
    };

    [Header("Referencias")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D globalLight;
    [SerializeField] private AudioSource musicSource;

    private Zone currentZone;

    // ─────────────────────────────────────────────────────────────
    //  TRANSICIÓN DE ZONA
    // ─────────────────────────────────────────────────────────────

    public void EnterZone(string zoneName)
    {
        Zone zone = zones.Find(z => z.zoneName == zoneName);
        if (zone == null)
        {
            Debug.LogWarning($"[RoomManager] Zona '{zoneName}' no encontrada.");
            return;
        }

        if (currentZone == zone) return;
        currentZone = zone;

        if (!zone.isDiscovered)
        {
            zone.isDiscovered = true;
            UIManager.Instance?.ShowNotification(zone.zoneDisplayName);
        }

        // Cambiar cámara
        cameraController?.SetZoom(zone.cameraZoom);

        // Cambiar música
        if (musicSource != null && zone.zoneMusic != null)
            StartCoroutine(CrossfadeMusic(zone.zoneMusic));

        // Cambiar iluminación ambiental
        StartCoroutine(TransitionAmbientLight(zone.zoneAmbientColor));

        Debug.Log($"[Zone] Entrando a {zone.zoneDisplayName}");
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip)
    {
        float fadeDur = 1.0f;
        float startVol = musicSource.volume;

        // Fade out
        float elapsed = 0f;
        while (elapsed < fadeDur)
        {
            musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDur);
            elapsed += Time.deltaTime;
            yield return null;
        }

        musicSource.clip = newClip;
        musicSource.Play();

        // Fade in
        elapsed = 0f;
        while (elapsed < fadeDur)
        {
            musicSource.volume = Mathf.Lerp(0f, startVol, elapsed / fadeDur);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator TransitionAmbientLight(Color targetColor)
    {
        // if (globalLight == null) yield break;
        yield break; //
        Color startColor = globalLight.color;
        float elapsed = 0f;
        float duration = 1.5f;

        while (elapsed < duration)
        {
            globalLight.color = Color.Lerp(startColor, targetColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        globalLight.color = targetColor;
    }

    public Zone GetCurrentZone() => currentZone;
    public bool IsZoneDiscovered(string name) => zones.Find(z => z.zoneName == name)?.isDiscovered ?? false;
}

// ─────────────────────────────────────────────────────────────────────────────
//  ZoneTrigger — collider que activa la transición de zona
// ─────────────────────────────────────────────────────────────────────────────
public class ZoneTrigger : MonoBehaviour
{
    [SerializeField] private string zoneName;
    [SerializeField] private Transform cameraMinBound;
    [SerializeField] private Transform cameraMaxBound;
    [SerializeField] private Transform respawnPoint;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        RoomManager.Instance?.EnterZone(zoneName);

        // Actualizar límites de cámara para esta habitación
        if (cameraMinBound != null && cameraMaxBound != null)
        {
            Camera.main.GetComponent<CameraController>()?.SetRoomBounds(
                cameraMinBound.position.x, cameraMaxBound.position.x,
                cameraMinBound.position.y, cameraMaxBound.position.y);
        }

        // Actualizar punto de respawn al entrar a una zona
        if (respawnPoint != null && col.TryGetComponent<PlayerHealth>(out var ph))
            ph.SetRespawnPoint(respawnPoint);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  SavePoint / Banco — punto de guardado y curación
// ─────────────────────────────────────────────────────────────────────────────
public class SavePoint : MonoBehaviour
{
    [SerializeField] private bool playerIsUsing;
    [SerializeField] private Animator benchAnim;
    [SerializeField] private ParticleSystem glowFX;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;
        ActivateSavePoint(col.GetComponent<PlayerHealth>());
    }

    private void ActivateSavePoint(PlayerHealth ph)
    {
        if (ph == null) return;

        // Curar al jugador completamente
        int diff = ph.MaxHealth - ph.CurrentHealth;
        if (diff > 0)
            for (int i = 0; i < diff; i++)
                ph.GainSoul(33); // llenar alma y curarse

        // Guardar datos (integrar con SaveSystem si se desea)
        SaveSystem.Instance?.SaveGame(transform.position);

        UIManager.Instance?.ShowNotification("DATOS GUARDADOS");
        benchAnim?.SetTrigger("Activate");
        glowFX?.Play();

        Debug.Log("[SavePoint] Juego guardado.");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  SaveSystem — sistema básico de guardado con PlayerPrefs
// ─────────────────────────────────────────────────────────────────────────────
public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveGame(Vector3 playerPos)
    {
        PlayerPrefs.SetFloat("player_x", playerPos.x);
        PlayerPrefs.SetFloat("player_y", playerPos.y);
        PlayerPrefs.SetFloat("player_z", playerPos.z);
        PlayerPrefs.SetString("last_zone", RoomManager.Instance?.GetCurrentZone()?.zoneName ?? "shibuya");
        PlayerPrefs.Save();
    }

    public Vector3 LoadPlayerPosition()
    {
        return new Vector3(
            PlayerPrefs.GetFloat("player_x", 0f),
            PlayerPrefs.GetFloat("player_y", 0f),
            PlayerPrefs.GetFloat("player_z", 0f));
    }

    public string LoadLastZone() => PlayerPrefs.GetString("last_zone", "shibuya");
    public bool HasSaveData() => PlayerPrefs.HasKey("player_x");
}