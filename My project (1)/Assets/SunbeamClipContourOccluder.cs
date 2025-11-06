using UnityEngine;

/// Per-column cutoff that hugs colliders using a 1xN texture fed to the shader.
/// Attach to the same object as the Sunbeam SpriteRenderer that uses the SunbeamClip shader.
[RequireComponent(typeof(SpriteRenderer))]
public class SunbeamClipContourOccluder : MonoBehaviour
{
    public enum RayOriginMode
    {
        // Version A: Project the sun onto each column's line (your “CHANGED” variant).
        ProjectSunOntoColumn,

        // Version B: Start at the sun position, offset laterally per column (your second script).
        SunPositionPlusLateral
    }
    [Header("Direction")]
    [SerializeField] private bool invertBeamDirection = true; // set true if rays point the wrong way


    [Header("Links")]
    [SerializeField] private Sun sunRef;                    // drag your Sun (with Sun.cs)
    [SerializeField] private LayerMask occluderMask = ~0;   // Everything to test; then Default
    [SerializeField] private float skin = 0.05f;

    [Header("Sampling")]
    [SerializeField, Range(8, 256)] private int samples = 64;  // 32–96 is a good range
    [SerializeField] private bool smooth = true;

    [Header("Rays")]
    [SerializeField] private RayOriginMode rayOriginMode = RayOriginMode.ProjectSunOntoColumn;

    [Header("Debug")]
    [SerializeField] private bool debugRays = false;

    private SpriteRenderer sr;
    private Material mat;
    private Texture2D cutTex;
    private Color[] row;

    // Cached shader property IDs
    private static readonly int CutTexId = Shader.PropertyToID("_CutTex");
    private static readonly int UseCutId = Shader.PropertyToID("_UseCutTex");

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mat = sr.material; // instance (so _UseCutTex/_CutTex are per-beam)
        Debug.Log($"[SunBeam] Material = {mat.name}, Shader = {mat.shader.name}");


        AllocateTex();
        mat.SetTexture(CutTexId, cutTex);
        mat.SetFloat(UseCutId, 1f); // tell shader to use the texture
    }

    void OnValidate()
    {
        if (samples < 8) samples = 8;
        if (Application.isPlaying && cutTex != null && cutTex.width != samples)
            AllocateTex();
    }

    void OnDestroy()
    {
        if (cutTex != null)
        {
            Destroy(cutTex);
            cutTex = null;
        }
        // No need to unset material texture; material instance will be cleaned up with renderer.
    }

    void AllocateTex()
    {
        // Destroy old one if resolution changed
        if (cutTex != null && cutTex.width != samples)
        {
            Destroy(cutTex);
            cutTex = null;
        }

        // Allocate a 1D texture in the RED channel only
        if (cutTex == null)
        {
            cutTex = new Texture2D(samples, 1, TextureFormat.R8, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,   // stops it repeating
                filterMode = FilterMode.Point,      // keeps crisp steps
                name = "Sunbeam_CutMask"
            };
        }

        // Allocate our row buffer
        if (row == null || row.Length != samples)
            row = new Color[samples];

        // Fill row with visible (cutoff=0)
        for (int i = 0; i < samples; i++)
        {
            row[i].r = 0f;     // start fully visible
            row[i].g = 0f;
            row[i].b = 0f;
        }

        // Upload to the GPU
        cutTex.SetPixels(row);
        cutTex.Apply(false, false);

        // Push to the material
        if (mat != null)
        {
            mat.SetTexture("_CutTex", cutTex);
            mat.SetFloat("_UseCutTex", 1f);
        }
    }


    void LateUpdate()
    {
        if (!sunRef || !sr || !sr.sprite) return;

        // World-space basis derived from sprite local axes (stable under rotation/scale)
        Vector2 beamDir = (invertBeamDirection ? -(Vector2)transform.TransformVector(Vector3.up)
                                               : (Vector2)transform.TransformVector(Vector3.up)).normalized;
        Vector2 perp = ((Vector2)transform.TransformVector(Vector3.right)).normalized;

        // ----- Exact sprite quad from LOCAL bounds (independent of pivot/AABB distortion) -----
        // Sprite local bounds in units (pixelsPerUnit applied)
        var lb = sr.sprite.bounds;               // local-space AABB of sprite geometry
        float leftX = lb.min.x;
        float rightX = lb.max.x;
        float topY = lb.max.y;                 // <- TRUE top edge in sprite local space

        // World top edge endpoints (for reference/debug)
        Vector3 worldTopL = transform.TransformPoint(new Vector3(leftX, topY, 0));
        Vector3 worldTopR = transform.TransformPoint(new Vector3(rightX, topY, 0));

        // Column geometry & casts
        float widthWorld = Vector2.Distance(worldTopL, worldTopR);
        int N = Mathf.Max(2, samples);
        float colWidth = widthWorld / N;

        // Tunables for robustness at glancing angles
        float preBias = Mathf.Max(0.002f * widthWorld, 0.005f);  // tiny inset toward sun
        float castDepth = widthWorld * 3.0f;                        // long enough for any tilt
        float boxHeight = Mathf.Max(0.01f * widthWorld, 0.002f);
        float boxWidth = Mathf.Max(colWidth * 1.25f, 0.0025f);
        float zDeg = transform.eulerAngles.z;

        // Estimated sprite height in world along beamDir (for mapping to UV.v)
        // Use local height then transform its vector length to world:
        float localH = lb.size.y;
        float worldH = ((Vector2)transform.TransformVector(new Vector3(0, localH, 0))).magnitude;
        worldH = Mathf.Max(worldH, 0.0001f);

        for (int i = 0; i < N; i++)
        {
            float u01 = (N == 1) ? 0.5f : i / (float)(N - 1);
            float xLoc = Mathf.Lerp(leftX, rightX, u01);

            // TRUE top point for this column (world)
            Vector2 colTop = (Vector2)transform.TransformPoint(new Vector3(xLoc, topY, 0));

            // start slightly 'before' the top, toward the sun
            Vector2 origin = colTop - beamDir * preBias;

            // Sub-sample across slice; take earliest occlusion
            float bestVisibleLen = worldH;
            int subCount = 2;
            for (int s = 0; s < subCount; s++)
            {
                // Lateral offset in world along the top edge
                float off = (subCount == 1) ? 0f : ((s - (subCount - 1) * 0.5f) * (colWidth * 0.4f));
                Vector2 o = origin + perp * off;

                RaycastHit2D hit = Physics2D.BoxCast(
                    o,
                    new Vector2(boxWidth, boxHeight),
                    zDeg,
                    beamDir,
                    castDepth,
                    occluderMask
                );

                Vector2 target = hit.collider ? hit.point : (colTop + beamDir * worldH);
                float visLen = Mathf.Clamp(Vector2.Dot(target - colTop, beamDir), 0f, worldH);
                if (visLen < bestVisibleLen) bestVisibleLen = visLen;

#if UNITY_EDITOR
            if (debugRays)
            {
                Color   c   = hit.collider ? Color.red : Color.green;
                Vector2 end = hit.collider ? hit.point : (o + beamDir * castDepth);
                Debug.DrawLine(o, end, c);
            }
#endif
            }

            // UV.v cutoff (shader discards v < cutoff); top (v=1)→bottom (v=0)
            float cutV = 1f - (bestVisibleLen / worldH);
            row[i].r = cutV; row[i].g = row[i].b = 0f;
        }

        // Optional smoothing
        if (smooth && N >= 3)
        {
            for (int i = 1; i < N - 1; i++)
            {
                float a0 = row[i - 1].r, a1 = row[i].r, a2 = row[i + 1].r;
                row[i].r = (a0 + a1 + a2) / 3f;
            }
        }

        cutTex.SetPixels(row);
        cutTex.Apply(false, false);
    }
}