using UnityEngine;

[System.Serializable]
public class PhysicsCalculator
{
    public Vector3 CalculateVelocity(
        MovementState state,
        Vector3 currentVelocity,
        float moveSpeed,
        float moveDir,
        float airMoveValue,
        float gravityStrength,
        float jumpStrength,
        ref float swingAngle,
        ref float angularVelocity,
        Vector3 pivotPosition,
        float ropeLength,
        float deltaTime
    )
    {
        switch (state)
        {
            //------------------------------------------------------------------------------------- GROUNDED ----------------------------------------------------------------------------------------------
            case MovementState.Grounded:
                currentVelocity.x = moveDir * moveSpeed;
                currentVelocity.y = 0f;
                return currentVelocity;

            //------------------------------------------------------------------------------------- AIRBORNE ----------------------------------------------------------------------------------------------
            case MovementState.Airborne:
                //float targetAirSpeed = moveDir * airMoveValue;
                //currentVelocity.x = Mathf.Lerp(currentVelocity.x, targetAirSpeed, 0.1f);
                //// Gravity
                //currentVelocity.y -= gravityStrength * deltaTime;
                //return currentVelocity;

                // 1) Start from incoming velocity (don’t zero it)
                Vector3 v = currentVelocity;

                // 2) Gravity integration
                v.y -= gravityStrength * deltaTime;

                // 3) Horizontal air control: steer toward target speed with a time-based lerp
                //    - moveDir is your A/D input in [-1, 0, 1]
                //    - moveSpeed is your max horizontal speed
                //    - airMoveValue is "how quickly you correct" (higher = snappier)
                float targetVx = moveDir * moveSpeed;

                // Exponential, framerate-independent smoothing: t in [0..1] per dt
                float t = 1f - Mathf.Exp(-airMoveValue * deltaTime);
                v.x = Mathf.Lerp(v.x, targetVx, t);

                // 4) Keep Z as-is
                return v;

            //--------------------------------------------------------------------------------------- PIVOT ----------------------------------------------------------------------------------------------
            case MovementState.Pivot:

                float L = Mathf.Max(ropeLength, 0.05f);
                float g = gravityStrength;           // use your game gravity
                const float gravityBias = 1.2f;      // >1 pulls harder toward bottom (try 2–4)
                const float damping = 2f;      // big viscous drag kills swing/oscillation
                const float accelCap = 500f;      // safety caps
                const float omegaCap = 50f;

                // Torque toward straight-down (θ = 0) + heavy damping
                float angAcc = -(g / L) * Mathf.Sin(swingAngle) * gravityBias
                               - damping * angularVelocity;
                angAcc = Mathf.Clamp(angAcc, -accelCap, accelCap);

                // Semi-implicit Euler
                angularVelocity += angAcc * deltaTime;
                angularVelocity = Mathf.Clamp(angularVelocity, -omegaCap, omegaCap);
                swingAngle += angularVelocity * deltaTime;

                // Wrap to [-π, π] and add tiny dead-zone to “stick” at bottom
                swingAngle = Mathf.Repeat(swingAngle + Mathf.PI, 2f * Mathf.PI) - Mathf.PI;
                if (Mathf.Abs(swingAngle) < 1f * Mathf.Deg2Rad && Mathf.Abs(angularVelocity) < 0.2f)
                {
                    swingAngle = 0f; angularVelocity = 0f;
                }

                // Constrained position on the rope circle
                Vector3 offset = new Vector3(Mathf.Sin(swingAngle), -Mathf.Cos(swingAngle), 0f) * L;
                return pivotPosition + offset;

            //float gravity = 9.81f;

            //// Pendulum angular acceleration
            //float angularAcceleration = -(gravity / ropeLength) * Mathf.Sin(swingAngle);

            //// Update angular velocity + angle
            //angularVelocity += angularAcceleration * deltaTime;
            //swingAngle += angularVelocity * deltaTime;

            //// Constrained offset from pivot
            //Vector3 offset = new Vector3(
            //    Mathf.Sin(swingAngle),
            //    -Mathf.Cos(swingAngle),
            //    0f
            //) * ropeLength;

            //// Return absolute new position
            //return pivotPosition + offset;

            default:
                return currentVelocity;
        }
    }
}
