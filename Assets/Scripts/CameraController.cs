using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float panSpeed = 15f; 

    [Header("Zoom Ayarları (Perspektif)")]
    public float zoomSpeed = 25f;
    public float minZoomZ = -5f;
    public float maxZoomZ = -25f;

    [Header("Kamera Sınırları")]
    public Vector2 minBounds = new Vector2(-20f, -20f); 
    public Vector2 maxBounds = new Vector2(20f, 20f);   

    private Camera cam;
    private Vector3 dragOrigin;
    
    // Shake offset
    private Vector3 shakeOffset = Vector3.zero;
    private float shakeTimer = 0f;
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;
    private Vector3 targetPosition = Vector3.zero;

    void Start()
    {
        cam = Camera.main;
        targetPosition = transform.position;
        
        if (cam.orthographic) 
        {
            cam.orthographic = false;
        }
    }

    void LateUpdate()
    {
        HandleKeyboardPan();
        HandleMouseDragPan();
        HandleZoom();
        UpdateShake();
        ClampCameraPosition();
    }

    private void HandleKeyboardPan()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(x, y, 0).normalized * panSpeed * Time.deltaTime;
        targetPosition += move;
    }

    private void HandleMouseDragPan()
    {
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) 
        {
            dragOrigin = GetMouseWorldPosition();
        }

        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            Vector3 currentMousePos = GetMouseWorldPosition();
            Vector3 difference = dragOrigin - currentMousePos;
            targetPosition += difference;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(cam.transform.position.z); 
        return cam.ScreenToWorldPoint(mousePos);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            Vector3 pos = targetPosition;
            pos.z += scroll * zoomSpeed;
            pos.z = Mathf.Clamp(pos.z, maxZoomZ, minZoomZ); 
            targetPosition = pos;
        }
    }

    private void UpdateShake()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float progress = 1f - (shakeTimer / shakeDuration);
            float falloff = 1f - (progress * progress);
            shakeOffset = Random.insideUnitSphere * shakeMagnitude * falloff;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        transform.position = targetPosition + shakeOffset;
    }

    private void ClampCameraPosition()
    {
        float clampedX = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
        float clampedY = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        targetPosition = new Vector3(clampedX, clampedY, targetPosition.z);
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeTimer = duration;
        shakeMagnitude = magnitude;
    }

    public static void ShakeLight()
    {
        CameraController controller = FindObjectOfType<CameraController>();
        if (controller != null)
            controller.Shake(0.1f, 0.075f);
    }
}