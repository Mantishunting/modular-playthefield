using UnityEngine;

public class Flower : MonoBehaviour
{
    [Header("Detection Mode")]
    [Tooltip("If true, bees pass through. If false, bees bounce off.")]
    [SerializeField] private bool usesTrigger = true;

    [Header("State")]
    [SerializeField] private int timesVisited = 0;

    public int TimesVisited => timesVisited;

    void Start()
    {
        var collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = usesTrigger;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (usesTrigger) return;

        if (collision.gameObject.GetComponent<Bee>() != null)
        {
            RegisterVisit();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!usesTrigger) return;

        if (other.GetComponent<Bee>() != null)
        {
            RegisterVisit();
        }
    }

    void RegisterVisit()
    {
        timesVisited++;

        if (BeeVisitTracker.Instance != null)
        {
            BeeVisitTracker.Instance.RegisterVisit();
        }

        Debug.Log($"Flower visited! {gameObject.name} - Local: {timesVisited}");
    }
}