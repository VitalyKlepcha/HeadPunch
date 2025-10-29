using UnityEngine;
using System.Collections;

/// <summary>
/// Camera feedback helper for hits: direct camera shake and slow motion.
/// </summary>
public class CameraFX : MonoBehaviour
{
    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float maxShakeAmount = 0.015f;

    [Header("Slow Motion")]
    [Tooltip("TimeScale over time; x = seconds (unscaled), y = timeScale")] 
    private AnimationCurve slowMoCurve = null;
    [Tooltip("Duration to blend timeScale back to 1.0 after the curve ends")] 
    [SerializeField] private float returnDuration = 0.5f;

    private Vector3 originalLocalPosition;
    private float currentShakeDuration;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
        
        // Ensure slowMoCurve is initialized if null
        if (slowMoCurve == null || slowMoCurve.length < 2)
        {
            slowMoCurve = AnimationCurve.EaseInOut(0f, 0.2f, 0.4f, 0.2f);
        }
    }

    private void Reset()
    {
        // Provide a default curve if none is set
        if (slowMoCurve == null || slowMoCurve.length < 2)
            slowMoCurve = AnimationCurve.EaseInOut(0f, 0.2f, 0.4f, 0.2f);
    }

    private void Update()
    {
        if (currentShakeDuration > 0f)
        {
            currentShakeDuration -= Time.deltaTime;
            if (currentShakeDuration <= 0f)
            {
                transform.localPosition = originalLocalPosition;
                currentShakeDuration = 0f;
            }
            else
            {
                float shakeAmount = (currentShakeDuration / shakeDuration) * maxShakeAmount;
                transform.localPosition = originalLocalPosition + Random.insideUnitSphere * shakeAmount;
            }
        }
    }

    /// <summary>
    /// Triggers camera shake. Typical strength range is 0..1,
    /// but any positive value will work.
    /// </summary>
    public void Impulse(float strength)
    {
        currentShakeDuration = shakeDuration * Mathf.Clamp01(strength);
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
        // Ensure curve is valid - create default if needed
        if (slowMoCurve == null || slowMoCurve.length < 2)
        {
            slowMoCurve = AnimationCurve.EaseInOut(0f, 0.2f, 0.4f, 0.2f);
        }

        float duration = slowMoCurve[slowMoCurve.length - 1].time;

        float t = 0f;
        while (t < duration)
        {
            float scale = slowMoCurve.Evaluate(t);
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


