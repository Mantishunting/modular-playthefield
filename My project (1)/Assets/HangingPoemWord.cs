using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(DistanceJoint2D))]
public class HangingPoemWord : MonoBehaviour
{
    private TextMeshPro textMesh;
    private Rigidbody2D rb;
    private DistanceJoint2D joint;
    private HumanClick parentBlock;

    [Header("Visual Settings")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private float fontSize = 4f;
    [SerializeField] private float hangingDistance = 1.0f;

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

        // Force TMPro update to calculate preferred bounds immediately
        textMesh.ForceMeshUpdate();
        Vector2 textPreferredSize = textMesh.GetPreferredValues();

        // 2. Rigidbody Setup
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 0.1f;
        rb.linearDamping = 1f;
        
        // CRITICAL CONSTRAINT: Freeze rotation so text remains flat to the world horizon
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 3. Collider Setup (auto-adjust to word size)
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
        }
        col.size = new Vector2(textPreferredSize.x * 1.1f, textPreferredSize.y);
        col.isTrigger = false;

        // Ignore collision with the parent block to prevent jitter
        Collider2D parentCollider = parentBlock.GetComponent<Collider2D>();
        if (parentCollider != null)
        {
            Physics2D.IgnoreCollision(col, parentCollider);
        }

        // 4. Joint Setup
        joint = GetComponent<DistanceJoint2D>();
        Rigidbody2D parentRb = parentBlock.GetComponent<Rigidbody2D>();
        if (parentRb != null)
        {
            joint.connectedBody = parentRb;
            joint.autoConfigureConnectedAnchor = false;
            // Attach top of text to bottom of parent block
            joint.connectedAnchor = new Vector2(0f, -0.5f);
            joint.anchor = new Vector2(0f, textPreferredSize.y / 2f);
            joint.distance = hangingDistance;
            joint.maxDistanceOnly = false; // Rigid distance behaviour
            joint.enabled = true;
        }
        else
        {
            joint.enabled = false;
        }
    }
}
