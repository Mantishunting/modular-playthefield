using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the visual display of tutorial UI elements
/// Shows text box, next button, and arrow based on current tutorial step
/// Supports both world-space arrows (for blocks) and UI-space arrows (for buttons)
/// </summary>
public class TutorialUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button nextButton;
    [SerializeField] private GameObject uiArrow; // Arrow in Canvas for pointing at UI elements

    [Header("World Space Arrow")]
    [SerializeField] private GameObject worldArrow; // Arrow in scene for pointing at game objects

    [Header("References")]
    [SerializeField] private TutorialManager tutorialManager;

    [Header("Arrow Animation")]
    [SerializeField] private float bounceHeight = 0.2f;
    [SerializeField] private float bounceSpeed = 2f;

    [Header("Next Button Timing")]
    [SerializeField] private float infoStepDelay = 2f; // Delay for info-only steps
    [SerializeField] private float actionStepDelay = 5f; // Delay for action-required steps

    private Vector3 worldArrowOriginalPosition;
    private Vector2 uiArrowOriginalPosition;
    private bool arrowIsBouncing = false;
    private bool currentArrowIsWorldSpace = false;

    private Coroutine nextButtonDelayCoroutine;

    void Start()
    {
        if (tutorialManager == null)
        {
            tutorialManager = FindObjectOfType<TutorialManager>();
            if (tutorialManager == null)
            {
                Debug.LogError("TutorialUI: Cannot find TutorialManager in scene!");
                enabled = false;
                return;
            }
        }

        if (nextButton != null && tutorialManager != null)
        {
            nextButton.onClick.AddListener(() => tutorialManager.OnNextButtonClicked());
        }
        else if (nextButton != null)
        {
            Debug.LogError("TutorialUI: TutorialManager is null, cannot connect Next button!");
        }
        else
        {
            Debug.LogWarning("TutorialUI: No Next button assigned!");
        }

        // Store initial positions
        if (worldArrow != null)
        {
            worldArrowOriginalPosition = worldArrow.transform.position;
        }

        if (uiArrow != null)
        {
            RectTransform uiArrowRect = uiArrow.GetComponent<RectTransform>();
            if (uiArrowRect != null)
            {
                uiArrowOriginalPosition = uiArrowRect.anchoredPosition;
            }
        }
    }

    void Update()
    {
        // Handle arrow bouncing animation
        if (arrowIsBouncing)
        {
            float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;

            if (currentArrowIsWorldSpace && worldArrow != null)
            {
                // Bounce world space arrow in world units
                worldArrow.transform.position = worldArrowOriginalPosition + new Vector3(0, bounce, 0);
            }
            else if (!currentArrowIsWorldSpace && uiArrow != null)
            {
                // Bounce UI arrow in UI units
                RectTransform arrowRect = uiArrow.GetComponent<RectTransform>();
                if (arrowRect != null)
                {
                    arrowRect.anchoredPosition = uiArrowOriginalPosition + new Vector2(0, bounce);
                }
            }
        }
    }

    /// <summary>
    /// Display a tutorial step with its message, button, and arrow
    /// </summary>
    public void DisplayStep(TutorialStep step)
    {
        if (step == null)
        {
            Debug.LogError("TutorialUI: Attempted to display null step!");
            return;
        }

        // Show the panel
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        // Set message text
        if (messageText != null)
        {
            messageText.text = step.messageText;
        }

        // Show/hide next button with delay
        if (nextButton != null)
        {
            if (step.showNextButton)
            {
                // Stop any existing delay coroutine
                if (nextButtonDelayCoroutine != null)
                {
                    StopCoroutine(nextButtonDelayCoroutine);
                }

                // Determine delay based on trigger type
                float delay = step.triggerType == TriggerType.NextButtonOnly ? infoStepDelay : actionStepDelay;

                // Start delayed appearance
                nextButtonDelayCoroutine = StartCoroutine(ShowNextButtonAfterDelay(delay));
            }
            else
            {
                // No button needed, hide immediately
                nextButton.gameObject.SetActive(false);
            }
        }

        // Show/hide and position arrows
        if (step.showArrow)
        {
            if (step.arrowInWorldSpace)
            {
                // Use world space arrow for pointing at blocks/game objects
                ShowWorldArrow(step);
            }
            else
            {
                // Use UI arrow for pointing at buttons/UI elements
                ShowUIArrow(step);
            }

            arrowIsBouncing = step.arrowShouldBounce;
        }
        else
        {
            // Hide both arrows
            HideAllArrows();
        }
    }

    /// <summary>
    /// Show the world space arrow and hide UI arrow
    /// </summary>
    private void ShowWorldArrow(TutorialStep step)
    {
        if (worldArrow != null)
        {
            worldArrow.SetActive(true);
            worldArrow.transform.position = step.arrowTargetPosition;
            worldArrowOriginalPosition = step.arrowTargetPosition;
            worldArrow.transform.rotation = Quaternion.Euler(0, 0, step.arrowRotation);
            currentArrowIsWorldSpace = true;
        }
        else
        {
            Debug.LogWarning("TutorialUI: Step requires world arrow but worldArrow is not assigned!");
        }

        if (uiArrow != null)
        {
            uiArrow.SetActive(false);
        }
    }

    /// <summary>
    /// Show the UI arrow and hide world arrow
    /// </summary>
    private void ShowUIArrow(TutorialStep step)
    {
        if (uiArrow != null)
        {
            uiArrow.SetActive(true);

            RectTransform arrowRect = uiArrow.GetComponent<RectTransform>();
            if (arrowRect != null)
            {
                arrowRect.anchoredPosition = step.arrowUIPosition;
                uiArrowOriginalPosition = step.arrowUIPosition;
                arrowRect.rotation = Quaternion.Euler(0, 0, step.arrowRotation);
                currentArrowIsWorldSpace = false;
            }
            else
            {
                Debug.LogError("TutorialUI: uiArrow does not have RectTransform component!");
            }
        }
        else
        {
            Debug.LogWarning("TutorialUI: Step requires UI arrow but uiArrow is not assigned!");
        }

        if (worldArrow != null)
        {
            worldArrow.SetActive(false);
        }
    }

    /// <summary>
    /// Hide all arrows
    /// </summary>
    private void HideAllArrows()
    {
        if (uiArrow != null)
        {
            uiArrow.SetActive(false);
        }

        if (worldArrow != null)
        {
            worldArrow.SetActive(false);
        }

        arrowIsBouncing = false;
    }

    /// <summary>
    /// Hide all tutorial UI elements
    /// </summary>
    public void Hide()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        HideAllArrows();
    }

    /// <summary>
    /// Show the tutorial UI (useful if you want to re-enable after hiding)
    /// </summary>
    public void Show()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Coroutine to show Next button after a delay
    /// </summary>
    private System.Collections.IEnumerator ShowNextButtonAfterDelay(float delay)
    {
        // Hide button initially
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
        }

        // Wait for delay
        yield return new WaitForSeconds(delay);

        // Show button
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
        }
    }
}