using UnityEngine;
using System.Collections.Generic;

public class SunlightBeamRenderer : MonoBehaviour
{
    [Header("Beam Properties")]
    [Tooltip("Width of the sunlight beam")]
    [SerializeField] private float beamWidth = 10f;

    [Tooltip("Maximum length of beam (if no occlusion)")]
    [SerializeField] private float maxBeamLength = 20f;

    [Tooltip("Material with sunlight sprite texture")]
    [SerializeField] private Material beamMaterial;

    private Mesh beamMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    void Start()
    {
        // Create mesh components
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();

        beamMesh = new Mesh();
        beamMesh.name = "SunlightBeam";
        meshFilter.mesh = beamMesh;

        if (beamMaterial != null)
        {
            meshRenderer.material = beamMaterial;
        }

        // Generate initial static mesh
        GenerateStaticBeam();
    }

    void GenerateStaticBeam()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Simple quad: from (0,0) extending down
        float halfWidth = beamWidth / 2f;

        // Top edge (at sun)
        vertices.Add(new Vector3(-halfWidth, 0, 0));
        vertices.Add(new Vector3(halfWidth, 0, 0));

        // Bottom edge (max length)
        vertices.Add(new Vector3(halfWidth, -maxBeamLength, 0));
        vertices.Add(new Vector3(-halfWidth, -maxBeamLength, 0));

        // UVs (so texture displays correctly)
        uvs.Add(new Vector2(0, 1)); // Top-left
        uvs.Add(new Vector2(1, 1)); // Top-right
        uvs.Add(new Vector2(1, 0)); // Bottom-right
        uvs.Add(new Vector2(0, 0)); // Bottom-left

        // Two triangles for quad
        triangles.Add(0);
        triangles.Add(1);
        triangles.Add(2);

        triangles.Add(0);
        triangles.Add(2);
        triangles.Add(3);

        // Apply to mesh
        beamMesh.Clear();
        beamMesh.SetVertices(vertices);
        beamMesh.SetTriangles(triangles, 0);
        beamMesh.SetUVs(0, uvs);
        beamMesh.RecalculateBounds();
    }
}