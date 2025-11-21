using UnityEngine;
using TMPro;
using System.Collections;

public class UI : MonoBehaviour
{
    [Header("Resource Display")]
    [SerializeField] private TextMeshProUGUI foodText;
    [SerializeField] private TextMeshProUGUI flowerText;

    [Header("Win Popup & Level")]
    [Tooltip("The Button that loads the next level")]
    [SerializeField] private GameObject nextLevelButton; // <--- NEW BUTTON SLOT

    [Tooltip("TMP text object that will pop up when you reach the threshold")]
    [SerializeField] private TextMeshProUGUI winText;
    [Tooltip("How many flowers to win")]
    [SerializeField] private int winThreshold = 10;
    [Tooltip("Message shown on win")]
    [SerializeField] private string winMessage = "Calafornication";
    [Tooltip("Seconds to fade the win text in")]
    [SerializeField] private float winFadeInSeconds = 0.35f;

    private int flowerCount = 0;
    private bool winShown = false;
    private Coroutine winFadeRoutine = null;

    void OnEnable()
    {
        HumanClick.OnBlockPlaced += HandlePlaced;
        HumanClick.OnBlockDestroyed += HandleDestroyed;
    }

    void OnDisable()
    {
        HumanClick.OnBlockPlaced -= HandlePlaced;
        HumanClick.OnBlockDestroyed -= HandleDestroyed;
    }

    void Start()
    {
        // Ensure win text starts hidden
        if (winText != null)
        {
            var c = winText.color;
            c.a = 0f;
            winText.color = c;
            winText.gameObject.SetActive(false);
        }

        // Ensure button starts hidden
        if (nextLevelButton != null)
        {
            nextLevelButton.SetActive(false); // <--- HIDE BUTTON ON START
        }

        UpdateFlowerUI();
    }

    void Update()
    {
        // Food display (kept as-is)
        if (foodText != null && Resources.Instance != null)
        {
            foodText.text = Resources.Instance.GetCurrentFood().ToString();
        }
    }

    private void HandlePlaced(BlockType type)
    {
        if (type != null && type.blockName == "Flower")
        {
            flowerCount++;
            UpdateFlowerUI();
            TryShowWin();
        }
    }

    private void HandleDestroyed(BlockType type)
    {
        if (type != null && type.blockName == "Flower")
        {
            flowerCount = Mathf.Max(0, flowerCount - 1);
            UpdateFlowerUI();

            // Hide popup AND button if they drop below threshold
            if (flowerCount < winThreshold && winShown)
            {
                if (winText != null)
                {
                    var c = winText.color;
                    c.a = 0f;
                    winText.color = c;
                    winText.gameObject.SetActive(false);
                }

                if (nextLevelButton != null)
                {
                    nextLevelButton.SetActive(false); // <--- HIDE BUTTON
                }

                winShown = false;
            }
        }
    }

    private void UpdateFlowerUI()
    {
        if (flowerText != null)
        {
            flowerText.text = $"Flowers: {flowerCount}";
        }
    }

    private void TryShowWin()
    {
        if (winShown) return; // Already won
        if (flowerCount < winThreshold) return;

        winShown = true;

        // 1. Show Button
        if (nextLevelButton != null)
        {
            nextLevelButton.SetActive(true); // <--- SHOW BUTTON
        }

        // 2. Show Text (Keep existing visual flair)
        if (winText != null)
        {
            winText.text = winMessage;
            winText.gameObject.SetActive(true);

            if (winFadeRoutine != null) StopCoroutine(winFadeRoutine);
            winFadeRoutine = StartCoroutine(FadeInWinText());
        }
    }

    private IEnumerator FadeInWinText()
    {
        float t = 0f;
        var c = winText.color;
        c.a = 0f;
        winText.color = c;

        while (t < winFadeInSeconds)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / winFadeInSeconds);
            c.a = a;
            winText.color = c;
            yield return null;
        }

        c.a = 1f;
        winText.color = c;
        winFadeRoutine = null;
    }
}