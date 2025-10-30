using UnityEngine;
using UnityEngine.InputSystem;

/// Applies a forward punch impulse to a fist rigidbody.
public class FistPunch : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody fistRb;           // fist rigidbody
    [SerializeField] private Transform forwardSource;    

    [Header("Input")]
    [SerializeField] private InputActionProperty punchAction; 

    [Header("Punch")]
    [SerializeField] private float punchVelocity = 18f;  // m/s applied as VelocityChange
    [SerializeField] private float cooldown = 0.25f;     // seconds

    [Header("Charge (optional)")]
    [SerializeField] private bool enableCharge = true;
    [SerializeField] private float maxChargeMultiplier = 2.2f; // scales velocity
    [SerializeField] private float fullChargeTime = 0.7f;       // seconds to reach max multiplier

    [Header("Punch State")]
    [SerializeField] private float punchActiveWindow = 0.2f;  // seconds punch is considered active

    private float lastPunchTime = -999f;
    private float pressTime;
    private bool isHeld;
    
    // Ensures only one damage application per punch per fist
    private bool hasDealtDamageThisPunch;
    
    public bool IsPunchActive => (Time.time - LastPunchTime) < punchActiveWindow;
    public float LastPunchVelocity { get; private set; }
    public float LastPunchTime { get; private set; } = -999f;
    
    // Visual feedback (0-1)
    public float ChargeProgress
    {
        get
        {
            if (!enableCharge || !isHeld) return 0f;
            float held = Mathf.Max(0f, Time.time - pressTime);
            return Mathf.Clamp01(held / Mathf.Max(0.0001f, fullChargeTime));
        }
    }

    private void Reset()
    {
        if (fistRb == null) fistRb = GetComponent<Rigidbody>();
        if (forwardSource == null && Camera.main != null) forwardSource = Camera.main.transform;
    }

    private void OnEnable()
    {
        var act = punchAction.action;
        if (act != null)
        {
            act.started += OnPunchStarted;
            act.canceled += OnPunchCanceled;
            act.Enable();
        }
    }

    private void OnDisable()
    {
        var act = punchAction.action;
        if (act != null)
        {
            act.started -= OnPunchStarted;
            act.canceled -= OnPunchCanceled;
            act.Disable();
        }
    }

    private void OnPunchStarted(InputAction.CallbackContext ctx)
    {
        isHeld = true;
        pressTime = Time.time;
        
        // Start charge loop
        if (enableCharge && AudioManager.Instance != null && fistRb != null)
        {
            AudioManager.Instance.StartChargeLoop(fistRb.transform);
        }
        
        TryPunchIfNoCharge();
    }

    private void OnPunchCanceled(InputAction.CallbackContext ctx)
    {
        if (!enableCharge) { isHeld = false; return; }
        
        // Stop charge loop
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopChargeLoop();
        }
        
        float held = Mathf.Max(0f, Time.time - pressTime);
        float t = Mathf.Clamp01(held / Mathf.Max(0.0001f, fullChargeTime));
        float mul = Mathf.Lerp(1f, maxChargeMultiplier, t);
        TryApplyImpulse(mul);
        isHeld = false;
    }

    private void TryPunchIfNoCharge()
    {
        if (enableCharge) return;
        TryApplyImpulse(1f);
    }

    private void TryApplyImpulse(float multiplier)
    {
        if (Time.time < lastPunchTime + cooldown) return;
        if (fistRb == null || forwardSource == null) return;

        lastPunchTime = Time.time;
        float actualVelocity = punchVelocity * Mathf.Max(0.1f, multiplier);
        
        // Store for damage calculation
        LastPunchVelocity = actualVelocity;
        LastPunchTime = Time.time;
        
        // Reset hit token
        hasDealtDamageThisPunch = false;

        Vector3 dir = forwardSource.forward;
        Vector3 dv = dir * actualVelocity;
        fistRb.AddForce(dv, ForceMode.VelocityChange);
    }

    public bool TryConsumeHitThisPunch()
    {
        if (!IsPunchActive) return false;
        if (hasDealtDamageThisPunch) return false;
        hasDealtDamageThisPunch = true;
        return true;
    }
}


