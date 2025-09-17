using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class EnemyBrain : MonoBehaviour
{
    public enum State { Idle, Huh, Scan, Stomped, Alerted }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerController playerCtrl;   // read velocity / grounded / state
    //[SerializeField] private AimRig aimRig;
    [SerializeField] private Collider proximityTrigger;     // IsTrigger = true
    //[SerializeField] private ScanDetector scanDetector;     // will use later

    [Header("UI / Icons")]
    [SerializeField] private BehaviorIndicator indicators; // << Option B presenter

    [Header("Huh")]
    [SerializeField, Range(0.1f, 1.5f)] private float huhDuration = 0.6f;
    [SerializeField, Range(0.0f, 1.0f)] private float snapshotTime = 0.2f;  // when to snapshot during Huh

    [Header("Aggro/Signals")]
    public UnityEvent OnAlerted; // used later; Alerted state not implemented in this step

    [Header("Scan (Simple)")]
    [SerializeField] private Transform headToRotate;
    [SerializeField] private GameObject visionConeObject;
    [SerializeField, Range(0.5f, 6f)] private float scanDuration = 2.0f;
    [SerializeField, Range(0.5f, 20f)] private float scanAimLerp = 8f;

    // Your head mesh faces -Z by default; rotate 180° yaw so -Z aims at target.
    [SerializeField] private Vector3 headForwardOffsetEuler = new Vector3(0f, 180f, 0f);

    // Timer that resets on new noise during Scan
    private float _scanTimeRemaining;

    // Track if we’ve received a noise snapshot (so Huh can prefer it)
    private bool _hasNoiseSnapshot;

    // runtime
    public State Current { get; private set; } = State.Idle;
    private bool isPlayerInside;
    private int aggroCounter;
    private Coroutine _currentCoroutine;          // tracked coroutine for state timers
    private Vector3 _snapshotPoint;      // last-known/noise pos captured in Huh

    void OnEnable()
    {
        SwitchTo(State.Idle);
    }

    void OnDisable()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }
        _currentCoroutine = null;
    }

    // ---------- Basic update to kick us from Idle Huh using either trigger or distance/noise ----------


    // ---------- Proximity via trigger ----------
    void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("PlayerPresence"))
        {
            isPlayerInside = true;
        }
    }

    void OnTriggerExit(Collider collision)
    {
        if (collision.CompareTag("PlayerPresence"))
        {
            isPlayerInside = false;
        }
    }

    // ---------- Simple “noise spike” detector (we’ll refine later) ----------
    // Called by the player when a noisy action occurs.
    public void HearNoise(Vector3 noisePos)
    {
        // Only react if the player is inside our proximity trigger
        if (!isPlayerInside) return;

        _snapshotPoint = noisePos;
        _hasNoiseSnapshot = true;

        if (Current == State.Idle)
        {
            // Kick off the startle flow
            SwitchTo(State.Huh);
        }

        else if (Current == State.Scan)
        {
            // NEW: while scanning, keep aiming at the new source and extend the window
            _scanTimeRemaining = scanDuration;
            // (Head will lerp toward _snapshotPoint in CoScanSimple)
        }
    }


    // ---------- State machine ----------
    void SwitchTo(State next)
    {
        // stop previous state co only, not StopAllCoroutines()
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }
        _currentCoroutine = null;

        // exit effects for later states can go here

        Current = next;
        switch (next)
        {
            case State.Idle:
                {
                    EnterIdle();
                    break;
                }
            case State.Huh:
                {
                    _currentCoroutine = StartCoroutine(CoHuh());
                    break;
                }
            case State.Scan:   // NEW
                {
                    _scanTimeRemaining = scanDuration;              // NEW: start/refresh timer on entry
                    _currentCoroutine = StartCoroutine(CoScanSimple());
                    break;
                }
            default:
                {
                    EnterIdle(); // only implementing Idle+Huh this pass
                    break;
                }
        }
    }

    void EnterIdle()
    {
        aggroCounter = 0; // spec: reset aggro on entering Idle

        // UI: hide all indicators in Idle
        indicators.HideAll();
        if (visionConeObject) visionConeObject.SetActive(false);
        _hasNoiseSnapshot = false;
    }

    IEnumerator CoHuh()
    {
        // UI: show "?" during Huh
        indicators.ShowQuestion(true);

        // small wait before snapshot
        float t = 0f;
        bool playerLocationSnap = false;

        while (t < huhDuration)
        {
            t += Time.deltaTime;

            if (playerLocationSnap == false && t >= snapshotTime)
            {
                playerLocationSnap = true;

                if (!_hasNoiseSnapshot) _snapshotPoint = player.position; // prefer noise if we have one
            }

            // If stomp/other interrupts arrive later, they’d switch state here.

            yield return null;
        }

        // Huh ends → (later will go to Scan; for now return to Idle)
        SwitchTo(State.Scan);
    }

    IEnumerator CoScanSimple()
    {
        // UI: keep "?" on during Scan
        indicators.ShowQuestion(true);

        // Turn on the cone object
        if (visionConeObject) visionConeObject.SetActive(true);

        while (_scanTimeRemaining > 0f)
        {
            _scanTimeRemaining -= Time.deltaTime;  // FIX: count down
            LerpHeadTowards(_snapshotPoint, Time.deltaTime);
            yield return null;
        }

        // Turn cone off and clear UI
        if (visionConeObject) visionConeObject.SetActive(false);
        indicators.ShowQuestion(false);

        // Back to Idle (Alert transition will come later)
        SwitchTo(State.Idle);
    }

    void LerpHeadTowards(Vector3 worldTarget, float dt)
    {
        Transform swivel = headToRotate ? headToRotate : transform;

        Vector3 from = swivel.position;
        Vector3 dir = worldTarget - from;
        if (dir.sqrMagnitude < 0.0001f) return;

        // LookRotation assumes +Z is forward; your head model faces -Z.
        Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
        Quaternion targetRot = look * Quaternion.Euler(headForwardOffsetEuler);

        swivel.rotation = Quaternion.Slerp(swivel.rotation, targetRot, dt * scanAimLerp);
    }
}
