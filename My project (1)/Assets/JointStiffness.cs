using UnityEngine;

/// <summary>
/// Global "plant" controller for the FixedJoint2D connections between blocks.
///
/// A plant is modelled with the two STABLE regions of a FixedJoint2D:
///   * a rigid WELD (frequency 0, a hard constraint) for the trunk / base, so
///     branches stick out and support the leaves, and
///   * LOW-frequency soft springs toward the tips, so heavy / long branches bend
///     under their own weight and droop. (A spring deflects in proportion to the
///     torque on it, so heavier sub-trees sag more and long chains accumulate
///     droop -- the bending is automatic.)
///
/// The unstable high-frequency middle of a FixedJoint2D is never used, so this
/// can't blow up. Stiffness is a function of a joint's DEPTH from the root and
/// the LOAD (mass) hanging below it; it is computed only when the tree changes
/// or this slider moves, never per frame.
/// </summary>
[DefaultExecutionOrder(-100)]
public class JointStiffness : MonoBehaviour
{
    public static JointStiffness Instance { get; private set; }

    [Header("Overall Plant Stiffness")]
    [Tooltip("0 = floppy / droopy plant. 1 = firm, stands tall. " +
             "Raises how many generations near the root stay rigid and firms up the springs.")]
    [Range(0f, 1f)]
    [SerializeField] private float stiffness = 0.6f;

    [Header("Plant Shape (advanced)")]
    [Tooltip("Generations from a root that are a rigid weld at full stiffness. " +
             "Scaled down by the stiffness slider. This is the firm 'trunk'.")]
    [SerializeField] private float maxRigidGenerations = 4f;

    [Tooltip("Extra welded generations granted per unit of supported mass, so a joint " +
             "carrying a lot of weight stays firm enough to hold it up before bending.")]
    [SerializeField] private float loadReinforcement = 1.5f;

    [Tooltip("Spring frequency (Hz) of the first bending generation past the rigid trunk. Firmer.")]
    [SerializeField] private float baseFrequency = 12f;

    [Tooltip("Spring frequency (Hz) at the deepest tips. Softest -- tips droop the most.")]
    [SerializeField] private float tipFrequency = 5f;

    [Tooltip("How many generations it takes to fall off from baseFrequency to tipFrequency.")]
    [SerializeField] private float softFalloff = 6f;

    [Tooltip("Joint shock absorption. 0 = bouncy, 1 = no overshoot. High keeps sway calm.")]
    [Range(0f, 1f)]
    [SerializeField] private float dampingRatio = 0.85f;

    [Tooltip("Safety: spring frequency can never exceed stabilityFactor / physicsStep, " +
             "so it stays in the numerically stable range. 0.5 is safe at Unity's 50Hz.")]
    [Range(0.1f, 0.6f)]
    [SerializeField] private float stabilityFactor = 0.5f;

    /// <summary>Bumped whenever the tuning changes, so connectors recompute live.</summary>
    public int SettingsVersion { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); }
    }

    void OnValidate()
    {
        // Live tuning: connectors watch this and recompute when it changes.
        SettingsVersion++;
    }

    public float GetDampingRatio() => dampingRatio;

    /// <summary>
    /// Target FixedJoint2D.frequency for a joint at the given depth carrying the given
    /// load (total mass below it). Returns 0 for a rigid weld.
    /// </summary>
    public float GetFrequency(int depth, float load)
    {
        // Rigid trunk: welded near the root, extended where lots of mass must be supported.
        float rigid = maxRigidGenerations * stiffness + loadReinforcement * load;
        if (depth <= rigid) return 0f;

        // Soft bending band: firm just past the trunk, softest at the tips.
        float remaining = depth - rigid;
        float t = Mathf.Clamp01((remaining - 1f) / Mathf.Max(1f, softFalloff));
        float freq = Mathf.Lerp(baseFrequency, tipFrequency, t);

        // The slider also firms up / loosens the whole spring band.
        freq *= Mathf.Lerp(0.6f, 1.2f, stiffness);

        // Never exceed the numerically stable ceiling for the current physics step.
        float cap = stabilityFactor / Mathf.Max(0.0001f, Time.fixedDeltaTime);
        return Mathf.Min(freq, cap);
    }
}
