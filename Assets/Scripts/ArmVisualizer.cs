
using UnityEngine;

public class ArmVisualizer : MonoBehaviour
{
    [Header("References")]
    public Transform arm;              // Arm/hand pivot
    public PivotManager pivotManager;  // Finds closest pivot

    [Header("Settings")]
    public float rotateSpeed = 5f;
    public Quaternion idleRotation;    // Resting full rotation when no pivot

    void Update()
    {
        Transform targetPivot = pivotManager.currentPivot;

        Quaternion targetRotation;

        if (targetPivot != null)
        {
            // --- Get world direction toward pivot ---
            Vector3 dirWorld = (targetPivot.position - arm.position).normalized;

            // --- Build a full aim rotation ---
            targetRotation = Quaternion.LookRotation(dirWorld, Vector3.up);
        }
        else
        {
            // No pivot → return to idle rotation
            targetRotation = idleRotation;
        }

        // Smoothly rotate toward target
        arm.rotation = Quaternion.Slerp(arm.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }
}

