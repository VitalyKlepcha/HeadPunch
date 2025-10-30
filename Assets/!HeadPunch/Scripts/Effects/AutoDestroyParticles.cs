using UnityEngine;

/// Destroys the particle system GameObject after all particles finish playing.
public class AutoDestroyParticles : MonoBehaviour
{
    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps == null) ps = GetComponentInChildren<ParticleSystem>();
    }

    private void Update()
    {
        if (ps != null && !ps.IsAlive())
        {
            Destroy(gameObject);
        }
    }
}

