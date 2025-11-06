using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SunbeamClipOccluder : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Sun sunRef;                 // your Sun (with Sun.cs)
    [SerializeField] private LayerMask occluderMask = ~0; // Everything while testing, later Default
    [SerializeField] private float skin = 0.05f;
    [SerializeField] private bool debugRays = false;

    private SpriteRenderer sr;
    private Material mat;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mat = sr.material; // instance so _CutV only affects this beam
    }

    void LateUpdate()
    {
        if (!sunRef || !sr || !sr.sprite) { SetCut(0f); return; }

        // 1) Light direction from Sun toward plant
        Vector3 d3 = sunRef.GetLightDirection();
        if (d3 == Vector3.zero) d3 = -sunRef.transform.up;
        Vector2 dir = ((Vector2)d3).normalized;

        // 2) Sprite world top and height (we assume your SunbeamTracker orients +Y along the beam)
        float h = sr.bounds.size.y;
        Vector2 top = (Vector2)transform.position + (Vector2)transform.up * (h * 0.5f);

        // 3) Cast from near the Sun along the beam
        Vector2 sunOrigin = (Vector2)sunRef.transform.position + dir * skin;
        float maxLen = h + 1f;
        RaycastHit2D hit = Physics2D.Raycast(sunOrigin, dir, maxLen + Vector2.Distance(sunOrigin, top), occluderMask);

        // 4) Pick the point to cut to: hit point if in front, else bottom of the sprite
        Vector2 target = hit.collider ? hit.point : (top + dir * h);

        // 5) Project target onto beam direction relative to the SPRITE TOP
        float visibleLen = Mathf.Clamp(Vector2.Dot(target - top, dir), 0f, h);

        // 6) Convert to UV cutoff for shader: keep top→visibleLen; discard below
        float cutV = 1f - (visibleLen / Mathf.Max(h, 0.0001f));
        SetCut(cutV);

        if (debugRays)
        {
            Debug.DrawLine(sunOrigin, sunOrigin + dir * 2f, Color.yellow);
            Debug.DrawLine(top, top + (Vector2)transform.up * h, Color.cyan);
            Color rayCol = hit.collider ? Color.red : Color.green;
            Vector2 rayEnd = hit.collider ? hit.point : (top + dir * h);
            Debug.DrawLine(sunOrigin, rayEnd, rayCol);
        }
    }

    private void SetCut(float v) => mat?.SetFloat("_CutV", Mathf.Clamp01(v));
}
