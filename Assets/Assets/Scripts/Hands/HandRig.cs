using UnityEngine;

/// Creates short spring joints from hand rigidbodies to sockets on the player.
/// Hands are connected to a kinematic player Rigidbody so they move with the body
/// and don't get dragged by world-space target positions.
public class HandRig : MonoBehaviour
{
    [Header("Player Rigidbody (kinematic)")]
    [SerializeField] private Rigidbody playerRb; // Add kinematic RB on PlayerRoot

    [Header("Sockets (children of the player)")]
    [SerializeField] private Transform leftSocket;
    [SerializeField] private Transform rightSocket;

    [Header("Hand Rigidbodies")]
    [SerializeField] private Rigidbody leftHand;
    [SerializeField] private Rigidbody rightHand;

    [Header("FistPunch Components (for charge feedback)")]
    [SerializeField] private FistPunch leftFistPunch;
    [SerializeField] private FistPunch rightFistPunch;

    [Header("Joint Tuning")]
    [SerializeField] private float linearSpring = 1400f;
    [SerializeField] private float linearDamping = 90f;
    [SerializeField] private float maxForce = 20000f;
    [SerializeField] private float slackMeters = 0.1f; // Max distance fist can travel from socket

    [Header("Charge Pullback")]
    [SerializeField] private float maxPullbackDistance = 0.15f; // meters to pull back when fully charged
    private Vector3 leftBaseLocalPos;
    private Vector3 rightBaseLocalPos;

    private ConfigurableJoint leftJoint;
    private ConfigurableJoint rightJoint;
    private Transform forwardSource; // Camera or similar for forward direction

    private void Awake()
    {
        if (playerRb == null)
        {
            // Ensure a kinematic RB exists on the player root
            playerRb = GetComponent<Rigidbody>();
            if (playerRb == null) playerRb = gameObject.AddComponent<Rigidbody>();
            playerRb.isKinematic = true;
        }

        // Find forward source (usually camera) for pullback direction
        if (forwardSource == null && Camera.main != null)
            forwardSource = Camera.main.transform;

        // Auto-find FistPunch components if not assigned
        if (leftFistPunch == null && leftHand != null)
            leftFistPunch = leftHand.GetComponent<FistPunch>();
        if (rightFistPunch == null && rightHand != null)
            rightFistPunch = rightHand.GetComponent<FistPunch>();

        // Cache base local positions of sockets for visual offsets during charge
        if (leftSocket != null) leftBaseLocalPos = leftSocket.localPosition;
        if (rightSocket != null) rightBaseLocalPos = rightSocket.localPosition;

        leftJoint = SetupJoint(leftHand, leftSocket);
        rightJoint = SetupJoint(rightHand, rightSocket);
    }

    private void FixedUpdate()
    {
        // Apply charge pullback by moving sockets locally (stable and intuitive)
        ApplySocketPullback(leftSocket, leftBaseLocalPos, leftFistPunch);
        ApplySocketPullback(rightSocket, rightBaseLocalPos, rightFistPunch);
        
        // Update connectedAnchor each physics step so joints follow sockets as the player rotates
        if (leftJoint != null && leftSocket != null)
            leftJoint.connectedAnchor = playerRb.transform.InverseTransformPoint(leftSocket.position);
        if (rightJoint != null && rightSocket != null)
            rightJoint.connectedAnchor = playerRb.transform.InverseTransformPoint(rightSocket.position);
        
        // Increase reach to compensate for pullback - ensures charged punches reach same distance
        UpdateJointReachForCharge(leftJoint, leftFistPunch);
        UpdateJointReachForCharge(rightJoint, rightFistPunch);
    }

    private void ApplySocketPullback(Transform socket, Vector3 baseLocalPos, FistPunch fistPunch)
    {
        if (socket == null) return;
        if (fistPunch == null)
        {
            socket.localPosition = baseLocalPos;
            return;
        }

        float chargeProgress = fistPunch.ChargeProgress;
        if (chargeProgress <= 0f)
        {
            socket.localPosition = baseLocalPos;
            return;
        }

        // Move the socket backward in its own local space 
        float pullback = chargeProgress * maxPullbackDistance;
        Vector3 localOffset = new Vector3(0f, 0f, -pullback);
        socket.localPosition = baseLocalPos + localOffset;
    }

    /// <summary>
    /// Dynamically increases joint reach limit when charging to compensate for pullback.
    /// This ensures charged punches can reach the same distance as uncharged ones.
    /// </summary>
    private void UpdateJointReachForCharge(ConfigurableJoint joint, FistPunch fistPunch)
    {
        if (joint == null || fistPunch == null) return;

        float chargeProgress = fistPunch.ChargeProgress;
        if (chargeProgress <= 0f)
        {
            // Reset to base reach when not charging
            var baseLimit = new SoftJointLimit { limit = slackMeters };
            joint.linearLimit = baseLimit;
            return;
        }

        // Compensate pullback by increasing reach by the same amount
        float pullbackAmount = chargeProgress * maxPullbackDistance;
        float effectiveReach = slackMeters + pullbackAmount;
        
        var limit = new SoftJointLimit { limit = Mathf.Max(0.001f, effectiveReach) };
        joint.linearLimit = limit;
    }

    private ConfigurableJoint SetupJoint(Rigidbody hand, Transform socket)
    {
        if (hand == null || socket == null) return null;

        // Start at socket position
        hand.position = socket.position;

        var joint = hand.GetComponent<ConfigurableJoint>();
        if (joint == null) joint = hand.gameObject.AddComponent<ConfigurableJoint>();

        joint.connectedBody = playerRb;
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = playerRb.transform.InverseTransformPoint(socket.position);
        joint.anchor = Vector3.zero;

        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;

        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        var limit = new SoftJointLimit { limit = Mathf.Max(0.001f, slackMeters) };
        joint.linearLimit = limit;

        var drive = new JointDrive
        {
            positionSpring = linearSpring,
            positionDamper = linearDamping,
            maximumForce = maxForce
        };
        joint.xDrive = drive;
        joint.yDrive = drive;
        joint.zDrive = drive;

        // We drive to zero because connectedAnchor is set to socket position
        joint.targetPosition = Vector3.zero;

        return joint;
    }
}


