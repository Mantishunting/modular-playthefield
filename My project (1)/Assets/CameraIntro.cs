using UnityEngine;
using System.Collections;

public class CameraIntro : MonoBehaviour
{
    [Header("Camera States")]
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private float startZoom = 50f;
    [Space(10)]
    [SerializeField] private Vector3 endPosition;
    [SerializeField] private float endZoom = 10f;

    [Header("Animation Settings")]
    [SerializeField] private float totalDuration = 3f;
    [SerializeField][Range(0f, 1f)] private float easeInPercent = 0.3f;
    [SerializeField][Range(0f, 1f)] private float easeOutPercent = 0.3f;

    [Header("Controls")]
    [SerializeField] private KeyCode skipKey = KeyCode.Alpha0;
    [SerializeField] private bool playOnStart = true;

    [Header("References")]
    [SerializeField] private CameraController cameraController;

    private Camera cam;
    private bool isPlaying = false;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (cameraController == null)
        {
            cameraController = GetComponent<CameraController>();
        }

        // Disable player control during intro
        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        if (playOnStart)
        {
            StartCoroutine(PlayIntro());
        }
    }

    void Update()
    {
        // Skip key
        if (isPlaying && Input.GetKeyDown(skipKey))
        {
            StopAllCoroutines();
            SnapToEnd();
        }
    }

    private IEnumerator PlayIntro()
    {
        isPlaying = true;

        // Set to start state
        transform.position = startPosition;
        cam.orthographicSize = startZoom;

        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / totalDuration;

            // Apply custom easing curve
            float easedT = EaseInOut(t, easeInPercent, easeOutPercent);

            // Lerp position and zoom
            transform.position = Vector3.Lerp(startPosition, endPosition, easedT);
            cam.orthographicSize = Mathf.Lerp(startZoom, endZoom, easedT);

            yield return null;
        }

        // Snap to exact end state
        SnapToEnd();
    }

    private void SnapToEnd()
    {
        transform.position = endPosition;
        cam.orthographicSize = endZoom;
        isPlaying = false;

        // Enable player control
        if (cameraController != null)
        {
            cameraController.enabled = true;
        }

        // Disable this script
        this.enabled = false;
    }

    /// <summary>
    /// Custom easing function with configurable ease-in and ease-out zones
    /// </summary>
    private float EaseInOut(float t, float easeInPct, float easeOutPct)
    {
        // Ease in zone
        if (t < easeInPct)
        {
            float localT = t / easeInPct;
            return Mathf.SmoothStep(0f, easeInPct, localT);
        }
        // Ease out zone
        else if (t > 1f - easeOutPct)
        {
            float localT = (t - (1f - easeOutPct)) / easeOutPct;
            return Mathf.SmoothStep(1f - easeOutPct, 1f, localT);
        }
        // Linear middle zone
        else
        {
            return t;
        }
    }

    #region Editor Helpers
    [ContextMenu("Save Current as Start")]
    private void SaveCurrentAsStart()
    {
        startPosition = transform.position;
        startZoom = cam != null ? cam.orthographicSize : 50f;
        Debug.Log($"Saved Start: pos={startPosition}, zoom={startZoom}");
    }

    [ContextMenu("Save Current as End")]
    private void SaveCurrentAsEnd()
    {
        endPosition = transform.position;
        endZoom = cam != null ? cam.orthographicSize : 10f;
        Debug.Log($"Saved End: pos={endPosition}, zoom={endZoom}");
    }

    [ContextMenu("Preview Start")]
    private void PreviewStart()
    {
        transform.position = startPosition;
        if (cam != null) cam.orthographicSize = startZoom;
    }

    [ContextMenu("Preview End")]
    private void PreviewEnd()
    {
        transform.position = endPosition;
        if (cam != null) cam.orthographicSize = endZoom;
    }
    #endregion
}