using UnityEngine;

/// <summary>
/// Holds the ONE shared <see cref="FlowerGenome"/> for the current run. Static, so it survives scene
/// loads for free (level -> level keeps its tree) — the same idiom <c>BlockGeneration</c> uses. It is
/// only wiped when the player returns to the landing page (<see cref="GenomeResetOnLanding"/>).
///
/// The genome is a recursive part-tree (see <see cref="GenomeNode"/>). <see cref="Evolve"/> fills the
/// shallowest empty slot with a random part from the <see cref="FlowerPieceCatalogue"/>. There is NO
/// numeric budget — the finite tree bounds growth.
/// </summary>
public static class GenomeService
{
    /// <summary>The live genome every newly placed flower reads (deep-copied at placement).</summary>
    public static FlowerGenome Current { get; private set; }

    /// <summary>Cumulative pollinations since the last landing-page reset.</summary>
    public static int Pollinations { get; private set; }

    /// <summary>How many pollinations between automatic evolutions (one filled slot each). Tunable.</summary>
    public const int PollinationsPerEvolution = 10;

    private static FlowerPieceCatalogue _catalogue;
    private static System.Random _rng;

    /// <summary>The fixed part set. Loaded lazily from Resources; retried until found.</summary>
    public static FlowerPieceCatalogue Catalogue
    {
        get
        {
            if (_catalogue == null)
                _catalogue = Resources.Load<FlowerPieceCatalogue>("FlowerPieceCatalogue");
            return _catalogue;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // Static event + static handler -> one subscription persists for the whole session.
        BeeVisitTracker.OnVisitRegistered -= OnPollination;
        BeeVisitTracker.OnVisitRegistered += OnPollination;
        EnsureExists();
    }

    /// <summary>Ensure a genome exists. Also self-heals the root part if the catalogue arrived late.</summary>
    public static void EnsureExists()
    {
        if (Current == null || Current.root == null) { RollFresh(); return; }
        if (Current.root.part == null && Catalogue != null && Catalogue.root != null)
            Current.root = GenomeNode.Make(Catalogue.root);   // catalogue loaded after the first roll
    }

    /// <summary>Wipe to a brand-new genome (bare GBass root). Called when returning to the landing page.</summary>
    public static void ResetForNewGame()
    {
        RollFresh();
        Debug.Log("[GENOME] reset for new game (landing page) -> fresh GBass root.");
    }

    private static void RollFresh()
    {
        int seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        _rng = new System.Random(seed);
        Current = new FlowerGenome
        {
            seed = seed,
            generation = 0,
            root = GenomeNode.Make(Catalogue != null ? Catalogue.root : null)
        };
        Pollinations = 0;
    }

    /// <summary>
    /// One evolution: grant the genome `pointsPerEvolution` points and spend each (seeded-random, by the
    /// catalogue weights) on ONE monotonic change to a specific part — add a part, +G, or +colour. Never
    /// removes. Driven by pollinations (A2) and the overlay's Evolve button.
    /// </summary>
    public static void Evolve()
    {
        EnsureExists();
        var cat = Catalogue;
        if (cat == null || cat.slotParts == null || cat.slotParts.Length == 0)
        {
            Debug.LogWarning("[GENOME] no FlowerPieceCatalogue (or empty slotParts) in a Resources folder — cannot evolve.");
            return;
        }

        if (_rng == null) _rng = new System.Random(Current.seed);

        int points = Mathf.Max(1, cat.pointsPerEvolution);
        for (int p = 0; p < points; p++) SpendPoint(cat);

        Current.generation++;
        Debug.Log($"[GENOME] evolved -> gen {Current.generation} (spent {points} points).");
    }

    // Spend one point on a weighted-random monotonic change. If the chosen action can't apply (e.g. no
    // empty slot to add a part), fall through to the next so a point is never wasted.
    private static void SpendPoint(FlowerPieceCatalogue cat)
    {
        float wPart = Mathf.Max(0f, cat.weightAddPart);
        float wG = Mathf.Max(0f, cat.weightAddG);
        float wColour = Mathf.Max(0f, cat.weightAddColour);
        float total = wPart + wG + wColour;
        if (total <= 0f) { AddPart(cat); return; }

        float r = (float)_rng.NextDouble() * total;
        if (r < wPart) { if (AddPart(cat)) return; r = wPart; } // fall through if no slot
        if (r < wPart + wG) { AddG(); return; }
        AddColour();
    }

    private static bool AddPart(FlowerPieceCatalogue cat)
    {
        var (owner, index) = GenomeNode.FirstEmptySlot(Current.root);
        if (owner == null) return false; // tree full -> caller falls through to G/colour
        GrowthPattern part = cat.slotParts[_rng.Next(cat.slotParts.Length)];
        owner.children[index] = GenomeNode.Make(part);
        return true;
    }

    private static void AddG() { var n = PickNode(); if (n != null) n.g++; }
    private static void AddColour() { var n = PickNode(); if (n != null) n.colour++; }

    private static GenomeNode PickNode()
    {
        var all = new System.Collections.Generic.List<GenomeNode>();
        GenomeNode.Collect(Current.root, all);
        return all.Count == 0 ? null : all[_rng.Next(all.Count)];
    }

    /// <summary>
    /// Debug/testing: add to G on EVERY node of the live tree (clamped at 0). G drives GMove length
    /// (round(base + G*gCoeff)), so bump this then place a flower to see G-steps grow. Real per-part G
    /// evolution comes in slice B.
    /// </summary>
    public static void AdjustAllG(int delta)
    {
        EnsureExists();
        ApplyG(Current.root, delta);
    }

    private static void ApplyG(GenomeNode n, int delta)
    {
        if (n == null) return;
        n.g = Mathf.Max(0, n.g + delta);
        if (n.children != null)
            foreach (var c in n.children) ApplyG(c, delta);
    }

    /// <summary>A deep copy of the current tree root for a freshly placed bloom (so later evolution can't mutate it).</summary>
    public static GenomeNode SnapshotRoot()
    {
        EnsureExists();
        return Current.root.Clone();
    }

    // Every Nth pollination grows the shared genome by one slot. The overlay's Evolve button still
    // works alongside this for manual testing.
    private static void OnPollination(int _perSceneTotal)
    {
        Pollinations++;
        if (Pollinations % PollinationsPerEvolution == 0)
            Evolve();
    }
}
