using System;
using UnityEngine;

/// Tracks head health and exposes damage ratio for visuals.
public class HeadHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Damage Visualization")]
    [SerializeField] private Renderer[] renderers;

    public float DamageRatio => 1f - Mathf.Clamp01(currentHealth / Mathf.Max(0.0001f, maxHealth));
    
    private static readonly int DamagePropertyID = Shader.PropertyToID("_Damage");

    public event Action OnDeath;
    private bool isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
        
        // Auto-find renderers if not assigned
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                Debug.LogWarning($"[HeadHealth] No Renderer components found on {gameObject.name}");
        }
        
        // Update initial damage state
        UpdateDamageVisuals();
    }

    public void AddDamage(float amount)
    {
        if (amount <= 0f) return;
        if (isDead) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        UpdateDamageVisuals();

        if (!isDead && currentHealth <= 0f)
        {
            isDead = true;
            OnDeath?.Invoke();
        }
    }
    
    private void UpdateDamageVisuals()
    {
        float damageRatio = DamageRatio;
        
        if (renderers == null) return;
        
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            
            foreach (var material in renderer.materials)
            {
                if (material.HasProperty(DamagePropertyID))
                {
                    material.SetFloat(DamagePropertyID, damageRatio);
                }
            }
        }
    }
    
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        UpdateDamageVisuals();
    }
}


