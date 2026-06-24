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

    [Header("Keyboard Controls")]
    [SerializeField] private float keyboardPanSpeed = 15f;
    [SerializeField] private float keyboardZoomSpeed = 15f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    public static bool IsPanning { get; private set; } = false;

    private Camera cam;
    private Vector3 lastMousePosition;
    private bool isMouseOrTouchPanning = false;
    private bool isOneFingerPanning = false;

    private float initialTouchDistance;
    private float initialOrthographicSize;

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
        // Touch Input takes priority (mobile/touch screens)
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        else
        {
            // Desktop/Mouse & Keyboard Input
            HandlePanning();
            HandleZoom();
            HandleKeyboardMovement();
        }

        if (enableEdgePanning)
        {
            HandleEdgePanning();
        }
    }

    private bool IsOverBlock(Vector2 screenPosition)
    {
        if (cam == null) return false;
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPosition);
        worldPos.z = 0;
        
        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        if (hit != null && hit.GetComponentInParent<HumanClick>() != null)
        {
            return true;
        }
        return false;
    }

    private bool IsOverUI(int pointerId)
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return false;
        return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(pointerId);
    }

    private bool IsOverUIOrBlock(Touch touch)
    {
        if (IsOverUI(touch.fingerId)) return true;
        return IsOverBlock(touch.position);
    }

    void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (!IsOverUIOrBlock(touch))
                {
                    isOneFingerPanning = true;
                    IsPanning = true;
                    lastMousePosition = touch.position;
                }
            }
            else if (touch.phase == TouchPhase.Moved && isOneFingerPanning)
            {
                Vector3 dragDelta = (Vector3)touch.position - lastMousePosition;
                float worldUnitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
                Vector3 worldDelta = new Vector3(
                    dragDelta.x * worldUnitsPerPixel,
                    dragDelta.y * worldUnitsPerPixel,
                    0
                );
                transform.position -= worldDelta * panSpeed;
                lastMousePosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isOneFingerPanning = false;
                IsPanning = false;
            }
        }
        else if (Input.touchCount == 2)
        {
            isOneFingerPanning = false; // Cancel single finger panning
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                initialTouchDistance = Vector2.Distance(touch0.position, touch1.position);
                initialOrthographicSize = cam.orthographicSize;
                lastMousePosition = (touch0.position + touch1.position) * 0.5f;
                IsPanning = true;
            }
            else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                // Zoom
                float currentTouchDistance = Vector2.Distance(touch0.position, touch1.position);
                if (initialTouchDistance > 0.01f)
                {
                    float factor = initialTouchDistance / currentTouchDistance;
                    float newSize = initialOrthographicSize * factor;
                    cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
                }

                // Pan
                Vector2 currentMidpoint = (touch0.position + touch1.position) * 0.5f;
                Vector3 dragDelta = (Vector3)currentMidpoint - lastMousePosition;
                float worldUnitsPerPixel = (cam.orthographicSize * 2f) / Screen.height;
                Vector3 worldDelta = new Vector3(
                    dragDelta.x * worldUnitsPerPixel,
                    dragDelta.y * worldUnitsPerPixel,
                    0
                );
                transform.position -= worldDelta * panSpeed;
                lastMousePosition = currentMidpoint;
                IsPanning = true;
            }
        }
        else
        {
            isOneFingerPanning = false;
            IsPanning = false;
        }
    }

    private Vector3 panStartMousePosition;
    private bool isPanningTriggered = false;
    private const float panDragThreshold = 5f; // pixels

    void HandlePanning()
    {
        // Right mouse button OR Middle mouse button for panning
        if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            panStartMousePosition = Input.mousePosition;
            lastMousePosition = Input.mousePosition;
            isPanningTriggered = false;

            // Middle click or Right click on empty space starts panning immediately
            bool isMiddleClick = Input.GetMouseButtonDown(2);
            bool isRightClickOnEmptySpace = Input.GetMouseButtonDown(1) && !IsOverBlock(Input.mousePosition) && !IsOverUI(-1);

            if (isMiddleClick || isRightClickOnEmptySpace)
            {
                isPanningTriggered = true;
                isMouseOrTouchPanning = true;
                IsPanning = true;

                if (showDebugLogs)
                {
                    Debug.Log("Started panning immediately");
                }
            }
        }

        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            if (!isPanningTriggered)
            {
                // Check if mouse dragged past the threshold
                if (Vector3.Distance(Input.mousePosition, panStartMousePosition) > panDragThreshold)
                {
                    isPanningTriggered = true;
                    isMouseOrTouchPanning = true;
                    IsPanning = true;

                    if (showDebugLogs)
                    {
                        Debug.Log("Started panning via mouse drag threshold");
                    }
                }
            }

            if (isMouseOrTouchPanning)
            {
                // Calculate mouse movement in screen space
                Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

                // Convert to world space movement
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

        if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
        {
            if (isMouseOrTouchPanning && showDebugLogs)
            {
                Debug.Log("Stopped panning");
            }

            isMouseOrTouchPanning = false;
            IsPanning = false;
            isPanningTriggered = false;
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

    void HandleKeyboardMovement()
    {
        // WASD or Arrow Keys for panning
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h != 0 || v != 0)
        {
            Vector3 move = new Vector3(h, v, 0).normalized;
            // Scale speed by orthographicSize so movement speed scales with zoom level
            float speedScale = cam.orthographicSize / 10f;
            transform.position += move * keyboardPanSpeed * speedScale * Time.deltaTime;
        }

        // Keypad Plus/Minus, Equals/Minus, PageUp/PageDown for zoom
        float zoomDir = 0f;
        if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.PageUp))
        {
            zoomDir = -1f; // zoom in
        }
        else if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.PageDown))
        {
            zoomDir = 1f; // zoom out
        }

        if (zoomDir != 0)
        {
            float newSize = cam.orthographicSize + (zoomDir * keyboardZoomSpeed * Time.deltaTime);
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
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