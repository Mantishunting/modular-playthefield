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
    public bool showConnectors = false;
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

    [Header("Diagnostics (U)")]
    [Tooltip("Show WHY each side is open/rejected, plus each block's real orientation. Hotkey: U")]
    public bool showDiagnostics = false;
    public KeyCode diagnosticsKey = KeyCode.U;
    [Tooltip("Length of the orientation ticks (block's local up = yellow, right = cyan).")]
    public float axisTickLength = 0.4f;

    [Header("Performance")]
    [Tooltip("Recompute the heavier overlays (block scan + open spots) every N frames. The cursor dot still updates every frame.")]
    [Range(1, 20)] public int recomputeInterval = 3;

    // Built-in unlit material used for GL drawing (created on demand).
    private Material lineMaterial;

    // Per-frame caches (filled in Update, drawn by every camera).
    private HumanClick[] blocks;
    private readonly List<Vector3> openSpots = new List<Vector3>();
    private readonly List<Vector3> scratch = new List<Vector3>();
    private readonly HashSet<Vector2Int> seen = new HashSet<Vector2Int>();
    private bool cursorDotValid;
    private Vector3 cursorDotPos;

    // Diagnostics caches (slot positions grouped by rejection reason + orientation ticks).
    private readonly List<Vector3> diagOpen = new List<Vector3>();
    private readonly List<Vector3> diagChild = new List<Vector3>();
    private readonly List<Vector3> diagOccupied = new List<Vector3>();
    private readonly List<Vector3> diagHazard = new List<Vector3>();
    private readonly List<Vector3> diagInvalid = new List<Vector3>();
    private readonly List<Vector3> diagUpLines = new List<Vector3>();    // vertex pairs
    private readonly List<Vector3> diagRightLines = new List<Vector3>(); // vertex pairs
    private readonly List<HumanClick.SlotDiag> diagScratch = new List<HumanClick.SlotDiag>();

    private Camera cam;

    void Start()
    {
        // Enforce all debug visual overlays to be off by default when play mode starts
        showConnectors = false;
        showClickNodes = false;
        showOpenSpots = false;
        showCursorDot = false;
        showDiagnostics = false;
    }

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
        if (Input.GetKeyDown(diagnosticsKey)) showDiagnostics = !showDiagnostics;

        BlockType selected = (BlockTypeManager.Instance != null)
            ? BlockTypeManager.Instance.GetSelectedType()
            : null;

        // Throttle the heavy work (scanning every block + occupancy checks) to every
        // few frames; the dots simply persist between recomputes.
        if (blocks == null || Time.frameCount % Mathf.Max(1, recomputeInterval) == 0)
        {
            blocks = FindObjectsOfType<HumanClick>();
            ComputeOpenSpots(selected);
            ComputeDiagnostics(selected);
        }

        // Cursor dot stays responsive every frame (cheap — only blocks near the
        // mouse do real work, thanks to the distance early-out).
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

    private void ComputeDiagnostics(BlockType selected)
    {
        diagOpen.Clear(); diagChild.Clear(); diagOccupied.Clear();
        diagHazard.Clear(); diagInvalid.Clear();
        diagUpLines.Clear(); diagRightLines.Clear();
        if (!showDiagnostics || blocks == null) return;

        for (int i = 0; i < blocks.Length; i++)
        {
            HumanClick b = blocks[i];
            diagScratch.Clear();
            b.GetPlacementDiagnostics(selected, diagScratch, out Vector3 up, out Vector3 right);

            Vector3 c = b.transform.position;
            diagUpLines.Add(c); diagUpLines.Add(c + up.normalized * axisTickLength);
            diagRightLines.Add(c); diagRightLines.Add(c + right.normalized * axisTickLength);

            for (int j = 0; j < diagScratch.Count; j++)
            {
                HumanClick.SlotDiag d = diagScratch[j];
                switch (d.status)
                {
                    case HumanClick.SlotStatus.Open: diagOpen.Add(d.pos); break;
                    case HumanClick.SlotStatus.HasChild: diagChild.Add(d.pos); break;
                    case HumanClick.SlotStatus.Occupied: diagOccupied.Add(d.pos); break;
                    case HumanClick.SlotStatus.Hazard: diagHazard.Add(d.pos); break;
                    case HumanClick.SlotStatus.Invalid: diagInvalid.Add(d.pos); break;
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
        bool anyLines = showConnectors || showClickNodes || showDiagnostics;
        bool anyDots = (showOpenSpots && openSpots.Count > 0) || (showCursorDot && cursorDotValid)
                       || showDiagnostics;
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

            if (showDiagnostics)
            {
                // Orientation ticks: local up = yellow, local right = cyan.
                GL.Color(Color.yellow);
                for (int i = 0; i + 1 < diagUpLines.Count; i += 2)
                { GL.Vertex(diagUpLines[i]); GL.Vertex(diagUpLines[i + 1]); }
                GL.Color(Color.cyan);
                for (int i = 0; i + 1 < diagRightLines.Count; i += 2)
                { GL.Vertex(diagRightLines[i]); GL.Vertex(diagRightLines[i + 1]); }
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

            if (showDiagnostics)
            {
                // Color-coded by WHY the side is open/rejected.
                GL.Color(Color.green);    for (int i = 0; i < diagOpen.Count; i++)     DotGL(diagOpen[i], dotHalfSize);
                GL.Color(Color.blue);     for (int i = 0; i < diagChild.Count; i++)    DotGL(diagChild[i], dotHalfSize);
                GL.Color(Color.red);      for (int i = 0; i < diagOccupied.Count; i++) DotGL(diagOccupied[i], dotHalfSize);
                GL.Color(new Color(1f, 0.5f, 0f)); for (int i = 0; i < diagHazard.Count; i++) DotGL(diagHazard[i], dotHalfSize);
                GL.Color(Color.magenta);  for (int i = 0; i < diagInvalid.Count; i++)  DotGL(diagInvalid[i], dotHalfSize);
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
