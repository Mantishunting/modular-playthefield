using UnityEngine;

// Add this script to your block prefab
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(HingeJoint2D))]
[RequireComponent(typeof(HumanClick))]
public class PhysicsConnector : MonoBehaviour
{
    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        HingeJoint2D joint = GetComponent<HingeJoint2D>();
        HumanClick humanClick = GetComponent<HumanClick>();

        // Find the parent block using HumanClick's logic
        HumanClick parentBlock = humanClick.GetParent(); // Uses the GetParent() method

        if (parentBlock != null)
        {
            // We have a parent! Find its Rigidbody2D and connect to it.
            Rigidbody2D parentRb = parentBlock.GetComponent<Rigidbody2D>();
            if (parentRb != null)
            {
                joint.connectedBody = parentRb;
            }
        }
        else
        {
            // No parent. We are a ROOT block.
            // Anchor this block to the world so it doesn't fall.
            rb.bodyType = RigidbodyType2D.Kinematic; // Use Kinematic for 2D

            // We don't need a joint if we're the root.
            if (joint != null) Destroy(joint);
        }
    }
}