using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ProjectileLogic : MonoBehaviour
{
    private Collider lastDoorTrigger;

    void OnTriggerEnter(Collider other)
    {
        // Remember any trigger tagged "door"
        if (other.CompareTag("door"))
        {
            lastDoorTrigger = other;
            return;
        }

        // If we "hit" (overlap) a trigger tagged "man"
        if (other.CompareTag("man"))
        {
            MakeLastDoorSolid();
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        var col = collision.collider;

        if (col.CompareTag("man"))
        {
            MakeLastDoorSolid();
            Destroy(gameObject);
            return;
        }

        // Any hard surface (non-trigger) => disappear
        if (!col.isTrigger)
        {
            Destroy(gameObject);
        }
    }

    private void MakeLastDoorSolid()
    {
        if (lastDoorTrigger == null) return;

        // Flip collider to solid
        lastDoorTrigger.isTrigger = false;

        // Enable MeshRenderer on the door object
        var renderer = lastDoorTrigger.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }
        else
        {
            // if the collider is on a child, check parent for renderer
            var parentRenderer = lastDoorTrigger.GetComponentInParent<MeshRenderer>();
            if (parentRenderer != null) parentRenderer.enabled = true;
        }

        lastDoorTrigger = null;
    }
}