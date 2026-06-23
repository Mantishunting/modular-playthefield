using UnityEngine;

/// <summary>
/// The "seed" a flower reads when it is placed to decide how to grow. ONE genome is shared by all
/// flowers (see <see cref="GenomeService"/>); it persists across scene loads and evolves on
/// pollinations. Each placed flower snapshots the field(s) it needs at placement time.
///
/// Slice 1: the only field is <see cref="routineBudget"/> — how many growth agents a single bloom
/// may run (see <see cref="RoutineBudget"/>). NO block cap, by design: growth is bounded purely by
/// agent count. Later slices add base shape, parts and colours here.
/// </summary>
[System.Serializable]
public class FlowerGenome
{
    [Tooltip("Random identity of this genome. Cosmetic for now; reserved for future shape/colour rolls.")]
    public int seed;

    [Tooltip("How many times this genome has evolved. 0 = fresh from the landing page.")]
    public int generation;

    [Tooltip("How many growth agents ONE bloom may run. Bounds fan-out AND depth with a single number.")]
    public int routineBudget;
}
