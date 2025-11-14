using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Displays tutorial messages one after another at the start of the game.
/// Messages fade in, hold, then fade out before the next one appears.
/// </summary>
public class TutorialMessages : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI tutorialText;

    [Header("Tutorial Messages")]
    [SerializeField]
    private string[] messages = new string[]
    {
        "You're a bean!",
        "Click the Wood button to grow your stem. Try it now!",
        "Click Leaf to add leaves to your body.",
        "You're not ready to flower... yet.",
        "Reach for the light!",
        "Leaves only produce food when you are in sunlight.",
        "Be careful. Light comes and goes, so spread out your leaves.",
        "When the bar spins and turns red, you'll start losing branches.",
        "Each cell you add will contiune to eat.",
        "Balance your resources as well as your branches.",
        "You are beautiful, and you will grow far and wide."
    };

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float holdDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float delayBetweenMessages = 0.3f;

    [Header("Settings")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool showDebugLogs = false;

    private bool isPlaying = false;

    void Start()
    {
        // Make sure text starts hidden
        if (tutorialText != null)
        {
            var c = tutorialText.color;
            c.a = 0f;
            tutorialText.color = c;
            tutorialText.gameObject.SetActive(false);
        }

        if (playOnStart)
        {
            StartTutorial();
        }
    }

    /// <summary>
    /// Start playing the tutorial messages
    /// </summary>
    public void StartTutorial()
    {
        if (isPlaying)
        {
            if (showDebugLogs)
                Debug.LogWarning("Tutorial is already playing!");
            return;
        }

        if (tutorialText == null)
        {
            Debug.LogError("Tutorial text reference is not assigned!");
            return;
        }

        StartCoroutine(PlayTutorialSequence());
    }

    /// <summary>
    /// Plays all tutorial messages in sequence
    /// </summary>
    IEnumerator PlayTutorialSequence()
    {
        isPlaying = true;

        if (showDebugLogs)
            Debug.Log("Starting tutorial sequence...");

        for (int i = 0; i < messages.Length; i++)
        {
            if (showDebugLogs)
                Debug.Log($"Showing message {i + 1}/{messages.Length}: {messages[i]}");

            yield return StartCoroutine(ShowMessage(messages[i]));

            // Delay between messages (except after the last one)
            if (i < messages.Length - 1)
            {
                yield return new WaitForSeconds(delayBetweenMessages);
            }
        }

        isPlaying = false;

        if (showDebugLogs)
            Debug.Log("Tutorial sequence complete!");
    }

    /// <summary>
    /// Shows a single message with fade in, hold, and fade out
    /// </summary>
    IEnumerator ShowMessage(string message)
    {
        // Set the message text
        tutorialText.text = message;
        tutorialText.gameObject.SetActive(true);

        // Fade in
        yield return StartCoroutine(FadeText(0f, 1f, fadeInDuration));

        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Fade out
        yield return StartCoroutine(FadeText(1f, 0f, fadeOutDuration));

        // Hide the GameObject
        tutorialText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Fades the text from one alpha to another over a duration
    /// </summary>
    IEnumerator FadeText(float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        Color c = tutorialText.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            c.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            tutorialText.color = c;
            yield return null;
        }

        // Lock to final value
        c.a = toAlpha;
        tutorialText.color = c;
    }

    /// <summary>
    /// Skip to the end of the tutorial (optional, can be called by a skip button)
    /// </summary>
    public void SkipTutorial()
    {
        if (isPlaying)
        {
            StopAllCoroutines();
            isPlaying = false;

            if (tutorialText != null)
            {
                var c = tutorialText.color;
                c.a = 0f;
                tutorialText.color = c;
                tutorialText.gameObject.SetActive(false);
            }

            if (showDebugLogs)
                Debug.Log("Tutorial skipped!");
        }
    }

    /// <summary>
    /// Check if tutorial is currently playing
    /// </summary>
    public bool IsPlaying()
    {
        return isPlaying;
    }

    /// <summary>
    /// Update a specific message at runtime (optional)
    /// </summary>
    public void SetMessage(int index, string newMessage)
    {
        if (index >= 0 && index < messages.Length)
        {
            messages[index] = newMessage;
        }
        else
        {
            Debug.LogWarning($"Invalid message index: {index}");
        }
    }
}