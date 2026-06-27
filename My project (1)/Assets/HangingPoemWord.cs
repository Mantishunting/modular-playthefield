using UnityEngine;
using TMPro;

public class HangingPoemWord : MonoBehaviour
{
    private TextMeshPro textMesh;
    private HumanClick parentBlock;

    [Header("Visual Settings")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float fontSize = 4f;
    [SerializeField] private float hangingDistance = 1.0f;

    [Header("Follow Settings")]
    [Tooltip("If true, the word sways with a soft visual drag behind the block. If false, it snaps instantly.")]
    [SerializeField] private bool useSmoothFollow = true;
    [SerializeField] private float followSpeed = 10f;

    public void Initialize(HumanClick parentBlock, string word)
    {
        this.parentBlock = parentBlock;

        // 1. TextMeshPro Setup
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMeshPro>();
        }
        textMesh.text = word;
        textMesh.fontSize = fontSize;
        textMesh.color = textColor;
        textMesh.alignment = TextAlignmentOptions.Center;

        // Immediately set initial position to avoid a frame of starting at (0,0,0)
        transform.position = parentBlock.transform.position + Vector3.down * hangingDistance;
        transform.rotation = Quaternion.identity;
    }

    private void LateUpdate()
    {
        if (parentBlock != null)
        {
            Vector3 targetPos = parentBlock.transform.position + Vector3.down * hangingDistance;
            
            if (useSmoothFollow && Application.isPlaying)
            {
                // Soft fluid lag/sway effect without any physical mass/feedback
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
            }
            else
            {
                transform.position = targetPos;
            }

            // Lock rotation to keep text parallel to the world horizon
            transform.rotation = Quaternion.identity;
        }
    }
}
