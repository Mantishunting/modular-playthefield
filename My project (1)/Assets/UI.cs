using UnityEngine;
using TMPro;

public class UI : MonoBehaviour
{
    [Header("Resource Display")]
    [SerializeField] private TextMeshProUGUI foodText;

    void Update()
    {
        // Update the food display every frame
        if (foodText != null && Resources.Instance != null)
        {
            foodText.text = Resources.Instance.GetCurrentFood().ToString();
        }
    }
}