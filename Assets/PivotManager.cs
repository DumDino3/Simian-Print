
using UnityEngine;

public class PivotManager : MonoBehaviour
{
    [Header("Detection Settings")]
    public LayerMask pivotLayer;
    public float pivotDetectionRadius = 5f;

    [Header("Runtime Info")]
    public Transform currentPivot; // Closest pivot this frame
    public float currentPivotDistance;      // Distance to that pivot

    public void DetectClosestPivot(Vector3 origin)
    {
        // Find all colliders in detection radius
        Collider[] hits = Physics.OverlapSphere(origin, pivotDetectionRadius, pivotLayer);

        if (hits.Length == 0)
        {
            //Debug.Log("NO pivots in range!");
            currentPivot = null;
            return;
        }

        //Debug.Log("Found " + hits.Length + " pivots in range.");

        currentPivot = null;
        currentPivotDistance = Mathf.Infinity;

        // Find closest pivot
        foreach (var h in hits)
        {
            float dist = Vector3.Distance(origin, h.transform.position);
            //Debug.Log("Pivot candidate: " + h.name + " distance=" + dist);

            if (dist < currentPivotDistance)
            {
                currentPivotDistance = dist;
                currentPivot = h.transform;
            }
        }
        //Debug.Log("Closest pivot now: " + (currentPivot ? currentPivot.name : "NONE"));
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pivotDetectionRadius);

        // Draw line to current pivot for debug
        if (currentPivot != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentPivot.position);
        }
    }
}
