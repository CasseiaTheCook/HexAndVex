using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float panSpeed = 15f; 

    [Header("Zoom Ayarları")]
    public float zoomSpeed = 5f;
    public float minZoom = 4f; 
    public float maxZoom = 15f; 

    [Header("Kamera Sınırları")]
    public Vector2 minBounds = new Vector2(-20f, -20f); 
    public Vector2 maxBounds = new Vector2(20f, 20f);   

    private Camera cam;
    private Vector3 dragOrigin;

    void Start()
    {
        cam = Camera.main;
    }

    // DÜZELTME 1: Update yerine LateUpdate! Her şey hesaplandıktan sonra kamera hareket eder, sıfır titreme olur.
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
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            
            // DÜZELTME 2: dragSpeed çarpanı silindi! Fare nereye giderse kamera 1:1 oranında oraya kitlenir. Titreme biter.
            transform.position += difference; 
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }

    private void ClampCameraPosition()
    {
        float clampedX = Mathf.Clamp(transform.position.x, minBounds.x, maxBounds.x);
        float clampedY = Mathf.Clamp(transform.position.y, minBounds.y, maxBounds.y);
        
        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
}