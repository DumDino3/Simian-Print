using UnityEngine;

public class PlayerThrow : MonoBehaviour
{
    [Header("Projectile Settings")]
    public GameObject projectilePrefab;  // prefab to throw
    public float projectileSpeed = 10f;  // throw speed
    public Transform spawnPoint;         // where projectile spawns (e.g. player's hand)

    Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // left click
        {
            ThrowProjectile();
        }
    }

    void ThrowProjectile()
    {
        // get mouse position in world space
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCam.transform.position.z - transform.position.z);
        Vector3 targetPos = mainCam.ScreenToWorldPoint(mousePos);

        // direction from spawn point to target
        Vector3 dir = (targetPos - spawnPoint.position).normalized;

        // spawn projectile
        GameObject proj = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);

        // give velocity (needs Rigidbody)
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = dir * projectileSpeed;
        }
    }
}