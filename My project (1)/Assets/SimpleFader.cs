using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SimpleFader : MonoBehaviour
{
    [Header("Timing (seconds)")]
    public float startDelay = 0f;   // NEW
    public float fadeInTime = 1f;
    public float holdTime = 1f;
    public float fadeOutTime = 1f;

    private CanvasGroup cg;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        cg.alpha = 0f;

        // Delay before starting fade
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        // Fade In
        for (float t = 0; t < fadeInTime; t += Time.deltaTime)
        {
            cg.alpha = t / fadeInTime;
            yield return null;
        }
        cg.alpha = 1f;

        // Hold
        yield return new WaitForSeconds(holdTime);

        // Fade Out
        for (float t = 0; t < fadeOutTime; t += Time.deltaTime)
        {
            cg.alpha = 1f - (t / fadeOutTime);
            yield return null;
        }
        cg.alpha = 0f;
    }
}
