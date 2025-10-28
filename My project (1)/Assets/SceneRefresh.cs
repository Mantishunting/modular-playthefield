using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneRefresh : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyCode refreshKey = KeyCode.X;
    [SerializeField] private bool showDebugMessage = true;
    
    void Update()
    {
        if (Input.GetKeyDown(refreshKey))
        {
            RefreshScene();
        }
    }
    
    void RefreshScene()
    {
        if (showDebugMessage)
        {
            Debug.Log("Refreshing scene...");
        }
        
        // Reload the current active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
