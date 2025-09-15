using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class PlayerController : MonoBehaviour
{
    [Header("REFERENCES")]
    public PhysicsCalculator physicsCalculator;
    public RaycastDetector raycastDetector;
    public PivotManager pivotManager;
    public Animator anim;

    [Header("MOVEMENT SETTINGS")]
    public float moveSpeed;
    public float crouchSpeed;
    public float gravityStrength;
    public float airMoveValue;
    public float jumpStrength;
    public bool canSecondJump = false;
    //private bool usedSecondJump = false;

    [Header("RUNTIME STATE")]
    public bool isRunning;
    public bool isCrouching;
    public bool isNoisy;
    public float moveDir;
    public Vector3 velocity;
    public GameObject Body;
    public MovementState currentState = MovementState.Grounded;

    [Header("COLLISION CHECKER")]
    public bool wallLeftHit = false;
    public bool wallRightHit = false;
    public bool ceilingHit = false;
    public bool isGrounded = true;

    [Header("PIVOT VARIABLES")]
    public float ropeLength;
    public float speed;
    public float angularVelocity;
    private float swingAngle;
    private Vector3 pivotPosition;
    public float upBoostMultiplier = 1.3f;

    [Header("PROCEDURAL VARIABLES")]
    public bool isRight = true;

    //Anim para hash
    static readonly int RunHash = Animator.StringToHash("Run");
    static readonly int IdleHash = Animator.StringToHash("Idle");
    static readonly int JumpHash = Animator.StringToHash("Jump");
    static readonly int FallHash = Animator.StringToHash("Fall");
    static readonly int SwingHash = Animator.StringToHash("Swing");
    static readonly int CrouchHash = Animator.StringToHash("Crouch");


    //------------------------------------------------------------------------------------------------- ON UPDATE -------------------------------------------------------------------------------------------------

    void Update()
    {
        ReadInput();

        RotateBodyFoward();

        //Debug.Log(currentState);

        DetectPivotIfAirborne();

        if (currentState == MovementState.Pivot)
        {
            //anim.SetTrigger("Swing");
            SwingInputAndLogic();
        }


        else
        {
            CalculateNormalVelocity();
            CollisionCheck();
            ApplyVelocity();
        }

        UpdateAnimator();


    }

    //------------------------------------------------------------------------------------------------- INPUT -------------------------------------------------------------------------------------------------
    void ReadInput() //This has been put in update
    {
        // Basic horizontal input
        moveDir = 0;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveDir = -1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveDir = 1;

        if (Input.GetKey(KeyCode.L))
        {
            isCrouching = true;
            isRunning = false;
        }

        else
        {
            isCrouching = false;
            isRunning = true;
        }

        //Space input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentState == MovementState.Grounded && isGrounded)
            {
                velocity.y = jumpStrength;
                EnterAirborne();
                //usedSecondJump = false;   // fresh airtime
                canSecondJump = false;   // will be re-enabled by trigger
            }
            else if (currentState == MovementState.Airborne /*&& !usedSecondJump*/ && canSecondJump)
            {
                velocity.y = jumpStrength;
                //usedSecondJump = true;    // consume the extra jump
                canSecondJump = false;   // must re-enter a head trigger to get another
            }
        }

        //Enable pivot attach if airborne
        if (pivotManager.currentPivot != null && (Input.GetKeyDown(KeyCode.J)))
        {
            AttachToPivot(pivotManager.currentPivot);
        }

        // Pivot release
        if (currentState == MovementState.Pivot && (Input.GetKeyUp(KeyCode.J)))
        {
            //Debug.Log("unpressed J");
            DetachFromPivot();
        }
    }

    void SwingInputAndLogic()
    {
        // --- INPUT INTENT (W dominates combos with A/D) ---
        bool holdA = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool holdD = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        bool holdW = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);

        bool driveUp = holdW && (holdA || holdD) || holdW;
        bool driveRight = !driveUp && holdD && !holdA;
        bool driveLeft = !driveUp && holdA && !holdD;

        // cache previous α (deg) for exact-angle crossing tests
        float prevAlphaDeg = swingAngle * Mathf.Rad2Deg - 90f; // right=0, up=+90, left=±180, down=−90

        if (driveUp || driveRight || driveLeft)
        {
            // --- FIXED DIRECTION BY FACING: right = CCW(+), left = CW(-) ---
            float sign = isRight ? +1f : -1f;
            angularVelocity = sign * speed;

            // integrate θ locally (drive ignores gravity)
            swingAngle += angularVelocity * Time.deltaTime;

            float currAlphaDeg = swingAngle * Mathf.Rad2Deg - 90f;

            // --- DETACH RULES (exact angles via crossing) ---
            if (driveRight)
            {
                // D => detach at α = 0°
                if (CrossedAngle(prevAlphaDeg, currAlphaDeg, -35f))
                {
                    ForceDetach(addUpBoost: false);
                    return;
                }
            }
            else if (driveLeft)
            {
                // A => detach at α = 180°
                if (CrossedAngle(prevAlphaDeg, currAlphaDeg, 210f))
                {
                    ForceDetach(addUpBoost: false);
                    return;
                }
            }
            else // driveUp
            {
                // W => +45° if facing right, −135° if facing left
                float target = isRight ? 40f : 140f;
                if (CrossedAngle(prevAlphaDeg, currAlphaDeg, target))
                {
                    ForceDetach(addUpBoost: true);
                    return;
                }
            }

            // place player on rope circle (same formula you already use on attach)
            Vector3 offset = new Vector3(Mathf.Sin(swingAngle), -Mathf.Cos(swingAngle), 0f) * ropeLength;
            Vector3 newPos = pivotPosition + offset;
            newPos.z = this.transform.position.z;
            transform.position = newPos;
        }
        else
        {
            // --- NO DRIVE INPUT: keep your existing gravity pendulum path via calculator ---
            Vector3 newPos = physicsCalculator.CalculateVelocity(
                currentState,
                velocity,
                moveSpeed,
                moveDir,
                airMoveValue,
                gravityStrength,
                jumpStrength,
                ref swingAngle,
                ref angularVelocity,
                pivotPosition,
                ropeLength,
                isRunning,
                isCrouching,
                crouchSpeed,
                Time.deltaTime);
            transform.position = newPos; // your Pivot path returns absolute position 
        }
    }

    //------------------------------------------------------------------------------------------------- PHYSICS -------------------------------------------------------------------------------------------------

    void CalculateNormalVelocity()
    {
        velocity = physicsCalculator.CalculateVelocity(
            currentState,
            velocity,
            moveSpeed,
            moveDir,
            airMoveValue,
            gravityStrength,
            jumpStrength,
            ref swingAngle,
            ref angularVelocity,
            pivotPosition,
            ropeLength,
            isRunning,
            isCrouching,
            crouchSpeed,
            Time.deltaTime);
    }

    void ApplyVelocity()
    {
        Vector3 newPos = transform.position + velocity * Time.deltaTime;
        newPos.z = this.transform.position.z;
        transform.position = newPos;
    }

    void CollisionCheck()
    {
        raycastDetector.CheckEnvironment();

        //Grounding logic
        if (!isGrounded && currentState != MovementState.Airborne && currentState != MovementState.Pivot)
        {
            EnterAirborne();
        }
        else if (isGrounded && currentState == MovementState.Airborne && velocity.y <= 0)
        {
            EnterGrounded();
        }

        //Ceiling
        if (ceilingHit && velocity.y > 0)
        {
            velocity.y = 0;
        }

        //Walls 
        if (wallLeftHit && velocity.x < 0)
        {
            velocity.x = 0;
        }
        if (wallRightHit && velocity.x > 0)
        {
            velocity.x = 0;
        }
    }

    //------------------------------------------------------------------------------------------------- STATE LOGIC -------------------------------------------------------------------------------------------------

    void EnterAirborne()
    {
        isGrounded = false;
        currentState = MovementState.Airborne;
    }

    void EnterGrounded()
    {
        currentState = MovementState.Grounded;
        velocity.y = 0f;   // reset vertical velocity
    }

    void DetectPivotIfAirborne()
    {
        if (currentState == MovementState.Airborne)
        {
            pivotManager.DetectClosestPivot(transform.position);
        }
    }

    void AttachToPivot(Transform pivot)
    {
        pivotPosition = pivot.position;

        // Calculate angle relative to vertical down
        Vector3 dir = (transform.position - pivotPosition).normalized;
        swingAngle = Mathf.Atan2(dir.x, -dir.y);


        //float baseAngular = Mathf.Abs(velocity.x) / velocity.x;
        float baseAngular = 1;
        if (isRight) { baseAngular = 1; } else if (!isRight) { baseAngular = -1; }

        angularVelocity = baseAngular * speed;

        //  SNAP player exactly onto the rope arc
        Vector3 offset = new Vector3(Mathf.Sin(swingAngle), -Mathf.Cos(swingAngle), 0f) * ropeLength;
        transform.position = pivotPosition + offset;

        currentState = MovementState.Pivot;

        //Debug.Log($"AttachToPivot: {pivot.name}, ropeLength={ropeLength}, swingAngle={swingAngle}, angularVel={angularVelocity}");
    }

    void DetachFromPivot()
    {
        // Compute fling tangent velocity
        Vector3 tangentDir = new Vector3(Mathf.Cos(swingAngle), Mathf.Sin(swingAngle), 0f);
        velocity = tangentDir * (angularVelocity * ropeLength);

        currentState = MovementState.Airborne;
    }

    //------------------------------------------------------------------------------------------------- VISUAL LOGIC -------------------------------------------------------------------------------------------------
    void RotateBodyFoward()
    {
        if (moveDir > 0)
        {
            Vector3 localEuler = Body.transform.localEulerAngles;
            localEuler.y = -6.445f;
            Body.transform.localEulerAngles = localEuler;

            isRight = true;
        }

        else if (moveDir < 0)
        {
            Vector3 localEuler = Body.transform.localEulerAngles;
            localEuler.y = -6.445f + 180f;
            Body.transform.localEulerAngles = localEuler;

            isRight = false;
        }
    }

    void UpdateAnimatorAndSound()
    {
        bool crouching = isGrounded && moveDir != 0f && isCrouching;
        bool running = isGrounded && moveDir != 0f && isRunning;
        bool idle = isGrounded && !running && !crouching;
        bool swing = currentState == MovementState.Pivot;
        bool jump = !isGrounded && velocity.y > 0f && !swing;
        bool fall = !isGrounded && velocity.y <= 0f && !swing;

        anim.SetBool(CrouchHash, crouching);
        anim.SetBool(RunHash, running);
        anim.SetBool(IdleHash, idle);
        anim.SetBool(JumpHash, jump);
        anim.SetBool(FallHash, fall);
        anim.SetBool(SwingHash, swing);
    }

    //------------------------------------------------------------------------------------------------- PIVOT HELPERS -------------------------------------------------------------------------------------------------

    // Wrap-safe detection that we hit the exact target angle between frames (zero tolerance semantics).
    bool CrossedAngle(float prevAlphaDeg, float currAlphaDeg, float targetDeg)
    {
        float a0 = Mathf.DeltaAngle(prevAlphaDeg, targetDeg);
        float a1 = Mathf.DeltaAngle(currAlphaDeg, targetDeg);

        // Exact hit is always valid
        if (Mathf.Approximately(a1, 0f)) return true;

        // Must cross the target line...
        if (Mathf.Sign(a0) != Mathf.Sign(a1))
        {
            // ...and the crossing must occur on the target's semicircle (±90°),
            // not way over by the opposite angle (±180°).
            return (Mathf.Abs(a0) <= 90f) || (Mathf.Abs(a1) <= 90f);
        }
        return false;
    }

    // Forced detach for angle thresholds; reuses your fling math and adds optional Up boost.
    void ForceDetach(bool addUpBoost)
    {
        Vector3 tangentDir = new Vector3(Mathf.Cos(swingAngle), Mathf.Sin(swingAngle), 0f);
        velocity = tangentDir * (angularVelocity * ropeLength);

        if (addUpBoost)
        {
            float minBoostY = jumpStrength * upBoostMultiplier;
            if (velocity.y < minBoostY) velocity.y = minBoostY;
            velocity.x = 0f;
        }

        currentState = MovementState.Airborne;
    }

}


//------------------------------------------------------------------------------------------------- STATES ENUM -------------------------------------------------------------------------------------------------
public enum MovementState
{
    Grounded,
    Airborne,
    Pivot
}