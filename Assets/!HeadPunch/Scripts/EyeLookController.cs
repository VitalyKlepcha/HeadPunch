using UnityEngine;

/// Rotates two eye transforms to smoothly look at a target (player root)
public class EyeLookController : MonoBehaviour
{
    [Header("Eye Transforms")]
    [SerializeField] private Transform leftEye = null;
    [SerializeField] private Transform rightEye = null;

    [Header("Target")]
    [Tooltip("Player root Transform. If left empty, will try to find GameObject tagged 'Player'.")]
    [SerializeField] private Transform target = null;

    [Header("Smoothing")]
    [Tooltip("Higher values = faster tracking. Typical range 6..14.")]
    [SerializeField] private float rotationLerpSpeed = 10f;

    [Header("Limits (degrees)")]
    [Tooltip("Maximum left/right rotation relative to the eye's forward.")]
    [SerializeField] private float maxYaw = 45f;
    [Tooltip("Maximum up/down rotation relative to the eye's forward.")]
    [SerializeField] private float maxPitch = 30f;

    [Header("Distance Gating")]
    [Tooltip("Under this distance, eye will not track (prevents extreme cross-eyed).")]
    [SerializeField] private float minTrackingDistance = 0.25f;
    [Tooltip("Beyond this distance, eye returns to forward.")]
    [SerializeField] private float maxTrackingDistance = 25f;

    private Quaternion leftEyeInitialLocalRotation;
    private Quaternion rightEyeInitialLocalRotation;

    private Transform[] eyes;
    private Quaternion[] initialLocalRotations;

    private void Awake()
    {
        // Auto-find target by tag if not assigned
        if (target == null)
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) target = tagged.transform;
        }

        eyes = new Transform[2] { leftEye, rightEye };
        initialLocalRotations = new Quaternion[2];

        for (int i = 0; i < eyes.Length; i++)
        {
            if (eyes[i] != null)
            {
                initialLocalRotations[i] = eyes[i].localRotation;
            }
        }
    }

    private void LateUpdate()
    {
        if (eyes == null) return;

        for (int i = 0; i < eyes.Length; i++)
        {
            Transform eye = eyes[i];
            if (eye == null) continue;

            Quaternion baseLocalRotation = initialLocalRotations[i];

            // Determine if we should track
            bool shouldTrack = target != null;
            Vector3 worldTargetPos = shouldTrack ? target.position : eye.position + eye.forward;
            float distanceToTarget = shouldTrack ? Vector3.Distance(eye.position, worldTargetPos) : Mathf.Infinity;

            if (!shouldTrack || distanceToTarget < minTrackingDistance || distanceToTarget > maxTrackingDistance)
            {
                Quaternion desired = baseLocalRotation;
                eye.localRotation = SmoothRotate(eye.localRotation, desired, rotationLerpSpeed);
                continue;
            }

            // Compute target direction
            Transform parent = eye.parent;
            if (parent == null)
            {
                Quaternion desiredWorld = ComputeClampedWorldRotation(eye, worldTargetPos, baseLocalRotation);
                eye.rotation = SmoothRotate(eye.rotation, desiredWorld, rotationLerpSpeed);
                continue;
            }

            Vector3 targetPosParentSpace = parent.InverseTransformPoint(worldTargetPos);
            Vector3 eyePosParentSpace = eye.localPosition; // already in parent space
            Vector3 dirParent = (targetPosParentSpace - eyePosParentSpace).normalized;

            // Convert to yaw (around Y) and pitch (around X) in degrees
            float yawDeg = Mathf.Atan2(dirParent.x, dirParent.z) * Mathf.Rad2Deg; 
            float pitchDeg = Mathf.Atan2(-dirParent.y, Mathf.Sqrt(dirParent.x * dirParent.x + dirParent.z * dirParent.z)) * Mathf.Rad2Deg; // up/down

            // Clamp to limits
            yawDeg = Mathf.Clamp(yawDeg, -maxYaw, maxYaw);
            pitchDeg = Mathf.Clamp(pitchDeg, -maxPitch, maxPitch);

            Quaternion desiredLocal = baseLocalRotation * Quaternion.Euler(pitchDeg, yawDeg, 0f);

            // Rotate in local space
            eye.localRotation = SmoothRotate(eye.localRotation, desiredLocal, rotationLerpSpeed);
        }
    }

    private static Quaternion SmoothRotate(Quaternion current, Quaternion target, float speed)
    {
        float t = 1f - Mathf.Exp(-Mathf.Max(0f, speed) * Time.deltaTime);
        return Quaternion.Slerp(current, target, t);
    }

    private Quaternion ComputeClampedWorldRotation(Transform eye, Vector3 worldTarget, Quaternion baseLocal)
    {
        Quaternion baseWorld = eye.rotation * Quaternion.Inverse(eye.localRotation) * baseLocal;
        Vector3 dirInBase = Quaternion.Inverse(baseWorld) * (worldTarget - eye.position).normalized;

        float yawDeg = Mathf.Atan2(dirInBase.x, dirInBase.z) * Mathf.Rad2Deg;
        float pitchDeg = Mathf.Atan2(-dirInBase.y, Mathf.Sqrt(dirInBase.x * dirInBase.x + dirInBase.z * dirInBase.z)) * Mathf.Rad2Deg;

        yawDeg = Mathf.Clamp(yawDeg, -maxYaw, maxYaw);
        pitchDeg = Mathf.Clamp(pitchDeg, -maxPitch, maxPitch);

        Quaternion desiredInBase = Quaternion.Euler(pitchDeg, yawDeg, 0f);
        return baseWorld * desiredInBase;
    }
}


