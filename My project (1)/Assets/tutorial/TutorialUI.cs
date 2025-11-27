using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the visual display of tutorial UI elements
/// Shows text box, next button, and arrow based on current tutorial step
/// </summary>
public class TutorialUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button nextButton;
    [SerializeField] private GameObject arrow;
    
    [Header("References")]
    [SerializeField] private TutorialManager tutorialManager;
    
    [Header("Arrow Animation")]
    [SerializeField] private float bounceHeight = 0.2f;
    [SerializeField] private float bounceSpeed = 2f;
    
    private Vector3 arrowOriginalPosition;
    private bool arrowIsBouncing = false;
    
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
        
        if (arrow != null)
        {
            arrowOriginalPosition = arrow.transform.position;
        }
    }
    
    void Update()
    {
        // Handle arrow bouncing animation
        if (arrowIsBouncing && arrow != null)
        {
            float bounce = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
            arrow.transform.position = arrowOriginalPosition + new Vector3(0, bounce, 0);
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
        
        // Show/hide next button
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(step.showNextButton);
        }
        
        // Show/hide and position arrow
        if (arrow != null)
        {
            arrow.SetActive(step.showArrow);
            
            if (step.showArrow)
            {
                // Set arrow position
                arrow.transform.position = step.arrowTargetPosition;
                arrowOriginalPosition = step.arrowTargetPosition;
                
                // Set arrow rotation (0=right, 90=up, 180=left, 270=down)
                arrow.transform.rotation = Quaternion.Euler(0, 0, step.arrowRotation);
                
                // Set bouncing animation
                arrowIsBouncing = step.arrowShouldBounce;
            }
            else
            {
                arrowIsBouncing = false;
            }
        }
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
        
        if (arrow != null)
        {
            arrow.SetActive(false);
            arrowIsBouncing = false;
        }
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
}
