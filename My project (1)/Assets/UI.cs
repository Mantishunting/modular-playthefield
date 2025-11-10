using UnityEngine;
using TMPro;
using System.Collections;

public class UI : MonoBehaviour
{
    [Header("Resource Display")]
    [SerializeField] private TextMeshProUGUI foodText;
    [SerializeField] private TextMeshProUGUI flowerText;

    [Header("Win Popup")]
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

            // Optional: if you want the popup to hide again if you drop below threshold
            if (flowerCount < winThreshold && winShown && winText != null)
            {
                // Hide instantly (cleaner UX than re-fading out, but you can add it)
                var c = winText.color;
                c.a = 0f;
                winText.color = c;
                winText.gameObject.SetActive(false);
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
        if (winShown || winText == null) return;
        if (flowerCount < winThreshold) return;

        winShown = true;
        winText.text = winMessage;
        winText.gameObject.SetActive(true);

        // reset alpha and fade in
        if (winFadeRoutine != null) StopCoroutine(winFadeRoutine);
        winFadeRoutine = StartCoroutine(FadeInWinText());
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

        // lock to fully visible
        c.a = 1f;
        winText.color = c;
        winFadeRoutine = null;
    }
}
