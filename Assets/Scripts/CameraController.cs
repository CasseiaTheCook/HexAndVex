using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float panSpeed = 15f; 

    [Header("Zoom Ayarları (Perspektif)")]
    public float zoomSpeed = 25f; // Perspektif için biraz daha yüksek hız
    public float minZoomZ = -5f;  // En fazla ne kadar YAKLAŞSIN (Kamera Z değeri)
    public float maxZoomZ = -25f; // En fazla ne kadar UZAKLAŞSIN (Kamera Z değeri)

    [Header("Kamera Sınırları")]
    public Vector2 minBounds = new Vector2(-20f, -20f); 
    public Vector2 maxBounds = new Vector2(20f, 20f);   

    private Camera cam;
    private Vector3 dragOrigin;

    void Start()
    {
        cam = Camera.main;
        
        // Yanlışlıkla Orthographic kaldıysa otomatik Perspektife çevirelim
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
        ClampCameraPosition();
    }

    private void HandleKeyboardPan()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(x, y, 0).normalized * panSpeed * Time.deltaTime;
        transform.Translate(move, Space.World);
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
            
            // Farenin hareketi kadar kamerayı da o yöne çekiyoruz
            transform.position += difference; 
        }
    }

    // YENİ: Perspektif kamerada farenin zemin (Z=0) üzerindeki tam konumunu bulur!
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        // Kameranın derinliğini (Z ekseni) mutlak değer olarak veriyoruz ki fare zemin seviyesini algılasın
        mousePos.z = Mathf.Abs(cam.transform.position.z); 
        return cam.ScreenToWorldPoint(mousePos);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            // Perspektif kamerada Zoom, kameranın Z ekseninde ileri veya geri gitmesidir.
            Vector3 pos = transform.position;
            pos.z += scroll * zoomSpeed;
            
            // Kameranın Z sınırlarını belirliyoruz (örn: -25 ile -5 arası)
            pos.z = Mathf.Clamp(pos.z, maxZoomZ, minZoomZ); 
            
            transform.position = pos;
        }
    }

    private void ClampCameraPosition()
    {
        float clampedX = Mathf.Clamp(transform.position.x, minBounds.x, maxBounds.x);
        float clampedY = Mathf.Clamp(transform.position.y, minBounds.y, maxBounds.y);
        
        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
}