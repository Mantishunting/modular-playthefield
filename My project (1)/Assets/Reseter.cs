using UnityEngine;
using UnityEngine.SceneManagement;

public class Reseter : MonoBehaviour
{
    [Header("Scene To Load")]
    [Tooltip("Exact name of the landing/title screen scene.")]
    public string landingSceneName = "LandingScreen";

    [Header("Idle Settings")]
    [Tooltip("Seconds of no player input before return.")]
    public float idleTimeoutSeconds = 300f; // 5 minutes

    [Header("Double-Press Settings")]
    [Tooltip("Max time between two presses for a 'double press'.")]
    public float doublePressWindow = 0.5f;

    private float lastInputTime;
    private float lastBackspaceTime = -10f;
    private float lastQTime = -10f;

    void Start()
    {
        lastInputTime = Time.time;
    }

    void Update()
    {
        // Detect ANY meaningful input
        if (AnyUserInput())
        {
            lastInputTime = Time.time;
        }

        // Idle timeout check
        if (Time.time - lastInputTime >= idleTimeoutSeconds)
        {
            LoadLanding();
        }

        // Backspace double-press
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (Time.time - lastBackspaceTime <= doublePressWindow)
            {
                LoadLanding();
            }
            lastBackspaceTime = Time.time;
        }

        // Q double-press
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (Time.time - lastQTime <= doublePressWindow)
            {
                LoadLanding();
            }
            lastQTime = Time.time;
        }
    }

    private bool AnyUserInput()
    {
        // Keyboard
        if (Input.anyKeyDown) return true;

        // Mouse
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) return true;
        if (Input.GetAxisRaw("Mouse X") != 0 || Input.GetAxisRaw("Mouse Y") != 0) return true;

        // Controller
        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0) return true;

        return false;
    }

    private void LoadLanding()
    {
        SceneManager.LoadScene(landingSceneName);
    }
}
