using System;
using UnityEngine;

/// Tracks combo hits and exposes current combo count with time-based decay.
public class ComboTracker : MonoBehaviour
{
    [SerializeField] private float comboTimeout = 2.5f;

    private int currentCombo;
    private float lastHitTime;
    private bool hasCombo;

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
        
        OnComboHit?.Invoke(currentCombo);
        OnComboChanged?.Invoke(currentCombo);
    }

    public void ResetCombo()
    {
        if (currentCombo == 0) return;
        
        currentCombo = 0;
        hasCombo = false;
        OnComboChanged?.Invoke(0);
    }
}

