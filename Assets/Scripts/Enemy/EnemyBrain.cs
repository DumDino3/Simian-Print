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
    void Update()
    {
        if (Current == State.Idle)
        {
            // Choose either trigger-based or distance-based here:
            if (isPlayerInside && PlayerMadeNoise())
            {
                SwitchTo(State.Huh);
            }
        }
    }

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
    bool PlayerMadeNoise()
    {
   // Very simple rule for now:
        // 1) Running fast OR 2) Recent heavy landing (airborne->grounded with high |vy|).
        // You already expose velocity and grounded state. (We’ll replace with a nicer gate later.)
        if (playerCtrl.velocity.magnitude > playerCtrl.moveSpeed * 0.9f)
        {
            return true;
        }

        return false;
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

                // snapshot player (or last-known/noise) position

                _snapshotPoint = player.position;
            }

            // If stomp/other interrupts arrive later, they’d switch state here.

            yield return null;
        }

        // Huh ends → (later will go to Scan; for now return to Idle)
        indicators.ShowQuestion(false);

        SwitchTo(State.Idle);
    }
}
