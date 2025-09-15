using UnityEngine;

public class HeadPouncer : MonoBehaviour
{
    public bool withinPouncable = false;
    public PlayerController plyrController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Head") && plyrController.currentState == MovementState.Airborne)
            plyrController.canSecondJump = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Head"))
            plyrController.canSecondJump = false;
    }
}
