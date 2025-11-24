using UnityEngine;
using TMPro;
using System.Collections;

public class UI : MonoBehaviour
{
    [Header("Resource Display")]
    [SerializeField] private TextMeshProUGUI foodText;
    [SerializeField] private TextMeshProUGUI visitText;

    [Header("Win Popup & Level")]
    [Tooltip("The Button that loads the next level")]
    [SerializeField] private GameObject nextLevelButton;

    [Tooltip("TMP text object that will pop up when you reach the threshold")]
    [SerializeField] private TextMeshProUGUI winText;
    [Tooltip("Message shown on win")]
    [SerializeField] private string winMessage = "FERTILISATION";
    [Tooltip("Seconds to fade the win text in")]
    [SerializeField] private float winFadeInSeconds = 0.35f;

    private bool winShown = false;
    private Coroutine winFadeRoutine = null;

    void OnEnable()
    {
        HumanClick.OnBlockPlaced += HandlePlaced;
        HumanClick.OnBlockDestroyed += HandleDestroyed;
        BeeVisitTracker.OnVisitRegistered += HandleVisitRegistered;
        BeeVisitTracker.OnWinConditionMet += ShowWin;
    }

    void OnDisable()
    {
        HumanClick.OnBlockPlaced -= HandlePlaced;
        HumanClick.OnBlockDestroyed -= HandleDestroyed;
        BeeVisitTracker.OnVisitRegistered -= HandleVisitRegistered;
        BeeVisitTracker.OnWinConditionMet -= ShowWin;
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
            nextLevelButton.SetActive(false);
        }

        UpdateVisitUI();
    }

    void Update()
    {
        // Food display
        if (foodText != null && Resources.Instance != null)
        {
            foodText.text = Resources.Instance.GetCurrentFood().ToString();
        }
    }

    private void HandlePlaced(BlockType type)
    {
        // Available for future use
    }

    private void HandleDestroyed(BlockType type)
    {
        // Available for future use
    }

    private void HandleVisitRegistered(int totalVisits)
    {
        UpdateVisitUI();
    }

    private void UpdateVisitUI()
    {
        if (visitText != null && BeeVisitTracker.Instance != null)
        {
            int current = BeeVisitTracker.Instance.TotalVisits;
            int goal = BeeVisitTracker.Instance.visitsToWin;
            visitText.text = $"Visits: {current} / {goal}";
        }
    }

    private void ShowWin()
    {
        if (winShown) return;

        winShown = true;

        // 1. Show Button
        if (nextLevelButton != null)
        {
            nextLevelButton.SetActive(true);
        }

        // 2. Show Text
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