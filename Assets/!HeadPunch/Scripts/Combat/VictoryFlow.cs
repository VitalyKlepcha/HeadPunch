using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Slow-mo, show UI, restart scene.
public class VictoryFlow : MonoBehaviour
{
    [SerializeField] private HeadHealth headHealth;
    [SerializeField] private CameraFX cameraFx;
    [SerializeField] private HeadUI headUI;
    [SerializeField] private float restartDelay = 1.5f; // seconds

    private bool handled;

    private void Reset()
    {
        if (headHealth == null) headHealth = GetComponent<HeadHealth>();
    }

    private void Awake()
    {
        if (headHealth == null) headHealth = GetComponent<HeadHealth>();
    }

    private void OnEnable()
    {
        if (headHealth != null)
            headHealth.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (headHealth != null)
            headHealth.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (handled) return;
        handled = true;

        cameraFx?.SlowMoKick();
        headUI?.ShowVictory();
        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        float t = 0f;
        while (t < restartDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}


