using UnityEngine;
using System.Collections;

/// <summary>
/// TOKYO METROIDVANIA - CameraController
/// Cámara suave con offset predictivo (mira hacia donde caminas),
/// zoom dinámico y screen shake neon.
/// </summary>
public class CameraController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Seguimiento")]
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);

    [Header("Offset Predictivo")]
    [SerializeField] private float lookAheadX = 3f;   // cuánto adelanta la cámara en X
    [SerializeField] private float lookAheadY = 1.5f; // cuánto baja/sube la cámara
    [SerializeField] private float lookAheadSmooth = 0.3f;

    [Header("Límites del Mundo")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private float minX = -50f;
    [SerializeField] private float maxX =  50f;
    [SerializeField] private float minY = -30f;
    [SerializeField] private float maxY =  30f;

    [Header("Screen Shake")]
    [SerializeField] private float shakeDecay = 5f;

    [Header("Zoom")]
    [SerializeField] private float defaultSize = 7f;
    [SerializeField] private float zoomSpeed = 2f;
    private float targetSize;

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────────────────────

    private Camera cam;
    private Vector3 velocity = Vector3.zero;
    private Vector3 lookAheadOffset;
    private Vector3 lookAheadVelocity;

    // Shake
    private float shakeMagnitude;
    private float shakeDuration;
    private float shakeTimer;

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetSize = defaultSize;
        if (cam != null) cam.orthographicSize = defaultSize;

        if (target == null && GameObject.FindWithTag("Player") is var p && p != null)
            target = p.transform;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        UpdateLookAhead();
        FollowTarget();
        HandleZoom();
        HandleShake();
    }

    // ─────────────────────────────────────────────────────────────
    //  FOLLOW
    // ─────────────────────────────────────────────────────────────

    private void UpdateLookAhead()
    {
        var playerCtrl = target.GetComponent<PlayerController>();
        if (playerCtrl == null) return;

        float tx = playerCtrl.InputX * lookAheadX;
        float ty = playerCtrl.InputY * lookAheadY;

        Vector3 targetLookAhead = new Vector3(tx, ty, 0f);
        lookAheadOffset = Vector3.SmoothDamp(
            lookAheadOffset, targetLookAhead,
            ref lookAheadVelocity, lookAheadSmooth);
    }

    private void FollowTarget()
    {
        Vector3 desiredPos = target.position + offset + lookAheadOffset;

        if (useBounds)
        {
            float halfH = cam.orthographicSize;
            float halfW = cam.orthographicSize * cam.aspect;
            desiredPos.x = Mathf.Clamp(desiredPos.x, minX + halfW, maxX - halfW);
            desiredPos.y = Mathf.Clamp(desiredPos.y, minY + halfH, maxY - halfH);
        }

        desiredPos.z = offset.z; // mantener Z fijo

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos,
            ref velocity, smoothTime);
    }

    // ─────────────────────────────────────────────────────────────
    //  ZOOM
    // ─────────────────────────────────────────────────────────────

    private void HandleZoom()
    {
        if (cam != null)
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, Time.deltaTime * zoomSpeed);
    }

    /// <summary>Cambiar zoom de forma suave (útil al entrar a salas grandes o jefes).</summary>
    public void SetZoom(float size) => targetSize = size;
    public void ResetZoom() => targetSize = defaultSize;

    // ─────────────────────────────────────────────────────────────
    //  SCREEN SHAKE
    // ─────────────────────────────────────────────────────────────

    private void HandleShake()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float s = shakeMagnitude * (shakeTimer / shakeDuration);
            Vector3 shakeOffset = Random.insideUnitSphere * s;
            shakeOffset.z = 0f;
            transform.position += shakeOffset;
        }
    }

    /// <summary>Activa un screen shake. magnitude en unidades de mundo, duration en segundos.</summary>
    public void ShakeCamera(float magnitude, float duration)
    {
        shakeMagnitude = magnitude;
        shakeDuration  = duration;
        shakeTimer     = duration;
    }

    // ─────────────────────────────────────────────────────────────
    //  CUARTOS / ROOMS (transición entre áreas)
    // ─────────────────────────────────────────────────────────────

    /// <summary>Limita la cámara a una habitación específica.</summary>
    public void SetRoomBounds(float x1, float x2, float y1, float y2)
    {
        minX = x1; maxX = x2;
        minY = y1; maxY = y2;
        useBounds = true;
    }

    public void DisableBounds() => useBounds = false;
}