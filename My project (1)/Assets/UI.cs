using UnityEngine;
using TMPro;
using System.Collections;

public class UI : MonoBehaviour
{
    [Header("Resource Display")]
    [SerializeField] private TextMeshProUGUI foodText;
    [SerializeField] private TextMeshProUGUI visitText;

    [Header("Food Rate Tracking")]
    [Tooltip("How many seconds to average over (should be longer than leaf production cycle)")]
    [SerializeField] private float sampleWindow = 5f;

    [Header("Win Popup & Level")]
    [SerializeField] private GameObject nextLevelButton;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private string winMessage = "FERTILISATION";
    [SerializeField] private float winFadeInSeconds = 0.35f;

    // Rolling window tracking
    private float foodPerSecond = 0f;
    private int foodAtWindowStart = 0;
    private float windowTimer = 0f;

    // Win state
    private bool winShown = false;
    private Coroutine winFadeRoutine = null;

    void OnEnable()
    {
        BeeVisitTracker.OnVisitRegistered += HandleVisitRegistered;
        BeeVisitTracker.OnWinConditionMet += ShowWin;
    }

    void OnDisable()
    {
        BeeVisitTracker.OnVisitRegistered -= HandleVisitRegistered;
        BeeVisitTracker.OnWinConditionMet -= ShowWin;
    }

    void Start()
    {
        // Initialize food tracking
        if (ResourceManager.Instance != null)
        {
            foodAtWindowStart = ResourceManager.Instance.GetCurrentFood();
        }
        windowTimer = 0f;

        // Initialize UI elements
        if (winText != null)
        {
            var c = winText.color;
            c.a = 0f;
            winText.color = c;
            winText.gameObject.SetActive(false);
        }

        if (nextLevelButton != null) nextLevelButton.SetActive(false);

        UpdateVisitUI();
    }

    void Update()
    {
        if (ResourceManager.Instance == null) return;

        // --- Rolling window: measure change over sampleWindow seconds ---
        int currentFood = ResourceManager.Instance.GetCurrentFood();
        windowTimer += Time.deltaTime;

        if (windowTimer >= sampleWindow)
        {
            foodPerSecond = (currentFood - foodAtWindowStart) / windowTimer;
            foodAtWindowStart = currentFood;
            windowTimer = 0f;
        }

        // --- Display ---
        if (foodText != null)
        {
            int netRateRounded = Mathf.RoundToInt(foodPerSecond);

            // Color based on rate
            if (netRateRounded > 0)
                foodText.color = Color.green;
            else if (netRateRounded < 0)
                foodText.color = Color.red;
            else
                foodText.color = Color.white;

            // Format display
            if (netRateRounded != 0)
                foodText.text = $"{currentFood} {netRateRounded:+#;-#}/s";
            else
                foodText.text = currentFood.ToString();
        }
    }

    // --- Visit Tracking ---

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
            visitText.text = $"{current} / {goal}";
        }
    }

    // --- Win State ---

    private void ShowWin()
    {
        if (winShown) return;

        winShown = true;

        if (nextLevelButton != null) nextLevelButton.SetActive(true);

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