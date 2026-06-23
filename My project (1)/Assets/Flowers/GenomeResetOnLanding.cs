using UnityEngine;

/// <summary>
/// Drop this on any object in the LANDING / title scene (e.g. StartScene). When that scene loads it
/// wipes the shared genome back to a fresh generation-0 seed. Resetting by scene presence — rather
/// than matching a scene name in <see cref="GenomeService"/> — catches EVERY path back to the title
/// (Reseter idle/double-press, a menu button, etc.).
/// </summary>
[DisallowMultipleComponent]
public class GenomeResetOnLanding : MonoBehaviour
{
    private void Awake()
    {
        GenomeService.ResetForNewGame();
    }
}
