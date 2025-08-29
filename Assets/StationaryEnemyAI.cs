using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class StationaryEnemyAI : MonoBehaviour
{
    public enum State { Idle, Startled, Suspicious, Alert }

    [Header("References")]
    [SerializeField] private EnemyHead head;                 // HeadPivot
    [SerializeField] private Transform player;               // Player root
    [SerializeField] private PlayerController playerCtrl;    // Uses playerCtrl.velocity (no Rigidbody needed)

    [Header("Awareness Trigger")]
    [SerializeField] private Collider triggerZone;           // MUST be IsTrigger = true

    [Header("Exclamation Canvas")]
    [SerializeField] private GameObject exclamationCanvas;   // World-space canvas to toggle

    [Header("Timing")]
    [SerializeField] private float startledDuration = 1.0f;
    [SerializeField] private float loseInterestTime = 2.5f;

    [Header("Rotation")]
    [SerializeField] private float suspiciousTurnSpeed = 180f;

    [Header("Movement Detection")]
    [SerializeField] private float moveThreshold = 0.1f;

    [Header("Events")]
    public UnityEvent OnAlert;

    // runtime
    public State CurrentState { get; private set; } = State.Idle;
    private bool _playerInside = false;
    private float _loseTimer = 0f;

    void Awake()
    {
        if (!triggerZone) triggerZone = GetComponent<Collider>();
        if (!player && playerCtrl) player = playerCtrl.transform;
        if (!playerCtrl && player) playerCtrl = player.GetComponent<PlayerController>();

        if (head) head.ToggleCones(false, false); // Wide/Long OFF at boot
        SetCanvas(false);                          // "!" OFF at boot
    }

    void OnEnable() => TransitionTo(State.Idle);

    void Update()
    {
        // ---- YOUR LOGIC ----
        // While inside the trigger, watch velocity; if moving and we're Idle -> Startled
        if (_playerInside && CurrentState == State.Idle && IsPlayerMoving())
        {
            TransitionTo(State.Startled);
            return;
        }

        // State-specific updates
        if (CurrentState == State.Startled)
        {
            if (head && player && head.CanSeeTarget(player, useCloseCone: true))
                TransitionTo(State.Alert);
        }
        else if (CurrentState == State.Suspicious)
        {
            DoSuspiciousUpdate();
        }
    }

    // ------------ States ------------
    void TransitionTo(State next)
    {
        StopAllCoroutines();

        switch (next)
        {
            case State.Idle:
                _loseTimer = 0f;
                SetCanvas(false);
                if (head) head.ToggleCones(false, false);
                break;

            case State.Startled:
                EnterStartled();
                break;

            case State.Suspicious:
                EnterSuspicious();
                break;

            case State.Alert:
                EnterAlert();
                break;
        }

        CurrentState = next;
    }

    void EnterStartled()
    {
        SetCanvas(true);                      // show "!"
        if (head)
        {
            head.ToggleCones(true, false);    // Wide ON, Long OFF
            if (player) head.SnapLookAt(player.position);
        }
        StartCoroutine(StartledTimer());
    }

    IEnumerator StartledTimer()
    {
        float t = 0f;
        while (t < startledDuration)
        {
            if (head && player && head.CanSeeTarget(player, useCloseCone: true))
            {
                TransitionTo(State.Alert);
                yield break;
            }
            t += Time.deltaTime;
            yield return null;
        }
        TransitionTo(State.Suspicious);
    }

    void EnterSuspicious()
    {
        _loseTimer = 0f;
        if (head) head.ToggleCones(false, true); // Wide OFF, Long ON
        // Canvas stays ON during Suspicious
    }

    void DoSuspiciousUpdate()
    {
        if (player && head)
            head.RotateTowards(player.position, suspiciousTurnSpeed);

        if (head && player && head.CanSeeTarget(player, useCloseCone: false))
        {
            TransitionTo(State.Alert);
            return;
        }

        bool visible = head && player && head.CanSeeTarget(player, useCloseCone: false);
        bool moving = IsPlayerMoving();

        // If not visible AND (player left trigger OR stopped moving) -> count down back to Idle
        if (!visible && (!_playerInside || !moving))
        {
            _loseTimer += Time.deltaTime;
            if (_loseTimer >= loseInterestTime) TransitionTo(State.Idle);
        }
        else _loseTimer = 0f;
    }

    void EnterAlert()
    {
        OnAlert?.Invoke();
        TransitionTo(State.Idle);
    }

    // ------------ Triggers (just set the flag) ------------
    void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other)) _playerInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other)) _playerInside = false;
    }

    bool IsPlayer(Collider c)
    {
        if (player && c.transform.root == player) return true;
        return c.CompareTag("Player") || c.transform.root.CompareTag("Player");
    }

    // ------------ Helpers ------------
    bool IsPlayerMoving()
    {
        if (!playerCtrl) return true;
        return playerCtrl.velocity.sqrMagnitude > (moveThreshold * moveThreshold);
    }

    void SetCanvas(bool on)
    {
        if (exclamationCanvas) exclamationCanvas.SetActive(on);
    }
}
