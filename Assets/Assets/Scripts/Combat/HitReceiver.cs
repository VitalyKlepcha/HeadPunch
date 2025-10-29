using UnityEngine;
using UnityEngine.Rendering.Universal;

/// Receives collision hits on the head and computes damage using punch state flag.
/// Only processes damage if the fist is in an active punch state.
public class HitReceiver : MonoBehaviour
{
    [Header("Damage")] 
    [SerializeField] private float damageScale = 0.18f;    // damage per m/s of punch velocity
    [SerializeField] private float critSpeed = 20f;        // m/s threshold to trigger slow-mo

    [Header("References")] 
    [SerializeField] private HeadHealth health;
    [SerializeField] private CameraFX cameraFx;
    [SerializeField] private ComboTracker comboTracker;             

    [Header("FX Prefabs (optional)")]
    [SerializeField] private ParticleSystem bloodFxPrefab;
    [SerializeField] private GameObject decalPrefab;        // URP Decal Projector prefab

    [Header("Decal Settings")]
    [Tooltip("If assigned, each decal instance will use a fresh clone of this base material.")]
    [SerializeField] private Material decalBaseMaterial;     // Base material to clone per instance
    [Tooltip("Textures to randomly assign to the decal's Base Map per critical hit.")]
    [SerializeField] private Texture2D[] decalTextures;
    [Tooltip("Randomly rotate decal around contact normal for variation.")]
    [SerializeField] private bool randomizeRotation = true;

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
        
        // Register combo hit
        comboTracker?.RegisterHit();

        // Spawn blood particles at contact point (optional)
        if (bloodFxPrefab != null)
            Instantiate(bloodFxPrefab, bestContact.point, Quaternion.LookRotation(bestContact.normal));

        // Only spawn decal on critical hits
        bool isCritical = punchSpeed > critSpeed;
        if (isCritical && decalPrefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(bestContact.normal);
            if (randomizeRotation)
            {
                rot *= Quaternion.AngleAxis(Random.Range(0f, 360f), bestContact.normal);
            }

            var decalGO = Instantiate(decalPrefab, bestContact.point, rot);

            // Try to assign a unique material per instance with a random texture
            var projector = decalGO.GetComponent<DecalProjector>();
            if (projector != null)
            {
                Material sourceMat = decalBaseMaterial != null ? decalBaseMaterial : projector.material;
                if (sourceMat != null)
                {
                    var instancedMat = new Material(sourceMat);

                    // Pick random texture if available
                    Texture2D chosen = null;
                    if (decalTextures != null && decalTextures.Length > 0)
                    {
                        int idx = Random.Range(0, decalTextures.Length);
                        chosen = decalTextures[idx];
                    }

                    if (chosen != null)
                    {
                        if (instancedMat.HasProperty("_BaseMap")) instancedMat.SetTexture("_BaseMap", chosen);
                        if (instancedMat.HasProperty("Base_Map")) instancedMat.SetTexture("Base_Map", chosen);
                    }

                    projector.material = instancedMat;
                }
            }
        }

        // Camera feedback based on punch speed
        cameraFx?.Impulse(Mathf.Clamp01(punchSpeed / 18f));
        if (punchSpeed > critSpeed){
            Debug.Log("SlowMoKick");
            cameraFx?.SlowMoKick();
        }
    }
}


