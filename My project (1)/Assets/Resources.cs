using UnityEngine;

public class Resources : MonoBehaviour
{
    public static Resources Instance { get; private set; }

    [Header("Resource Tracking")]
    [SerializeField] private int currentFood = 0;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Debug key: Press F to manually add 1 Food for testing
        if (Input.GetKeyDown(KeyCode.F))
        {
            AddFood(1);
            Debug.Log($"[DEBUG] Manual food added. Current Food: {currentFood}");
        }
    }

    /// <summary>
    /// Adds food to the total resource pool
    /// </summary>
    public void AddFood(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"Tried to add non-positive food amount: {amount}");
            return;
        }

        currentFood += amount;

        if (showDebugLogs)
        {
            Debug.Log($"Food added: +{amount} | Total Food: {currentFood}");
        }
    }

    /// <summary>
    /// Gets the current total food count
    /// </summary>
    public int GetCurrentFood()
    {
        return currentFood;
    }

    /// <summary>
    /// For future use: Subtracts food from the pool
    /// </summary>
    public bool TrySpendFood(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"Tried to spend non-positive food amount: {amount}");
            return false;
        }

        if (currentFood >= amount)
        {
            currentFood -= amount;

            if (showDebugLogs)
            {
                Debug.Log($"Food spent: -{amount} | Total Food: {currentFood}");
            }

            return true;
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log($"Not enough food! Need: {amount}, Have: {currentFood}");
            }

            return false;
        }
    }

    /// <summary>
    /// For future use: Checks if we can afford an amount without spending it
    /// </summary>
    public bool CanAfford(int amount)
    {
        return currentFood >= amount;
    }
}