using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Pan Settings")]
    [SerializeField] private float panSpeed = 1f;
    [SerializeField] private bool invertPan = false;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 50f;

    [Header("Edge Panning (Optional)")]
    [SerializeField] private bool enableEdgePanning = false;
    [SerializeField] private float edgePanSpeed = 10f;
    [SerializeField] private float edgePanBorder = 20f; // pixels from edge

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private Camera cam;
    private Vector3 lastMousePosition;
    private bool isPanning = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraController requires a Camera component!");
        }

        if (showDebugLogs)
        {
            Debug.Log("CameraController initialized");
        }
    }

    void Update()
    {
        HandlePanning();
        HandleZoom();

        if (enableEdgePanning)
        {
            HandleEdgePanning();
        }
    }

    void HandlePanning()
    {
        // Right mouse button for panning
        if (Input.GetMouseButtonDown(1))
        {
            isPanning = true;
            lastMousePosition = Input.mousePosition;

            if (showDebugLogs)
            {
                Debug.Log("Started panning");
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            isPanning = false;

            if (showDebugLogs)
            {
                Debug.Log("Stopped panning");
            }
        }

        if (isPanning)
        {
            // Calculate mouse movement in screen space
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

            // Convert to world space movement
            // Orthographic camera: 1 screen pixel = (orthographicSize * 2 / Screen.height) world units
            float worldUnitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
            Vector3 worldDelta = new Vector3(
                mouseDelta.x * worldUnitsPerPixel,
                mouseDelta.y * worldUnitsPerPixel,
                0
            );

            // Apply pan direction (inverted feels more natural - drag map to move)
            if (!invertPan)
            {
                worldDelta = -worldDelta;
            }

            // Move camera
            transform.position += worldDelta * panSpeed;

            lastMousePosition = Input.mousePosition;
        }
    }

    void HandleZoom()
    {
        // Mouse wheel zoom
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");

        if (scrollDelta != 0)
        {
            float newSize = cam.orthographicSize - (scrollDelta * zoomSpeed);
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);

            if (showDebugLogs)
            {
                Debug.Log($"Zoomed to size: {cam.orthographicSize}");
            }
        }
    }

    void HandleEdgePanning()
    {
        Vector3 edgePan = Vector3.zero;

        // Check mouse position relative to screen edges
        if (Input.mousePosition.x < edgePanBorder)
        {
            edgePan.x = -1;
        }
        else if (Input.mousePosition.x > Screen.width - edgePanBorder)
        {
            edgePan.x = 1;
        }

        if (Input.mousePosition.y < edgePanBorder)
        {
            edgePan.y = -1;
        }
        else if (Input.mousePosition.y > Screen.height - edgePanBorder)
        {
            edgePan.y = 1;
        }

        // Apply edge panning
        if (edgePan != Vector3.zero)
        {
            transform.position += edgePan * edgePanSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// Focuses camera on a specific world position
    /// </summary>
    public void FocusOnPosition(Vector3 worldPosition)
    {
        transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);

        if (showDebugLogs)
        {
            Debug.Log($"Camera focused on {worldPosition}");
        }
    }

    /// <summary>
    /// Sets the camera zoom level
    /// </summary>
    public void SetZoom(float orthographicSize)
    {
        cam.orthographicSize = Mathf.Clamp(orthographicSize, minZoom, maxZoom);
    }

    /// <summary>
    /// Resets camera to default position and zoom
    /// </summary>
    public void ResetCamera(Vector3 defaultPosition, float defaultZoom)
    {
        transform.position = defaultPosition;
        cam.orthographicSize = Mathf.Clamp(defaultZoom, minZoom, maxZoom);

        if (showDebugLogs)
        {
            Debug.Log("Camera reset");
        }
    }
}