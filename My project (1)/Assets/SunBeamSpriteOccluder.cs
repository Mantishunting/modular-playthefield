using UnityEngine;

/// Attach this to your **Sunbeam** object (the one with the SpriteRenderer).
/// Drag your Sun (with Sun.cs) into `sunRef`.
/// The script will resize and position the PNG so it stops at the first occluder.
[RequireComponent(typeof(SpriteRenderer))]
public class SunbeamSpriteOccluder : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Sun sunRef;                 // your Sun.cs object

    [Header("Beam (world units)")]
    [SerializeField] private float beamWidth = 10f;      // how wide the PNG should appear
    [SerializeField] private float maxBeamLength = 1500f;
    [SerializeField] private float skin = 0.05f;         // avoid self-hits at the Sun

    [Header("Raycast")]
    [SerializeField] private LayerMask occluderMask = default; // usually Default

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // IMPORTANT: must be Sliced or Tiled for .size to work
        sr.drawMode = SpriteDrawMode.Sliced;   // if your sprite has no borders, Sliced still works
        // Make sure the material is URP/Unlit with your sunshine1.png assigned (Base Map), Surface Type = Transparent.
    }

    void LateUpdate()
    {
        if (!sunRef || !sunRef.IsSunEnabled())
        {
            sr.size = new Vector2(beamWidth, 0f);
            return;
        }

        // Origin and direction (from Sun toward the plant/center)
        Vector3 origin3 = sunRef.transform.position;
        Vector3 dir3 = sunRef.GetLightDirection();          // already normalized in your Sun
        if (dir3 == Vector3.zero) dir3 = -sunRef.transform.up; // fallback if needed

        Vector2 origin = (Vector2)origin3 + (Vector2)dir3 * skin;
        Vector2 dir = ((Vector2)dir3).normalized;

        // We cast at left/right edges of the beam and take the shorter hit to keep a clean rectangle
        Vector2 perp = new Vector2(-dir.y, dir.x);
        float halfW = beamWidth * 0.5f;

        Vector2 leftStart = origin + perp * (-halfW);
        Vector2 rightStart = origin + perp * (halfW);

        float lenL = CastLength(leftStart, dir);
        float lenR = CastLength(rightStart, dir);
        float beamLen = Mathf.Min(lenL, lenR);
        beamLen = Mathf.Clamp(beamLen, 0f, maxBeamLength);

        // Resize sprite in **world units** (Sliced/Tiled mode uses sr.size in world space)
        sr.size = new Vector2(beamWidth, beamLen);

        // Orient the sprite so its local +Y points along the sun direction
        transform.up = dir;

        // Place the sprite so its **top edge** is at the Sun:
        // Sprite pivots at its center, so we offset by half the length along the direction.
        Vector3 center = origin3 + dir3 * (beamLen * 0.5f);
        transform.position = center;
    }

    float CastLength(Vector2 start, Vector2 dir)
    {
        var hit = Physics2D.Raycast(start, dir, maxBeamLength, occluderMask);
        return hit.collider ? hit.distance : maxBeamLength;
    }
}
