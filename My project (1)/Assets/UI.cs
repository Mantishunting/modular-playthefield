using UnityEngine;
using TMPro;
using System.Collections;

public class UI : MonoBehaviour
{
    [Header("Resource Display")]
    [SerializeField] private TextMeshProUGUI foodText;
    [SerializeField] private TextMeshProUGUI visitText;
    [SerializeField] private TextMeshProUGUI upkeepCostText;

    [Header("Food Rate & Upkeep Rhythms")]
    [Tooltip("Name of the block that produces food (must match exactly)")]
    [SerializeField] private string leafBlockName = "Leaf"; // <-- NEW: Tell UI what block to count

    [Tooltip("Amount of food gained per sun/leaf tick (default 100)")]
    [SerializeField] private int foodGainPerTick = 100;
    [Tooltip("Frequency (in seconds) of the Sun/Leaf food gain tick (default 4s)")]
    [SerializeField] private float foodGainFrequency = 4f;
    [Tooltip("Amount of food to subtract per upkeep tick (default 5)")]
    [SerializeField] private float upkeepCostPerTick = 5f;
    [Tooltip("Frequency (in seconds) of the Upkeep tick (default 3s)")]
    [SerializeField] private float upkeepFrequency = 3f;

    [Header("Win Popup & Level")]
    [SerializeField] private GameObject nextLevelButton;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private string winMessage = "FERTILISATION";
    [SerializeField] private float winFadeInSeconds = 0.35f;

    // Private variables for tracking
    private float netFoodRatePerSecond = 0f;
    private int currentLeafCount = 0; // <-- NEW: Track number of leaves
    private bool winShown = false;
    private Coroutine winFadeRoutine = null;
    private Coroutine upkeepRoutine = null;

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

        if (upkeepRoutine != null) StopCoroutine(upkeepRoutine);
    }

    void Start()
    {
        // 1. Initial Leaf Count (Find all existing blocks on start)
        CountExistingLeaves();

        // 2. Initial Rate Calculation
        RecalculateRate();

        // 3. Display Upkeep - MODIFIED to show cost per second (rounded)
        if (upkeepCostText != null)
        {
            // Calculate Upkeep cost per second: Cost / Frequency (e.g., 5 / 3s = 1.6667, rounded to 2)
            int upkeepPerSecond = Mathf.RoundToInt(upkeepCostPerTick / upkeepFrequency);
            upkeepCostText.text = $"Upkeep: -{upkeepPerSecond}/s";
        }

        // 4. Initialize UI elements
        if (winText != null)
        {
            var c = winText.color;
            c.a = 0f;
            winText.color = c;
            winText.gameObject.SetActive(false);
        }

        if (nextLevelButton != null) nextLevelButton.SetActive(false);

        UpdateVisitUI();

        // 5. Start Upkeep
        upkeepRoutine = StartCoroutine(UpkeepRoutine());
    }

    void Update()
    {
        // Food display
        if (foodText != null && Resources.Instance != null)
        {
            int currentFood = Resources.Instance.GetCurrentFood();
            // Round the net rate to the nearest integer for display
            int netRateRounded = Mathf.RoundToInt(netFoodRatePerSecond);

            // Set color based on the dynamic net rate (using the rounded integer)
            if (netRateRounded > 0)
            {
                foodText.color = Color.green;
            }
            else if (netRateRounded < 0)
            {
                foodText.color = Color.red;
            }
            else
            {
                foodText.color = Color.white;
            }

            // MODIFIED: Format based on user request (no decimals, integer rate with sign)
            if (netRateRounded != 0)
            {
                // Custom format string: +# (positive rate with plus sign), -# (negative rate with minus sign)
                string rateText = $" {netRateRounded:+#;-#}/s";
                foodText.text = currentFood.ToString() + rateText;
            }
            else
            {
                // If the rate is zero, just display the current food (e.g., "Food: 10")
                foodText.text = currentFood.ToString();
            }
        }
    }

    private void CountExistingLeaves()
    {
        currentLeafCount = 0;
        HumanClick[] allBlocks = FindObjectsOfType<HumanClick>();
        foreach (var block in allBlocks)
        {
            // Check if the block matches the leaf name
            BlockType type = block.GetBlockType();
            if (type != null && type.name == leafBlockName)
            {
                currentLeafCount++;
            }
        }
    }

    private void RecalculateRate()
    {
        // Income = (Gain * NumberOfLeaves) / Frequency
        float totalIncomePerSecond = (foodGainPerTick * currentLeafCount) / foodGainFrequency;

        // Upkeep = Cost / Frequency
        float totalUpkeepPerSecond = upkeepCostPerTick / upkeepFrequency;

        // Net Rate
        netFoodRatePerSecond = totalIncomePerSecond - totalUpkeepPerSecond;
    }

    // --- Event Handlers (Now Functional) ---

    private void HandlePlaced(BlockType type)
    {
        // If the placed block is a leaf, increase count and update rate
        if (type != null && type.name == leafBlockName)
        {
            currentLeafCount++;
            RecalculateRate();
        }
    }

    private void HandleDestroyed(BlockType type)
    {
        // If the destroyed block was a leaf, decrease count and update rate
        if (type != null && type.name == leafBlockName)
        {
            currentLeafCount--;
            // Safety check to prevent negative counts
            if (currentLeafCount < 0) currentLeafCount = 0;
            RecalculateRate();
        }
    }

    // --- Upkeep Coroutine ---

    private IEnumerator UpkeepRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(upkeepFrequency);

            if (Resources.Instance != null)
            {
                // Upkeep execution remains the same, subtracting the cost per tick
                Resources.Instance.SubtractFood(Mathf.RoundToInt(upkeepCostPerTick));
            }
        }
    }

    // --- Existing Helper Methods ---

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