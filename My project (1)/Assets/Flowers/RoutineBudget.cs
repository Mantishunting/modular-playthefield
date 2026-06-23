using UnityEngine;

/// <summary>
/// The live, per-bloom agent allowance. Attached to a bloom ROOT block by
/// <see cref="FlowerBloomStarter"/> with <see cref="capacity"/> snapshotted from the genome. Every
/// <see cref="ChainPatternAgent"/> in the bloom climbs to this and <see cref="TryClaim"/>s a slot
/// the moment it would run.
///
/// CRITICAL (rollback lesson): this object ONLY ever refuses. It never creates, starts or enables an
/// agent. A bloom with no RoutineBudget on its root (e.g. the title-screen BEAN chains) is never
/// gated at all.
/// </summary>
[DisallowMultipleComponent]
public class RoutineBudget : MonoBehaviour
{
    [Tooltip("Max agents this bloom may run. Snapshotted from FlowerGenome.routineBudget at placement.")]
    public int capacity;

    [SerializeField] private int used;

    public int Used => used;

    /// <summary>Claim one agent slot. True if a slot was free (and consumes it); false if the bloom is full.</summary>
    public bool TryClaim()
    {
        if (used >= capacity) return false;
        used++;
        return true;
    }
}
