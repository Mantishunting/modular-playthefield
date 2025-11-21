using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevelLoader : MonoBehaviour
{
    [Tooltip("Exact name of the scene to load (e.g., 'Level2')")]
    [SerializeField] private string sceneToLoad;

    public void LoadLevel()
    {
        // 1. Reset the static block costs so Level 2 doesn't start expensive
        HumanClick.ResetStaticData();

        // 2. Load the scene
        SceneManager.LoadScene(sceneToLoad);
    }
}