using UnityEngine;
using Cinemachine;
using System.Collections;

/// <summary>
/// Camera feedback helper for hits: generates Cinemachine impulse
/// and triggers a short time slowdown (slow motion) with smooth recovery.
/// Attach this to the Main Camera (or a child) and assign a
/// CinemachineImpulseSource.
/// </summary>
public class CameraFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineImpulseSource impulse;

    [Header("Slow Motion")]
    [Tooltip("TimeScale over time; x = seconds (unscaled), y = timeScale")] 
    [SerializeField] private AnimationCurve slowMoCurve = null;
    [Tooltip("Duration to blend timeScale back to 1.0 after the curve ends")] 
    [SerializeField] private float returnDuration = 0.25f;

    private void Reset()
    {
        // Provide a default curve if none is set
        if (slowMoCurve == null || slowMoCurve.length < 2)
            slowMoCurve = AnimationCurve.EaseInOut(0f, 1f, 0.15f, 0.3f);
    }

    /// <summary>
    /// Generates a camera impulse. Typical strength range is 0..1,
    /// but any positive value will work.
    /// </summary>
    public void Impulse(float strength)
    {
        if (impulse != null)
            impulse.GenerateImpulse(strength);
    }

    /// <summary>
    /// Triggers a brief slow-motion effect using the configured curve.
    /// </summary>
    public void SlowMoKick()
    {
        StopAllCoroutines();
        StartCoroutine(SlowMoRoutine());
    }

    private IEnumerator SlowMoRoutine()
    {
        float duration = slowMoCurve != null && slowMoCurve.length > 0
            ? slowMoCurve[slowMoCurve.length - 1].time
            : 0.15f;

        float t = 0f;
        while (t < duration)
        {
            float scale = slowMoCurve != null ? slowMoCurve.Evaluate(t) : 0.3f;
            Time.timeScale = Mathf.Clamp(scale, 0.05f, 1f);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        float rt = 0f;
        while (rt < returnDuration)
        {
            Time.timeScale = Mathf.Lerp(Time.timeScale, 1f, rt / Mathf.Max(0.0001f, returnDuration));
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            rt += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}


