using UnityEngine;
using System;

public class BeeVisitTracker : MonoBehaviour
{
    public static BeeVisitTracker Instance { get; private set; }

    [Header("Win Condition")]
    public int visitsToWin = 50;

    [Header("State")]
    [SerializeField] private int totalVisits = 0;

    public int TotalVisits => totalVisits;

    public static event Action<int> OnVisitRegistered;
    public static event Action OnWinConditionMet;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterVisit()
    {
        totalVisits++;
        Debug.Log($"Bee visit registered! Total: {totalVisits}/{visitsToWin}");

        OnVisitRegistered?.Invoke(totalVisits);

        if (totalVisits >= visitsToWin)
        {
            OnWin();
        }
    }

    void OnWin()
    {
        Debug.Log("WIN! Pollination target reached!");
        OnWinConditionMet?.Invoke();
    }

    public void ResetVisits()
    {
        totalVisits = 0;
    }
}