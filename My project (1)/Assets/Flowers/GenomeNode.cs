using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// One node in the flower-genome tree. A node = a part (a <see cref="GrowthPattern"/>) plus its
/// per-instance dials (G growth, C colour) and its filled child slots. <see cref="children"/>.Length
/// equals the part's slot count (number of RandomPiece step-iterations); a null entry is an empty,
/// not-yet-evolved slot. Runtime-only — the live genome lives in <see cref="GenomeService"/> static
/// state, not in any asset.
/// </summary>
[System.Serializable]
public class GenomeNode
{
    public GrowthPattern part;     // which flower part this node grows
    public int g;                  // growth dial (used in slice B)
    public float colourHue;        // C: shader hue (placeholder, applied in slice C)
    public GenomeNode[] children;  // one entry per slot (RandomPiece iteration); null = empty

    /// <summary>Make a fresh node for a part, with its slot array sized and empty.</summary>
    public static GenomeNode Make(GrowthPattern part)
    {
        return new GenomeNode
        {
            part = part,
            g = 0,
            colourHue = 0f,
            children = new GenomeNode[SlotCount(part)]
        };
    }

    /// <summary>
    /// Slot count for a part = every RandomPiece slot reachable through its pattern, INCLUDING those
    /// nested inside its SpawnPiece structural sub-agents (recursively). A SpawnPiece extends the same
    /// part's body and carries its slots; a RandomPiece is one slot. Depth-guarded against authoring loops.
    /// </summary>
    public static int SlotCount(GrowthPattern part) => SlotCount(part, 0);

    private static int SlotCount(GrowthPattern part, int depth)
    {
        if (part == null || part.steps == null || depth > 16) return 0;
        int n = 0;
        foreach (var s in part.steps)
        {
            int reps = Mathf.Max(1, s.repeats);
            if (s.type == StepType.RandomPiece)
                n += reps;
            else if (s.type == StepType.SpawnPiece && s.spawnPattern != null)
                n += reps * SlotCount(s.spawnPattern, depth + 1);
        }
        return n;
    }

    /// <summary>Deep copy, so an already-placed bloom never mutates when the shared genome evolves.</summary>
    public GenomeNode Clone()
    {
        var c = new GenomeNode { part = part, g = g, colourHue = colourHue };
        if (children != null)
        {
            c.children = new GenomeNode[children.Length];
            for (int i = 0; i < children.Length; i++)
                c.children[i] = children[i]?.Clone();
        }
        return c;
    }

    /// <summary>
    /// Breadth-first search for the shallowest empty slot (fill a node's own slots before descending
    /// into its filled children). Returns the owning node + slot index, or (null, -1) if none free.
    /// </summary>
    public static (GenomeNode owner, int index) FirstEmptySlot(GenomeNode root)
    {
        if (root == null) return (null, -1);
        var q = new Queue<GenomeNode>();
        q.Enqueue(root);
        while (q.Count > 0)
        {
            var n = q.Dequeue();
            if (n.children == null) continue;
            for (int i = 0; i < n.children.Length; i++)
                if (n.children[i] == null) return (n, i);   // shallowest empty slot
            for (int i = 0; i < n.children.Length; i++)
                q.Enqueue(n.children[i]);                   // all filled here -> descend
        }
        return (null, -1);
    }
}
