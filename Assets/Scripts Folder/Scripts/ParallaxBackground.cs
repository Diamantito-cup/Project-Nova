using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [SerializeField] private float parallaxSpeedX = 0.3f;
    [SerializeField] private bool infiniteHorizontal = true;

    private Transform cam;
    private Vector3 lastCamPos;
    private float textureUnitSizeX;

    private void Start()
    {
        cam = Camera.main.transform;
        lastCamPos = cam.position;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
            textureUnitSizeX = sr.sprite.texture.width / sr.sprite.pixelsPerUnit;
        else
            textureUnitSizeX = 20f; // valor por defecto
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        Vector3 deltaMove = cam.position - lastCamPos;
        transform.position += new Vector3(deltaMove.x * parallaxSpeedX, 0f, 0f);
        lastCamPos = cam.position;

        if (infiniteHorizontal && textureUnitSizeX > 0f)
        {
            float camX = cam.position.x;
            float bgX = transform.position.x;
            if (Mathf.Abs(camX - bgX) >= textureUnitSizeX)
            {
                float offset = (camX - bgX) % textureUnitSizeX;
                transform.position = new Vector3(camX + offset, transform.position.y, transform.position.z);
            }
        }
    }
}