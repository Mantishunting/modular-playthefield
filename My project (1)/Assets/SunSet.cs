using UnityEngine;

public class SkyColour : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Sun sun;
    [SerializeField] private SpriteRenderer background;

    [Header("Colours (Dawn → Dusk)")]
    [SerializeField] private Color colour1 = new Color(0.4f, 0.2f, 0.6f);  // Dawn
    [SerializeField] private Color colour2 = new Color(0.6f, 0.4f, 0.7f);  // Morning
    [SerializeField] private Color colour3 = new Color(0.8f, 0.6f, 0.9f);  // Midday
    [SerializeField] private Color colour4 = new Color(0.6f, 0.4f, 0.7f);  // Afternoon
    [SerializeField] private Color colour5 = new Color(0.3f, 0.1f, 0.4f);  // Dusk

    void Start()
    {
        if (sun == null)
            sun = FindObjectOfType<Sun>();

        if (background == null)
            background = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (sun == null || background == null) return;

        // Get normalised progress (0 to 1) through the sun's arc
        float t = Mathf.InverseLerp(sun.startAngle, sun.stopAngle, sun.GetCurrentAngle());

        // Pick colour based on which segment we're in
        Color targetColour;

        if (t <= 0.125f)
        {
            // 0 to 0.125: colour1 → colour2
            targetColour = Color.Lerp(colour1, colour2, t / 0.125f);
        }
        else if (t <= 0.5f)
        {
            // 0.125 to 0.5: colour2 → colour3
            targetColour = Color.Lerp(colour2, colour3, (t - 0.125f) / 0.375f);
        }
        else if (t <= 0.875f)
        {
            // 0.5 to 0.875: colour3 → colour4
            targetColour = Color.Lerp(colour3, colour4, (t - 0.5f) / 0.375f);
        }
        else
        {
            // 0.875 to 1: colour4 → colour5
            targetColour = Color.Lerp(colour4, colour5, (t - 0.875f) / 0.125f);
        }

        background.color = targetColour;
    }
}