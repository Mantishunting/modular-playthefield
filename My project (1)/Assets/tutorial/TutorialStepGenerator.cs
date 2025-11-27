using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper component that creates all 22 tutorial steps with pre-configured settings
/// Add this to your TutorialManager GameObject temporarily, click "Generate Steps" in Inspector, then remove it
/// </summary>
public class TutorialStepGenerator : MonoBehaviour
{
    [Header("This is a helper script")]
    [TextArea(3, 5)]
    public string instructions = "This script will auto-generate all 22 tutorial steps with the correct settings from your tutorial design document. Click 'Generate Steps' button below in the Inspector, then you can remove this component.";
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(TutorialStepGenerator))]
    public class TutorialStepGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            TutorialStepGenerator generator = (TutorialStepGenerator)target;
            
            EditorGUILayout.Space();
            if (GUILayout.Button("Generate All 22 Tutorial Steps", GUILayout.Height(40)))
            {
                generator.GenerateSteps();
            }
        }
    }
    #endif
    
    public void GenerateSteps()
    {
        TutorialManager manager = GetComponent<TutorialManager>();
        if (manager == null)
        {
            Debug.LogError("TutorialStepGenerator: No TutorialManager found on this GameObject!");
            return;
        }
        
        TutorialStep[] steps = new TutorialStep[22];
        
        // Step 0: Introduction
        steps[0] = new TutorialStep
        {
            messageText = "You're a BEAN",
            showNextButton = true,
            showArrow = false,
            triggerType = TriggerType.NextButtonOnly,
            designerNotes = "Introduction"
        };
        
        // Step 1: First Stem Block
        steps[1] = new TutorialStep
        {
            messageText = "Click here to grow your stem",
            showNextButton = true,
            showArrow = true,
            arrowRotation = 90f, // Points up
            arrowTargetPosition = new Vector3(0, 1, 0), // TODO: Adjust this position
            triggerType = TriggerType.BlockCountIncrease,
            designerNotes = "First stem placement - arrow needs positioning. Player can also click Next to skip."
        };
        
        // Step 2: Continue Growing
        steps[2] = new TutorialStep
        {
            messageText = "Add another stem block",
            showNextButton = true,
            showArrow = true,
            arrowRotation = 0f, // Points right
            arrowTargetPosition = new Vector3(-1, 2, 0), // TODO: Adjust this position
            triggerType = TriggerType.BlockCountIncrease,
            designerNotes = "Wait for stem count to increase"
        };
        
        // Step 3: Insert Block
        steps[3] = new TutorialStep
        {
            messageText = "Try clicking here to insert a stem",
            showNextButton = true,
            showArrow = true,
            arrowRotation = 90f,
            arrowTargetPosition = new Vector3(0, 1.5f, 0), // TODO: Between blocks 2 and 3
            triggerType = TriggerType.BlockCountIncrease,
            designerNotes = "Teaches insertion mechanic"
        };
        
        // Step 4: Click and Hold
        steps[4] = new TutorialStep
        {
            messageText = "Click and hold to add lots of blocks",
            showNextButton = true,
            showArrow = false,
            triggerType = TriggerType.BlockCountIncrease,
            designerNotes = "Wait for 3 blocks to be added"
        };
        
        // Step 5: Deletion Controls
        steps[5] = new TutorialStep
        {
            messageText = "You can cut off a branch with triple right click. A single click will show what's going to be deleted. Deleting works for flowers and leaves too",
            showNextButton = true,
            showArrow = true,
            arrowRotation = 270f, // Points down
            arrowTargetPosition = new Vector3(0, 3, 0), // TODO: Point to a block to delete
            triggerType = TriggerType.BlockCountDecrease,
            designerNotes = "Must delete a block to advance"
        };
        
        // Step 6: Switch to Leaf
        steps[6] = new TutorialStep
        {
            messageText = "Good. Now try switching to Leaf",
            showNextButton = true,
            showArrow = true,
            arrowRotation = 0f,
            arrowTargetPosition = new Vector3(0, 0, 0), // TODO: Point to Leaf button in UI
            arrowShouldBounce = true,
            triggerType = TriggerType.LeafButtonClicked,
            designerNotes = "Arrow should point to Leaf UI button"
        };
        
        // Step 7: Wood Requirement
        steps[7] = new TutorialStep
        {
            messageText = "Leaves need wood blocks to grow",
            showNextButton = true,
            showArrow = false,
            triggerType = TriggerType.NextButtonOnly,
            designerNotes = "Info about leaf requirements"
        };
        
        // Step 8: Place Leaves
        steps[8] = new TutorialStep
        {
            messageText = "Add some leaf blocks on both sides of your stem",
            showNextButton = true,
            showArrow = true,
            arrowRotation = 0f, // Will need both 0° and 180° - special case
            arrowTargetPosition = new Vector3(1, 2, 0), // TODO: Right side first
            triggerType = TriggerType.BlockCountIncrease,
            designerNotes = "Wait for 2 leaves - may need special handling for sequential arrows"
        };
        
        // Step 9: Food Production
        steps[9] = new TutorialStep
        {
            messageText = "Leaves produce food",
            showNextButton = true,
            showArrow = true,
            arrowRotation = 180f, // Points left (or wherever food UI is)
            arrowTargetPosition = new Vector3(-5, 4, 0), // TODO: Point to food UI
            triggerType = TriggerType.NextButtonOnly,
            designerNotes = "Arrow should point to food counter"
        };
        
        // Step 10: Support Limit
        steps[10] = new TutorialStep
        {
            messageText = "A leaf can support about 5 blocks",
            showNextButton = true,
            showArrow = false,
            triggerType = TriggerType.NextButtonOnly,
            designerNotes = "Info dump"
        };
        
        // Step 11: Upkeep Cost
        steps[11] = new TutorialStep
        {
            messageText = "Each block has an upkeep cost",
            showNextButton = true,
            showArrow = false,
            triggerType = TriggerType.NextButtonOnly,
            designerNotes = "Info dump"
        };
        
        // Step 12: Food Indicator
        steps[12] = new TutorialStep
        {
            messageText = "This is your food. It tells you if your food's going up or down",
            showNextButton = true,
            showArrow = true,
            arrowRotation = 180f,
            arrowTargetPosition = new Vector3(-5, 4, 0), // TODO: Point to food UI
            triggerType = TriggerType.NextButtonOnly,
            designerNotes = "Point to food indicator"
        };
        
        // Step 13: Light Requirement
        steps[13] = new TutorialStep
        {
            messageText = "Leaves need light",
            showNextButton = true,
            showArrow = false,
            triggerType = TriggerType.NextButtonOnly,
            designerNotes = "Should trigger transparency animation - see TutorialManager.HandleSpecialStepSetup"
        };
        
        // Step 14: Shade Problem
        steps[14] = new TutorialStep
        {
            messageText = "If a leaf is in the shade it can't photosynthesise",
            showNextButton = true,
            showArrow = true,
            arrowRotation = 270f,
            arrowTargetPosition = new Vector3(0, 1, 0), // TODO: Point to shaded leaf
            triggerType = TriggerType.NextButtonOnly,
            designerNotes = "Should spawn shaded leaf - see TutorialManager.HandleSpecialStepSetup"
        };
        
        // Step 15: Remove Shaded Leaf
        steps[15] = new TutorialStep
        {
            messageText = "Remove this one now",
            showNextButton = true,
            showArrow = true,
            arrowRotation = 270f,
            arrowTargetPosition = new Vector3(0, 1, 0), // TODO: Same as step 14
            triggerType = TriggerType.BlockCountDecrease,
            designerNotes = "Must delete the shaded leaf"
        };
        
        // Step 16: Flowers Introduction
        steps[16] = new TutorialStep
        {
            messageText = "Flowers are very expensive",
            showNextButton = true,
            showArrow = true,
            arrowRotation = 0f,
            arrowTargetPosition = new Vector3(5, 0, 0), // TODO: Point to flower button
            triggerType = TriggerType.NextButtonOnly,
            designerNotes = "Arrow should point to Flower UI button"
        };
        
        // Step 17: Place a Flower
        steps[17] = new TutorialStep
        {
            messageText = "Place a flower now",
            showNextButton = true,
            showArrow = true,
            arrowRotation = 0f,
            arrowTargetPosition = new Vector3(5, 0, 0), // TODO: Point to flower button or placement spot
            triggerType = TriggerType.BlockCountIncrease,
            designerNotes = "Wait for flower placement"
        };
        
        // Step 18: Bee Visits
        steps[18] = new TutorialStep
        {
            messageText = "Bees will visit your flowers",
            showNextButton = true,
            showArrow = false,
            triggerType = TriggerType.NextButtonOnly,
            designerNotes = "Info about bees - can increase spawn rate here"
        };
        
        // Step 19: Win Condition
        steps[19] = new TutorialStep
        {
            messageText = "When a bee visits your flower, you'll progress to the next level",
            showNextButton = true,
            showArrow = false,
            triggerType = TriggerType.BeeVisitedFlower,
            designerNotes = "Tutorial ends when bee visits - loads next level"
        };
        
        // Step 20: Camera Drag
        steps[20] = new TutorialStep
        {
            messageText = "Hold right click and drag to move the camera",
            showNextButton = true,
            showArrow = false,
            triggerType = TriggerType.NextButtonOnly,
            designerNotes = "Camera controls - moved to end so arrows stay in correct position"
        };
        
        // Step 21: Camera Zoom
        steps[21] = new TutorialStep
        {
            messageText = "Middle mouse wheel to zoom in and out",
            showNextButton = true,
            showArrow = false,
            triggerType = TriggerType.NextButtonOnly,
            designerNotes = "Camera controls - moved to end so arrows stay in correct position"
        };
        
        // Assign to manager using reflection (since tutorialSteps is private)
        #if UNITY_EDITOR
        SerializedObject serializedManager = new SerializedObject(manager);
        SerializedProperty stepsProperty = serializedManager.FindProperty("tutorialSteps");
        
        stepsProperty.arraySize = 22;
        for (int i = 0; i < 22; i++)
        {
            SerializedProperty stepProp = stepsProperty.GetArrayElementAtIndex(i);
            CopyStepToSerializedProperty(steps[i], stepProp);
        }
        
        serializedManager.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);
        
        Debug.Log("✓ Successfully generated all 22 tutorial steps! Check the TutorialManager Inspector.");
        Debug.Log("⚠ Remember to adjust arrow positions marked with TODO comments!");
        #endif
    }
    
    #if UNITY_EDITOR
    private void CopyStepToSerializedProperty(TutorialStep step, SerializedProperty prop)
    {
        prop.FindPropertyRelative("messageText").stringValue = step.messageText;
        prop.FindPropertyRelative("showNextButton").boolValue = step.showNextButton;
        prop.FindPropertyRelative("showArrow").boolValue = step.showArrow;
        prop.FindPropertyRelative("arrowRotation").floatValue = step.arrowRotation;
        prop.FindPropertyRelative("arrowTargetPosition").vector3Value = step.arrowTargetPosition;
        prop.FindPropertyRelative("arrowShouldBounce").boolValue = step.arrowShouldBounce;
        prop.FindPropertyRelative("triggerType").enumValueIndex = (int)step.triggerType;
        prop.FindPropertyRelative("designerNotes").stringValue = step.designerNotes;
    }
    #endif
}
