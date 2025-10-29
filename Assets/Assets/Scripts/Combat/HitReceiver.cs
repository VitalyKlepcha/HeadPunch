using UnityEngine;

/// Receives collision hits on the head and computes damage using punch state flag.
/// Only processes damage if the fist is in an active punch state.
public class HitReceiver : MonoBehaviour
{
    [Header("Damage")] 
    [SerializeField] private float damageScale = 0.18f;    // damage per m/s of punch velocity
    [SerializeField] private float critSpeed = 15f;        // m/s threshold to trigger slow-mo

    [Header("References")] 
    [SerializeField] private HeadHealth health;
    [SerializeField] private CameraFX cameraFx;             

    [Header("FX Prefabs (optional)")]
    [SerializeField] private ParticleSystem bloodFxPrefab;
    [SerializeField] private GameObject decalPrefab;        // URP Decal Projector prefab

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (health == null) health = GetComponent<HeadHealth>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        // Only first-frame impacts should deal damage
    }

    private void EvaluateCollision(Collision c)
    {
        var other = c.rigidbody;
        if (other == null) return;

        // Only process collisions from fists
        var fistPunch = other.GetComponent<FistPunch>();
        if (fistPunch == null) return;

        // Only apply damage if punch is active (within punch window)
        if (!fistPunch.IsPunchActive) return;

        ContactPoint bestContact = c.GetContact(0);
        
        // Use stored punch velocity directly (bypass all physics readings)
        float punchSpeed = fistPunch.LastPunchVelocity;
        
        Debug.Log($"Punch hit! Speed: {punchSpeed:F2} m/s (Active: {fistPunch.IsPunchActive})");

        // Apply damage based on stored punch velocity
        float dmg = punchSpeed * damageScale;
        health?.AddDamage(dmg);

        // Spawn FX at contact point
        if (bloodFxPrefab != null)
            Instantiate(bloodFxPrefab, bestContact.point, Quaternion.LookRotation(bestContact.normal));
        if (decalPrefab != null)
            Instantiate(decalPrefab, bestContact.point, Quaternion.LookRotation(bestContact.normal));

        // Camera feedback based on punch speed
        cameraFx?.Impulse(Mathf.Clamp01(punchSpeed / 18f));
        if (punchSpeed > critSpeed){
            Debug.Log("SlowMoKick");
            cameraFx?.SlowMoKick();
        }
    }
}


