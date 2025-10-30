using System;
using UnityEngine;

/// Tracks combo hits and exposes current combo count with time-based decay.
public class ComboTracker : MonoBehaviour
{
    [SerializeField] private float comboTimeout = 2.5f;
    [SerializeField] private int tierInterval = 5; // Play tier up sound every N hits (5, 10, 15)

    private int currentCombo;
    private float lastHitTime;
    private bool hasCombo;
    private int lastTierReached = 0; // Track highest tier reached to avoid repeating sounds

    public int ComboCount => currentCombo;
    public bool HasActiveCombo => hasCombo;

    public event Action<int> OnComboChanged;
    public event Action<int> OnComboHit;

    private void Update()
    {
        if (hasCombo && Time.time - lastHitTime > comboTimeout)
        {
            ResetCombo();
        }
    }

    public void RegisterHit()
    {
        currentCombo++;
        lastHitTime = Time.time;
        hasCombo = true;
        
        // Check for tier up (e.g., 5, 10, 15, 20)
        int currentTier = Mathf.FloorToInt(currentCombo / (float)tierInterval);
        if (currentTier > lastTierReached && currentCombo > 0)
        {
            lastTierReached = currentTier;
            
            // Play combo tier up sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayComboTierUp();
            }
        }
        
        OnComboHit?.Invoke(currentCombo);
        OnComboChanged?.Invoke(currentCombo);
    }

    public void ResetCombo()
    {
        if (currentCombo == 0) return;
        
        currentCombo = 0;
        hasCombo = false;
        lastTierReached = 0; // Reset tier tracking
        OnComboChanged?.Invoke(0);
    }
}

