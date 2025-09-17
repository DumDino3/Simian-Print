
using UnityEngine;

public class RaycastDetector : MonoBehaviour
{
    [Header("REFERENCES")]
    public PlayerController playerController; // Assign PlayerController

    [Header("Detection Settings")]
    public LayerMask groundLayer;
    public float checkDistance = 0.1f;
    public bool landedThisFrame;

    [Header("Ray Origins")]
    public Transform[] groundCasts;
    public Transform[] ceilingCasts;
    public Transform[] leftWallCasts;
    public Transform[] rightWallCasts;

    public void CheckEnvironment()
    {
        // reset per-frame
        landedThisFrame = false;

        // cache previous grounded before we overwrite it
        bool prevGrounded = playerController.isGrounded;

        playerController.isGrounded = CheckMultiple(groundCasts, Vector3.down);
        playerController.ceilingHit = CheckMultiple(ceilingCasts, Vector3.up);
        playerController.wallLeftHit = CheckMultiple(leftWallCasts, Vector3.left);
        playerController.wallRightHit = CheckMultiple(rightWallCasts, Vector3.right);

        // Detect "landing" edge: airborne -> grounded with non-positive vertical velocity.
        // (Avoid firing when snapping up from below, and ignore if we were already grounded.)
        if (!prevGrounded && playerController.isGrounded && playerController.currentState == MovementState.Airborne && playerController.velocity.y <= 0f)
        {
            landedThisFrame = true;
        }
    }

    bool CheckMultiple(Transform[] origins, Vector3 dir)
    {
        foreach (Transform origin in origins)
        {
            if (Physics.Raycast(origin.position, dir, checkDistance, groundLayer))
                return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw ground rays
        if (groundCasts != null)
        {
            Gizmos.color = Color.green;
            foreach (Transform t in groundCasts)
            {
                if (t != null)
                    Gizmos.DrawLine(t.position, t.position + Vector3.down * checkDistance);
            }
        }

        // Draw ceiling rays
        if (ceilingCasts != null)
        {
            Gizmos.color = Color.red;
            foreach (Transform t in ceilingCasts)
            {
                if (t != null)
                    Gizmos.DrawLine(t.position, t.position + Vector3.up * checkDistance);
            }
        }

        // Draw left wall rays
        if (leftWallCasts != null)
        {
            Gizmos.color = Color.blue;
            foreach (Transform t in leftWallCasts)
            {
                if (t != null)
                    Gizmos.DrawLine(t.position, t.position + Vector3.left * checkDistance);
            }
        }

        // Draw right wall rays
        if (rightWallCasts != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform t in rightWallCasts)
            {
                if (t != null)
                    Gizmos.DrawLine(t.position, t.position + Vector3.right * checkDistance);
            }
        }
    }
}
