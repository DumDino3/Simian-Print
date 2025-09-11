using UnityEngine;

public class EnemyHead : MonoBehaviour
{
    public enum AimAxis { PosZ, NegZ, PosX, NegX, PosY, NegY }

    [Header("Cone children (visuals)")]
    [SerializeField] private GameObject wideCone;  // child named "WideCone"
    [SerializeField] private GameObject longCone;  // child named "LongCone"

    [Header("Aiming")]
    [SerializeField] private AimAxis aimAxis = AimAxis.NegZ;
    [SerializeField] private Vector3 upHint = Vector3.up;

    [Header("Vision (Short/Wide) – Startled")]
    public float closeRange = 3f;
    [Range(0, 180)] public float closeHalfAngle = 70f;

    [Header("Vision (Long/Narrow) – Suspicious")]
    public float longRange = 8f;
    [Range(0, 180)] public float longHalfAngle = 20f;

    [Header("Line of Sight")]
    public LayerMask obstacleMask;
    public float playerHitRadius = 0.25f;

    // ---------- lifecycle ----------
    void Awake()
    {
        AutoWireConesIfMissing();
        // hard guarantee: cones OFF on load
        ToggleCones(false, false);
    }

    void OnEnable()
    {
        // if this object is toggled on/off at runtime, keep cones off until AI says otherwise
        ToggleCones(false, false);
    }

    void OnValidate()
    {
        AutoWireConesIfMissing();
    }

    private void AutoWireConesIfMissing()
    {
        if (!wideCone)
        {
            var t = transform.Find("WideCone");
            if (t) wideCone = t.gameObject;
        }
        if (!longCone)
        {
            var t = transform.Find("LongCone");
            if (t) longCone = t.gameObject;
        }
    }

    // ---------- public API ----------
    public void ToggleCones(bool wideOn, bool longOn)
    {
        if (wideCone && wideCone.activeSelf != wideOn) wideCone.SetActive(wideOn);
        if (longCone && longCone.activeSelf != longOn) longCone.SetActive(longOn);
    }

    public void SnapLookAt(Vector3 targetWorld)
    {
        transform.rotation = DesiredRotationTowards(targetWorld);
    }

    public void RotateTowards(Vector3 targetWorld, float degPerSec)
    {
        var desired = DesiredRotationTowards(targetWorld);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, degPerSec * Time.deltaTime);
    }

    public bool CanSeeTarget(Transform target, bool useCloseCone)
    {
        if (!target) return false;

        float range = useCloseCone ? closeRange : longRange;
        float halfAngle = useCloseCone ? closeHalfAngle : longHalfAngle;

        Vector3 origin = transform.position;
        Vector3 toTarget = target.position - origin;
        float dist = toTarget.magnitude;
        if (dist > range) return false;

        Vector3 fwd = GetWorldLookAxis();
        if (Vector3.Angle(fwd, toTarget) > halfAngle) return false;

        if (Physics.Raycast(origin, toTarget.normalized, out RaycastHit hit, dist, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        if (playerHitRadius > 0f)
        {
            var hits = Physics.OverlapSphere(target.position, playerHitRadius, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
                if (h.transform == target || h.CompareTag("Player"))
                    return true;
            return false;
        }

        return true;
    }

    // ---------- internals ----------
    Quaternion DesiredRotationTowards(Vector3 targetWorld)
    {
        Vector3 dir = (targetWorld - transform.position).normalized;
        if (dir.sqrMagnitude < 1e-6f) return transform.rotation;
        Quaternion lookPlusZ = Quaternion.LookRotation(dir, SafeUp(dir));
        return lookPlusZ * AxisToPlusZOffset(aimAxis);
    }

    Vector3 GetWorldLookAxis()
    {
        return aimAxis switch
        {
            AimAxis.PosZ => transform.forward,
            AimAxis.NegZ => -transform.forward,
            AimAxis.PosX => transform.right,
            AimAxis.NegX => -transform.right,
            AimAxis.PosY => transform.up,
            _ => -transform.up,
        };
    }

    static Quaternion AxisToPlusZOffset(AimAxis axis)
    {
        Vector3 localAxis = axis switch
        {
            AimAxis.PosZ => Vector3.forward,
            AimAxis.NegZ => Vector3.back,
            AimAxis.PosX => Vector3.right,
            AimAxis.NegX => Vector3.left,
            AimAxis.PosY => Vector3.up,
            _ => Vector3.down,
        };
        return Quaternion.FromToRotation(localAxis, Vector3.forward);
    }

    Vector3 SafeUp(Vector3 dir)
    {
        var up = upHint.sqrMagnitude < 1e-6f ? Vector3.up : upHint.normalized;
        if (Mathf.Abs(Vector3.Dot(dir.normalized, up)) > 0.98f)
            up = Mathf.Abs(Vector3.Dot(dir, Vector3.right)) < 0.9f ? Vector3.right : Vector3.forward;
        return up;
    }
}
