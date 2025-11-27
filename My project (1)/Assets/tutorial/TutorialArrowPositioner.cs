using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper tool for positioning tutorial arrows in the scene
/// Attach to TutorialManager temporarily while setting up arrow positions
/// </summary>
public class TutorialArrowPositioner : MonoBehaviour
{
    [Header("Instructions")]
    [TextArea(4, 8)]
    public string instructions = 
        "1. Play the game\n" +
        "2. Advance to the step you want to position\n" +
        "3. PAUSE the game (not stop!)\n" +
        "4. Move WorldArrow in scene to desired position\n" +
        "5. Rotate WorldArrow to desired angle\n" +
        "6. Click 'Save Current Arrow Position' button below\n" +
        "7. Continue to next step and repeat";
    
    [Header("Current Step Info")]
    [SerializeField] private int currentStepIndex = -1;
    [SerializeField] private string currentStepText = "Not running";
    
    private TutorialManager tutorialManager;
    private TutorialUI tutorialUI;
    private GameObject worldArrow;
    
    void Start()
    {
        tutorialManager = GetComponent<TutorialManager>();
        tutorialUI = GetComponent<TutorialUI>();
        
        if (tutorialManager == null)
        {
            Debug.LogError("TutorialArrowPositioner: No TutorialManager on this GameObject!");
            return;
        }
        
        if (tutorialUI == null)
        {
            Debug.LogError("TutorialArrowPositioner: No TutorialUI on this GameObject!");
            return;
        }
        
        // Find worldArrow through reflection since it's private
        var field = tutorialUI.GetType().GetField("worldArrow", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            worldArrow = field.GetValue(tutorialUI) as GameObject;
        }
        
        if (worldArrow == null)
        {
            Debug.LogWarning("TutorialArrowPositioner: Could not find worldArrow reference. Make sure TutorialUI has worldArrow assigned.");
        }
    }
    
    void Update()
    {
        // Update current step info display
        if (tutorialManager != null && Application.isPlaying)
        {
            // Use reflection to get private currentStepIndex
            var indexField = tutorialManager.GetType().GetField("currentStepIndex", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (indexField != null)
            {
                currentStepIndex = (int)indexField.GetValue(tutorialManager);
            }
            
            // Use reflection to get tutorialSteps array
            var stepsField = tutorialManager.GetType().GetField("tutorialSteps", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (stepsField != null)
            {
                TutorialStep[] steps = stepsField.GetValue(tutorialManager) as TutorialStep[];
                if (steps != null && currentStepIndex >= 0 && currentStepIndex < steps.Length)
                {
                    currentStepText = steps[currentStepIndex].messageText;
                }
            }
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Save the current worldArrow position and rotation to the current tutorial step
    /// </summary>
    public void SaveCurrentArrowPosition()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("TutorialArrowPositioner: Game must be PLAYING (and paused) to save arrow position!");
            EditorUtility.DisplayDialog("Not Playing", 
                "You must be playing the game (and paused on a step) to save arrow positions!", "OK");
            return;
        }
        
        if (tutorialManager == null)
        {
            Debug.LogError("TutorialArrowPositioner: No TutorialManager found!");
            return;
        }
        
        if (worldArrow == null)
        {
            Debug.LogError("TutorialArrowPositioner: WorldArrow not found!");
            return;
        }
        
        // Get current step index
        var indexField = tutorialManager.GetType().GetField("currentStepIndex", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (indexField == null)
        {
            Debug.LogError("TutorialArrowPositioner: Could not access currentStepIndex!");
            return;
        }
        
        int stepIndex = (int)indexField.GetValue(tutorialManager);
        
        // Get tutorial steps array
        var stepsField = tutorialManager.GetType().GetField("tutorialSteps", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (stepsField == null)
        {
            Debug.LogError("TutorialArrowPositioner: Could not access tutorialSteps!");
            return;
        }
        
        TutorialStep[] steps = stepsField.GetValue(tutorialManager) as TutorialStep[];
        
        if (steps == null || stepIndex < 0 || stepIndex >= steps.Length)
        {
            Debug.LogError($"TutorialArrowPositioner: Invalid step index {stepIndex}!");
            return;
        }
        
        TutorialStep currentStep = steps[stepIndex];
        
        // Get current arrow position and rotation
        Vector3 arrowPosition = worldArrow.transform.position;
        float arrowRotation = worldArrow.transform.rotation.eulerAngles.z;
        
        // Update the step
        currentStep.arrowTargetPosition = arrowPosition;
        currentStep.arrowRotation = arrowRotation;
        currentStep.arrowInWorldSpace = true; // Make sure it's set to world space
        
        // Mark as dirty so changes persist when exiting play mode
        EditorUtility.SetDirty(tutorialManager);
        
        Debug.Log($"✓ Saved arrow position for Step {stepIndex}: Position={arrowPosition}, Rotation={arrowRotation}°");
        Debug.Log($"  Step text: \"{currentStep.messageText}\"");
        
        EditorUtility.DisplayDialog("Position Saved!", 
            $"Arrow position saved for Step {stepIndex}:\n\n" +
            $"Position: {arrowPosition}\n" +
            $"Rotation: {arrowRotation}°\n\n" +
            $"Step: \"{currentStep.messageText}\"", "OK");
    }
    #endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(TutorialArrowPositioner))]
public class TutorialArrowPositionerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        TutorialArrowPositioner positioner = (TutorialArrowPositioner)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This tool only works while the game is PLAYING and PAUSED.\n\n" +
            "1. Play game\n" +
            "2. Pause on step you want to position\n" +
            "3. Move WorldArrow in scene\n" +
            "4. Click button below", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        GUI.enabled = Application.isPlaying;
        
        if (GUILayout.Button("Save Current Arrow Position", GUILayout.Height(40)))
        {
            positioner.SaveCurrentArrowPosition();
        }
        
        GUI.enabled = true;
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Button only works while game is playing!", MessageType.Warning);
        }
    }
}
#endif
