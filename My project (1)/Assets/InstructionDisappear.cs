using UnityEngine;
using UnityEngine.EventSystems; // Required for click detection

/// <summary>
/// Makes this GameObject disappear based on a selected mode (Click or Timer).
/// Attach this script to the UI Image/Sprite you want to hide.
/// </summary>
public class InstructionDisappear : MonoBehaviour, IPointerClickHandler
{
    // This creates the dropdown menu in the Inspector
    public enum DisappearMode
    {
        OnClick,
        OnTimer
    }

    [Tooltip("How should this object disappear?")]
    public DisappearMode mode = DisappearMode.OnClick;

    [Tooltip("Time in seconds before disappearing (only used for Timer mode)")]
    public float delayInSeconds = 30.0f;


    // This method runs when the game starts
    void Start()
    {
        // If the mode is set to Timer, start the countdown
        if (mode == DisappearMode.OnTimer)
        {
            // Call the "Disappear" method after 'delayInSeconds'
            Invoke(nameof(Disappear), delayInSeconds);
        }
    }

    // This method runs when the object is clicked
    // It's required by the IPointerClickHandler interface
    public void OnPointerClick(PointerEventData eventData)
    {
        // If the mode is set to OnClick, disappear immediately
        if (mode == DisappearMode.OnClick)
        {
            Disappear();
        }
    }

    // The method that actually hides the object
    private void Disappear()
    {
        gameObject.SetActive(false);
    }
}