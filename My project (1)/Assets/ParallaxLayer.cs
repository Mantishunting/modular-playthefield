using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField] private Camera targetCamera;
    [SerializeField][Range(0f, 1f)] private float parallaxFactor = 0.9f; // 0.9 = 90%, 1.0 = 100%

    [Header("Reference Values")]
    [SerializeField] private float referenceZoom = 10f; // The zoom level you consider "normal"

    private Vector3 initialPosition;
    private Vector3 initialScale;

    void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        initialPosition = transform.position;
        initialScale = transform.localScale;
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // Calculate how much we've zoomed from reference
        float zoomRatio = targetCamera.orthographicSize / referenceZoom;

        // Apply parallax to position relative to camera
        Vector3 cameraPos = targetCamera.transform.position;
        Vector3 offset = (cameraPos - initialPosition) * (1f - parallaxFactor);

        transform.position = initialPosition + offset * zoomRatio;

        // Optional: Scale based on zoom (makes it feel more "distant")
        // Uncomment if you want background to also scale
        // transform.localScale = initialScale * Mathf.Lerp(parallaxFactor, 1f, 1f - zoomRatio);
    }
}