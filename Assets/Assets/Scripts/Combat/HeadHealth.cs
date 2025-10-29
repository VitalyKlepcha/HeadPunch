using UnityEngine;

/// Tracks head health and exposes damage ratio for visuals.
public class HeadHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    public float DamageRatio => 1f - Mathf.Clamp01(currentHealth / Mathf.Max(0.0001f, maxHealth));

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void AddDamage(float amount)
    {
        if (amount <= 0f) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        // Hook for future: broadcast to material/shader or UI
    }
}


