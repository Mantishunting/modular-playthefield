using UnityEngine;

/// <summary>
/// Holds the ONE shared <see cref="FlowerGenome"/> for the current run. Static, so it survives
/// scene loads for free (level -> level keeps evolving) — the same idiom <c>BlockGeneration</c> and
/// <c>HumanClick.ResetStaticData</c> use for cross-scene state. It is NOT a per-level snapshot:
/// it drifts mid-run on pollinations and is only wiped when the player returns to the landing page
/// (<see cref="GenomeResetOnLanding"/>).
///
/// Evolution counts pollinations HERE, not in <see cref="BeeVisitTracker"/> — that tracker resets
/// every scene, so its total can't survive a level change. We keep our own cumulative count.
/// </summary>
public static class GenomeService
{
    // Tuning. Kept here for slice 1; can move onto an inspector asset later. All monotonic.
    public const int StartBudget = 3;
    public const int PollinationsPerEvolution = 10;
    public const int BudgetIncrement = 1;

    /// <summary>The live genome every newly placed flower reads. Created lazily.</summary>
    public static FlowerGenome Current { get; private set; }

    /// <summary>Cumulative pollinations since the last landing-page reset (drives evolution).</summary>
    public static int Pollinations { get; private set; }

    /// <summary>
    /// Subscribe once at app start. The event is static and the handler is static, so this single
    /// subscription persists for the whole session regardless of how many scenes load.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // Guard against double-subscription if the runtime re-invokes (domain reload settings vary).
        BeeVisitTracker.OnVisitRegistered -= OnPollination;
        BeeVisitTracker.OnVisitRegistered += OnPollination;
        EnsureExists();
    }

    /// <summary>Make sure a genome exists (cold start straight into a level, no landing visited).</summary>
    public static void EnsureExists()
    {
        if (Current == null) RollFresh();
    }

    /// <summary>
    /// Force the live budget (debug/tuning, e.g. the / overlay). Affects only flowers placed AFTER
    /// this — existing blooms already snapshotted their RoutineBudget. Clamped to a sane minimum.
    /// </summary>
    public static void SetRoutineBudget(int value)
    {
        EnsureExists();
        Current.routineBudget = Mathf.Max(1, value);
    }

    /// <summary>Wipe to a brand-new genome at generation 0. Called when returning to the landing page.</summary>
    public static void ResetForNewGame()
    {
        RollFresh();
        Debug.Log("[GENOME] reset for new game (landing page) -> fresh genome, budget " + Current.routineBudget);
    }

    private static void RollFresh()
    {
        Current = new FlowerGenome
        {
            seed = Random.Range(int.MinValue, int.MaxValue),
            generation = 0,
            routineBudget = StartBudget
        };
        Pollinations = 0;
    }

    // BeeVisitTracker.OnVisitRegistered passes the per-scene running total; we only treat each
    // invocation as one pollination pulse and keep our own cross-scene count.
    private static void OnPollination(int _perSceneTotal)
    {
        EnsureExists();
        Pollinations++;
        if (Pollinations % PollinationsPerEvolution == 0)
            Evolve();
    }

    private static void Evolve()
    {
        Current.routineBudget += BudgetIncrement;   // never shrink
        Current.generation += 1;
        Debug.Log($"[GENOME] evolved at {Pollinations} pollinations -> gen {Current.generation}, budget {Current.routineBudget}");
    }
}
