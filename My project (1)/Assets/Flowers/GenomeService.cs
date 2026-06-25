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
    /// One step of growth: fill the shallowest empty slot with a random slot-part (even odds, seeded
    /// so a run is reproducible). Monotonic — never removes. Slice A drives this manually from the
    /// overlay; slice A2 wires it to pollinations.
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

        var (owner, index) = GenomeNode.FirstEmptySlot(Current.root);
        if (owner == null)
        {
            Debug.Log("[GENOME] no empty slot to fill (does GBass have any RandomPiece slots yet?).");
            return;
        }

        if (_rng == null) _rng = new System.Random(Current.seed);
        GrowthPattern part = cat.slotParts[_rng.Next(cat.slotParts.Length)];
        owner.children[index] = GenomeNode.Make(part);
        Current.generation++;
        Debug.Log($"[GENOME] evolved -> gen {Current.generation}: filled a slot with '{(part != null ? part.name : "null")}'.");
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
