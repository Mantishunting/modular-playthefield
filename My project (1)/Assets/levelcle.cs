using UnityEngine;

public class ResetOnSceneStart : MonoBehaviour
{
    private static bool hasResetThisScene = false;

    void Awake()
    {
        // Ensure this only runs once per scene load
        if (hasResetThisScene) return;

        // Reset block counters + static HumanClick data
        HumanClick.ResetStaticData();

        // Mark as done so nothing resets twice
        hasResetThisScene = true;
    }

    void OnDestroy()
    {
        // When scene unloads, allow reset again next time
        hasResetThisScene = false;
    }
}
