using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

/// <summary>
/// Debug tool: visualises the plant's hidden structure. Four independent overlays,
/// each with its own checkbox and hotkey:
///
///   • JOINT CONNECTORS (J) — a line from every block to its parent (the physics
///     joints that hold the plant together).
///   • CLICKABLE NODES (N) — the circles around every block: inner = the block's
///     body (right-click delete / occupancy), outer = the larger place range.
///   • OPEN BUILD SPOTS (K) — a dot at EVERY empty cell where you could click to
///     place the currently selected block (respects the real placement rules).
///   • CURSOR SNAP DOT (M) — a single dot that follows the mouse and snaps to the
///     cell where your next block would actually land.
///
/// Shows in BOTH the Game view (drawn through the render pipeline, so it works in
/// URP and in builds) and the editor Scene view (gizmos).
///
/// HOW TO USE:
///   1. Make an empty GameObject (right-click in Hierarchy → Create Empty),
///      name it e.g. "JointDebug".
///   2. Add this component to it.
///   3. Tick the toggles in the Inspector, or press the hotkeys while playing.
///
/// The structure only exists while the game is running, so the overlay appears in
/// Play mode. "Open build spots" and "cursor snap dot" use the currently selected
/// block type (Wood / Leaf / Flower), so what they show changes with your selection.
/// </summary>
public class JointConnectorVisualizer : MonoBehaviour
{
    [Header("Joint Connectors (J)")]
    public bool showConnectors = true;
    public KeyCode connectorsKey = KeyCode.J;
    public Color connectorColor = new Color(0.2f, 1f, 1f, 0.9f); // cyan

    [Header("Clickable Nodes (N)")]
    public bool showClickNodes = false;
    public KeyCode nodesKey = KeyCode.N;
    [Tooltip("Inner circle = the block's own body (delete / occupancy).")]
    public Color bodyColor = new Color(1f, 0.85f, 0.2f, 0.9f); // amber
    [Tooltip("Outer circle = the larger range where clicking places a new block.")]
    public Color clickRangeColor = new Color(1f, 0.4f, 0.2f, 0.6f); // orange
    [Range(8, 64)] public int circleSegments = 28;

    [Header("Open Build Spots (K)")]
    [Tooltip("A dot at every empty cell where you could place the selected block.")]
    public bool showOpenSpots = false;
    public KeyCode openSpotsKey = KeyCode.K;
    public Color openSpotColor = new Color(0.3f, 1f, 0.3f, 0.9f); // green

    [Header("Cursor Snap Dot (M)")]
    [Tooltip("A dot that follows the mouse and snaps to where the next block lands.")]
    public bool showCursorDot = false;
    public KeyCode cursorDotKey = KeyCode.M;
    public Color cursorDotColor = new Color(1f, 1f, 1f, 1f); // white

    [Header("Dot Size")]
    [Tooltip("Half-size of the square dots, in world units.")]
    public float dotHalfSize = 0.08f;

    // Built-in unlit material used for GL drawing (created on demand).
    private Material lineMaterial;

    // Per-frame caches (filled in Update, drawn by every camera).
    private HumanClick[] blocks;
    private readonly List<Vector3> openSpots = new List<Vector3>();
    private readonly List<Vector3> scratch = new List<Vector3>();
    private readonly HashSet<Vector2Int> seen = new HashSet<Vector2Int>();
    private bool cursorDotValid;
    private Vector3 cursorDotPos;

    private Camera cam;

    void OnEnable()
    {
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    void Update()
    {
        // Hotkeys
        if (Input.GetKeyDown(connectorsKey)) showConnectors = !showConnectors;
        if (Input.GetKeyDown(nodesKey)) showClickNodes = !showClickNodes;
        if (Input.GetKeyDown(openSpotsKey)) showOpenSpots = !showOpenSpots;
        if (Input.GetKeyDown(cursorDotKey)) showCursorDot = !showCursorDot;

        // Cache the block list once per frame (not per camera).
        blocks = FindObjectsOfType<HumanClick>();

        BlockType selected = (BlockTypeManager.Instance != null)
            ? BlockTypeManager.Instance.GetSelectedType()
            : null;

        ComputeOpenSpots(selected);
        ComputeCursorDot(selected);
    }

    private void ComputeOpenSpots(BlockType selected)
    {
        openSpots.Clear();
        if (!showOpenSpots || selected == null || blocks == null) return;

        seen.Clear();
        for (int i = 0; i < blocks.Length; i++)
        {
            scratch.Clear();
            blocks[i].GetOpenPlacementPositions(selected, scratch);

            // De-duplicate cells that two neighbouring blocks both offer.
            for (int j = 0; j < scratch.Count; j++)
            {
                Vector3 p = scratch[j];
                Vector2Int key = new Vector2Int(
                    Mathf.RoundToInt(p.x * 4f),
                    Mathf.RoundToInt(p.y * 4f));
                if (seen.Add(key))
                    openSpots.Add(p);
            }
        }
    }

    private void ComputeCursorDot(BlockType selected)
    {
        cursorDotValid = false;
        if (!showCursorDot || selected == null || blocks == null) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 m = cam.ScreenToWorldPoint(Input.mousePosition);
        m.z = 0f;

        // Pick the snap offered by the block nearest the cursor.
        float best = float.MaxValue;
        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i].TryGetPlacementPosition(m, selected, out Vector3 pos))
            {
                float d = Vector3.Distance(blocks[i].transform.position, m);
                if (d < best)
                {
                    best = d;
                    cursorDotPos = pos;
                    cursorDotValid = true;
                }
            }
        }
    }

    void EnsureMaterial()
    {
        if (lineMaterial != null) return;
        lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        lineMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
    }

    // --- Game view (URP / SRP path) ---
    private void OnEndCameraRendering(ScriptableRenderContext context, Camera renderingCam)
    {
        DrawGL();
    }

    // --- Game view (built-in pipeline fallback) ---
    private void OnRenderObject()
    {
        if (GraphicsSettings.currentRenderPipeline == null)
            DrawGL();
    }

    private void DrawGL()
    {
        if (blocks == null) return;
        bool anyLines = showConnectors || showClickNodes;
        bool anyDots = (showOpenSpots && openSpots.Count > 0) || (showCursorDot && cursorDotValid);
        if (!anyLines && !anyDots) return;

        EnsureMaterial();
        lineMaterial.SetPass(0);
        GL.PushMatrix();

        // ----- LINES pass (connectors + node circles) -----
        if (anyLines)
        {
            GL.Begin(GL.LINES);

            if (showConnectors)
            {
                GL.Color(connectorColor);
                for (int i = 0; i < blocks.Length; i++)
                {
                    HumanClick parent = blocks[i].GetParent();
                    if (parent == null) continue;
                    GL.Vertex(blocks[i].transform.position);
                    GL.Vertex(parent.transform.position);
                }
            }

            if (showClickNodes)
            {
                for (int i = 0; i < blocks.Length; i++)
                {
                    Vector3 c = blocks[i].transform.position;
                    CircleGL(c, blocks[i].GetClickRadius(), clickRangeColor);
                    CircleGL(c, blocks[i].GetBodyRadius(), bodyColor);
                }
            }

            GL.End();
        }

        // ----- QUADS pass (dots) -----
        if (anyDots)
        {
            GL.Begin(GL.QUADS);

            if (showOpenSpots)
            {
                GL.Color(openSpotColor);
                for (int i = 0; i < openSpots.Count; i++)
                    DotGL(openSpots[i], dotHalfSize);
            }

            if (showCursorDot && cursorDotValid)
            {
                GL.Color(cursorDotColor);
                DotGL(cursorDotPos, dotHalfSize * 1.4f);
            }

            GL.End();
        }

        GL.PopMatrix();
    }

    // Circle as a loop of line segments (call between GL.Begin/End LINES).
    private void CircleGL(Vector3 center, float radius, Color color)
    {
        GL.Color(color);
        float step = Mathf.PI * 2f / circleSegments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int s = 1; s <= circleSegments; s++)
        {
            float a = step * s;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
            GL.Vertex(prev);
            GL.Vertex(next);
            prev = next;
        }
    }

    // Filled square dot (call between GL.Begin/End QUADS).
    private void DotGL(Vector3 c, float half)
    {
        GL.Vertex(c + new Vector3(-half, -half, 0f));
        GL.Vertex(c + new Vector3(-half, half, 0f));
        GL.Vertex(c + new Vector3(half, half, 0f));
        GL.Vertex(c + new Vector3(half, -half, 0f));
    }

    // --- Editor Scene view ---
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || blocks == null) return;

        if (showConnectors)
        {
            Gizmos.color = connectorColor;
            for (int i = 0; i < blocks.Length; i++)
            {
                HumanClick parent = blocks[i].GetParent();
                if (parent == null) continue;
                Gizmos.DrawLine(blocks[i].transform.position, parent.transform.position);
            }
        }

        if (showClickNodes)
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                Vector3 c = blocks[i].transform.position;
                Gizmos.color = clickRangeColor;
                Gizmos.DrawWireSphere(c, blocks[i].GetClickRadius());
                Gizmos.color = bodyColor;
                Gizmos.DrawWireSphere(c, blocks[i].GetBodyRadius());
            }
        }

        if (showOpenSpots)
        {
            Gizmos.color = openSpotColor;
            for (int i = 0; i < openSpots.Count; i++)
                Gizmos.DrawSphere(openSpots[i], dotHalfSize);
        }

        if (showCursorDot && cursorDotValid)
        {
            Gizmos.color = cursorDotColor;
            Gizmos.DrawSphere(cursorDotPos, dotHalfSize * 1.4f);
        }
    }

    void OnDestroy()
    {
        if (lineMaterial != null) DestroyImmediate(lineMaterial);
    }
}
