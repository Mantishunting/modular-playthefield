using UnityEngine;
using System.Collections.Generic;

/// Works whether attached to Sun OR to a separate "Sunbeam" object.
/// Uses Sun.cs to get the true world origin + light direction.
/// Phase 2: set rayCount = 1. Phase 3: raise to ~24–32.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SunlightBeamRendererFlexible : MonoBehaviour
{
    [Header("Sun linkage")]
    [SerializeField] private Sun sunRef;                // Drag your Sun (has Sun.cs)
    [SerializeField] private Transform originOverride;  // Optional: custom origin

    [Header("Beam Appearance")]
    [SerializeField] private float beamWidth = 10f;
    [SerializeField] private float maxBeamLength = 1500f;   // big so it's visible in large scenes
    [SerializeField] private Material beamMaterial;         // Prefer Unlit/Transparent

    [Header("Occlusion Sampling")]
    [SerializeField, Tooltip("1 for single cut; 24–32 for nice contour")]
    private int rayCount = 1;
    [SerializeField] private LayerMask occluderMask;
    [SerializeField] private float skinOffset = 0.05f;

    [Header("Sorting (MeshRenderer)")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 0;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRays = false;
    [SerializeField] private Color debugColor = Color.yellow;

    Mesh beamMesh;
    MeshFilter mf;
    MeshRenderer mr;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();

        if (!beamMesh) beamMesh = new Mesh { name = "SunlightBeam" };
        mf.sharedMesh = beamMesh;

        if (beamMaterial) mr.sharedMaterial = beamMaterial;

        // Transparent + correct draw order in 2D
        mr.sortingLayerName = sortingLayerName;
        mr.sortingOrder = sortingOrder;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        if (!sunRef) sunRef = FindObjectOfType<Sun>();
    }

    void LateUpdate()
    {
        if (!TryGetOriginAndDir(out Vector2 origin, out Vector2 dir))
        {
            beamMesh.Clear();
            return;
        }

        if (rayCount <= 1) BuildSingleRayQuad(origin, dir);
        else BuildMultiRayStrip(origin, dir);
    }

    bool TryGetOriginAndDir(out Vector2 origin, out Vector2 dir)
    {
        origin = Vector2.zero;
        dir = Vector2.down;

        if (sunRef == null)
        {
            // Fallback: use this object's transform as sun
            origin = transform.position;
            dir = -transform.up;
            return true;
        }

        origin = originOverride ? (Vector2)originOverride.position
                                : (Vector2)sunRef.transform.position;

        Vector3 d3 = sunRef.GetLightDirection();  // from Sun.cs
        if (d3 == Vector3.zero) return false;

        dir = ((Vector2)d3).normalized; // from Sun toward center/plant
        return true;
    }

    float CastLen(Vector2 start, Vector2 dir)
    {
        var hit = Physics2D.Raycast(start, dir, maxBeamLength, occluderMask);
        if (drawDebugRays)
            Debug.DrawLine(start, start + dir * (hit.collider ? hit.distance : maxBeamLength),
                           debugColor, 0f);
        return hit.collider ? hit.distance : maxBeamLength;
    }

    // --- NEW: helper to fix "skinny line" / space mismatch ---
    Vector3 Local(Vector2 worldPoint) => transform.InverseTransformPoint(worldPoint);

    void BuildSingleRayQuad(Vector2 origin, Vector2 dir)
    {
        beamMesh.Clear();

        float half = beamWidth * 0.5f;
        Vector2 perp = new Vector2(-dir.y, dir.x);

        Vector2 oL = origin + perp * (-half) + dir * skinOffset;
        Vector2 oR = origin + perp * (half) + dir * skinOffset;

        float lenL = CastLen(oL, dir);
        float lenR = CastLen(oR, dir);
        float len = Mathf.Min(lenL, lenR); // keep a clean rectangle

        var verts = new List<Vector3>(4) {
            Local(oL),
            Local(oR),
            Local(oR + dir * len),
            Local(oL + dir * len)
        };
        var uvs = new List<Vector2>(4) {
            new Vector2(0,1), new Vector2(1,1), new Vector2(1,0), new Vector2(0,0)
        };
        var tris = new List<int>(6) { 0, 1, 2, 0, 2, 3 };

        beamMesh.SetVertices(verts);
        beamMesh.SetUVs(0, uvs);
        beamMesh.SetTriangles(tris, 0);
        beamMesh.RecalculateBounds();
    }

    void BuildMultiRayStrip(Vector2 origin, Vector2 dir)
    {
        beamMesh.Clear();

        int samples = Mathf.Max(2, rayCount);
        float half = beamWidth * 0.5f;
        Vector2 perp = new Vector2(-dir.y, dir.x);

        var verts = new List<Vector3>(samples * 2);
        var uvs = new List<Vector2>(samples * 2);
        var tris = new List<int>((samples - 1) * 6);

        for (int i = 0; i < samples; i++)
        {
            float u = (samples == 1) ? 0f : i / (float)(samples - 1);
            float offset = Mathf.Lerp(-half, half, u);

            Vector2 o = origin + perp * offset + dir * skinOffset;
            float len = CastLen(o, dir);

            verts.Add(Local(o)); uvs.Add(new Vector2(u, 1)); // top
            verts.Add(Local(o + dir * len)); uvs.Add(new Vector2(u, 0)); // bottom
        }

        for (int i = 0; i < samples - 1; i++)
        {
            int i0 = i * 2, i1 = i0 + 1, i2 = i0 + 2, i3 = i0 + 3;
            tris.Add(i0); tris.Add(i2); tris.Add(i3);
            tris.Add(i0); tris.Add(i3); tris.Add(i1);
        }

        beamMesh.SetVertices(verts);
        beamMesh.SetUVs(0, uvs);
        beamMesh.SetTriangles(tris, 0);
        beamMesh.RecalculateBounds();
    }
}
