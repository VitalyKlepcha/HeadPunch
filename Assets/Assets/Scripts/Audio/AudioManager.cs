using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

/// Central audio manager using AudioMixer with SFX and UI groups.
/// Handles 3D spatial sounds for gameplay and 2D sounds for UI.
[DefaultExecutionOrder(-100)]
public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Clips")]
    [SerializeField] private AudioClip hitLight;
    [SerializeField] private AudioClip hitHeavy;
    [SerializeField] private AudioClip chargeLoop;
    [SerializeField] private AudioClip bloodSplat;
    [SerializeField] private AudioClip comboTierUp;

    [Header("Mixer")]
    [SerializeField] private AudioMixer masterMixer;

    [Header("3D Audio Settings")]
    [SerializeField] private float spatialBlend3D = 1f; // Full 3D spatialization
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 50f;
    [SerializeField] private int maxPooledSources = 10;

    // Internal audio sources
    private Queue<AudioSource> pooledSources3D;
    private AudioSource chargeLoopSource;
    private AudioSource uiSource2D;

    // Mixer groups
    private AudioMixerGroup sfxGroup;
    private AudioMixerGroup uiGroup;

    // Charge loop tracking
    private Transform chargeFollowTarget;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePools();
        FindMixerGroups();
        SetupDedicatedSources();
    }

    private void Update()
    {
        // Update charge loop position if active
        if (chargeLoopSource != null && chargeLoopSource.isPlaying && chargeFollowTarget != null)
        {
            chargeLoopSource.transform.position = chargeFollowTarget.position;
        }
    }

    private void InitializePools()
    {
        pooledSources3D = new Queue<AudioSource>();

        for (int i = 0; i < maxPooledSources; i++)
        {
            GameObject go = new GameObject($"PooledAudioSource_{i}");
            go.transform.SetParent(transform);
            AudioSource source = go.AddComponent<AudioSource>();
            ConfigureSource3D(source);
            source.playOnAwake = false;
            pooledSources3D.Enqueue(source);
        }
    }

    private void FindMixerGroups()
    {
        if (masterMixer == null)
        {
            Debug.LogWarning("[AudioManager] Master Mixer not assigned. Audio will not be routed through mixer.");
            return;
        }

        AudioMixerGroup[] groups = masterMixer.FindMatchingGroups("Master/SFX");
        if (groups != null && groups.Length > 0)
            sfxGroup = groups[0];
        else
            Debug.LogWarning("[AudioManager] SFX group not found in mixer. Check mixer structure: Master/SFX");

        groups = masterMixer.FindMatchingGroups("Master/UI");
        if (groups != null && groups.Length > 0)
            uiGroup = groups[0];
        else
            Debug.LogWarning("[AudioManager] UI group not found in mixer. Check mixer structure: Master/UI");
    }

    private void SetupDedicatedSources()
    {
        // Setup charge loop source (3D, follows transform)
        GameObject chargeGo = new GameObject("ChargeLoopSource");
        chargeGo.transform.SetParent(transform);
        chargeLoopSource = chargeGo.AddComponent<AudioSource>();
        ConfigureSource3D(chargeLoopSource);
        chargeLoopSource.loop = true;
        chargeLoopSource.outputAudioMixerGroup = sfxGroup;

        // Setup UI source (2D, non-spatial)
        GameObject uiGo = new GameObject("UISource");
        uiGo.transform.SetParent(transform);
        uiSource2D = uiGo.AddComponent<AudioSource>();
        uiSource2D.spatialBlend = 0f; // 2D
        uiSource2D.outputAudioMixerGroup = uiGroup;
        uiSource2D.playOnAwake = false;
    }

    private void ConfigureSource3D(AudioSource source)
    {
        source.spatialBlend = spatialBlend3D;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.outputAudioMixerGroup = sfxGroup;
    }

    private AudioSource GetPooledSource()
    {
        if (pooledSources3D.Count > 0)
        {
            AudioSource source = pooledSources3D.Dequeue();
            if (source != null && !source.isPlaying)
                return source;
        }

        // Create emergency source if pool exhausted
        GameObject go = new GameObject("EmergencyAudioSource");
        go.transform.SetParent(transform);
        AudioSource emergency = go.AddComponent<AudioSource>();
        ConfigureSource3D(emergency);
        return emergency;
    }

    private void ReturnToPool(AudioSource source)
    {
        if (source == null) return;
        source.Stop();
        source.clip = null;
        if (pooledSources3D.Count < maxPooledSources)
            pooledSources3D.Enqueue(source);
    }

    public void PlayHitLightAt(Vector3 position)
    {
        if (hitLight == null) return;
        PlayOneShotAt(hitLight, position);
    }

    /// Play heavy hit sound at world position (3D)
    public void PlayHitHeavyAt(Vector3 position)
    {
        if (hitHeavy == null) return;
        PlayOneShotAt(hitHeavy, position);
    }

    /// Play blood splat sound at world position (3D)
    public void PlayBloodSplatAt(Vector3 position)
    {
        if (bloodSplat == null) return;
        PlayOneShotAt(bloodSplat, position);
    }

    /// Start charge loop sound following a transform (3D)
    public void StartChargeLoop(Transform followTransform)
    {
        if (chargeLoop == null || followTransform == null) return;
        if (chargeLoopSource == null) return;

        chargeFollowTarget = followTransform;
        chargeLoopSource.transform.position = followTransform.position;
        chargeLoopSource.clip = chargeLoop;
        chargeLoopSource.Play();
    }

    /// Stop charge loop sound
    public void StopChargeLoop()
    {
        if (chargeLoopSource != null && chargeLoopSource.isPlaying)
        {
            chargeLoopSource.Stop();
            chargeLoopSource.clip = null;
        }
        chargeFollowTarget = null;
    }

 
    public void PlayComboTierUp()
    {
        if (comboTierUp == null || uiSource2D == null) return;
        uiSource2D.PlayOneShot(comboTierUp);
    }

    // Internal helper
    private void PlayOneShotAt(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        AudioSource source = GetPooledSource();
        if (source == null) return;

        source.transform.position = position;
        source.clip = clip;
        source.Play();

        // Return to pool when finished
        StartCoroutine(ReturnToPoolWhenFinished(source, clip.length));
    }

    private System.Collections.IEnumerator ReturnToPoolWhenFinished(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration + 0.1f); // Small buffer
        ReturnToPool(source);
    }
}

