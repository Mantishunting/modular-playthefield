using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement; // Essential for scene operations

public class SceneNavigator : MonoBehaviour
{
    // --- Auto-Reset Settings ---
    [Header("Auto-Reset Settings")]
    [Tooltip("Time in seconds before the scene automatically resets.")]
    public float timeBeforeReset = 10f;

    private string currentSceneName;
    private Coroutine currentResetCoroutine;

    void Start()
    {
        // Get the name of the current scene to reload.
        currentSceneName = SceneManager.GetActiveScene().name;

        // Start the automatic reset timer.
        currentResetCoroutine = StartCoroutine(ResetTimer());
    }

    void Update()
    {
        // --- Hotkey for Current Scene Reset (Key 1) ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Hotkey '1' pressed. Instantly restarting current scene: " + currentSceneName);

            // Stop the automatic timer before manually resetting.
            if (currentResetCoroutine != null)
            {
                StopCoroutine(currentResetCoroutine);
            }

            // Load the current scene.
            LoadSceneByName(currentSceneName);
        }

        // --- Hotkeys for Level/Scene Jumps (Keys 2 through 6) ---
        CheckForLevelJumpHotkeys();
    }

    /// <summary>
    /// Checks for keys 2, 3, 4, 5, and 6 to load scenes by their Build Index.
    /// </summary>
    void CheckForLevelJumpHotkeys()
    {
        // Check for the keys and determine the target scene index
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            LoadSceneByIndex(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            LoadSceneByIndex(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            LoadSceneByIndex(4);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            LoadSceneByIndex(5);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            LoadSceneByIndex(6);
        }
    }

    IEnumerator ResetTimer()
    {
        Debug.Log("Scene auto-reset timer started. Will reset in " + timeBeforeReset + " seconds.");
        yield return new WaitForSeconds(timeBeforeReset);

        // Execute the reset after the delay.
        LoadSceneByName(currentSceneName);
    }

    // --- Scene Loading Helper Functions ---

    void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    void LoadSceneByIndex(int buildIndex)
    {
        // You may want to add safety checks here, e.g., if buildIndex is valid.
        Debug.Log("Loading Scene at Build Index: " + buildIndex);
        SceneManager.LoadScene(buildIndex);
    }
}