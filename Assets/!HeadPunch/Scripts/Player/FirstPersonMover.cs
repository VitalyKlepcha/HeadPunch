using UnityEngine;
using UnityEngine.InputSystem;

/// Simple grounded first-person mover using CharacterController.
public class FirstPersonMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.0f;
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundSnap = -2.0f; // small downward force to keep grounded

    [Header("Jump")]
    [SerializeField] private float baseJumpVelocity = 5f;        // Base jump force
    [SerializeField] private float maxJumpMultiplier = 2.5f;     // Max charge multiplier
    [SerializeField] private float fullChargeTime = 1.2f;        // Seconds to reach max charge

    [Header("Input")] 
    [SerializeField] private InputActionProperty moveAction;   // Vector2
    [SerializeField] private InputActionProperty sprintAction;
    [SerializeField] private InputActionProperty jumpAction;   // Button for charging jump 

    private Vector3 velocity;
    private Vector2 moveInput;
    private bool sprintHeld;
    
    // Jump charging state
    private bool jumpHeld;
    private float jumpPressTime;

    private void Reset()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        moveAction.action?.Enable();
        sprintAction.action?.Enable();
        var jumpAct = jumpAction.action;
        if (jumpAct != null)
        {
            jumpAct.started += OnJumpStarted;
            jumpAct.canceled += OnJumpCanceled;
            jumpAct.Enable();
        }
    }

    private void OnDisable()
    {
        moveAction.action?.Disable();
        sprintAction.action?.Disable();
        var jumpAct = jumpAction.action;
        if (jumpAct != null)
        {
            jumpAct.started -= OnJumpStarted;
            jumpAct.canceled -= OnJumpCanceled;
            jumpAct.Disable();
        }
    }
    
    private void OnJumpStarted(InputAction.CallbackContext ctx)
    {
        jumpHeld = true;
        jumpPressTime = Time.time;
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        if (!jumpHeld) return;
        
        // Calculate charge multiplier based on hold time
        float held = Mathf.Max(0f, Time.time - jumpPressTime);
        float t = Mathf.Clamp01(held / Mathf.Max(0.0001f, fullChargeTime));
        float multiplier = Mathf.Lerp(1f, maxJumpMultiplier, t);
        
        TryJump(multiplier);
        jumpHeld = false;
    }
    
    private void TryJump(float multiplier)
    {
        if (controller == null || !controller.isGrounded) return;
        
        float jumpVelocity = baseJumpVelocity * Mathf.Max(0.1f, multiplier);
        velocity.y = jumpVelocity;
        
        Debug.Log($"Jump! Multiplier: {multiplier:F2}, Velocity: {jumpVelocity:F2}");
    }

    private void Update()
    {
        // Cache input in frame time
        moveInput = moveAction.action != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        sprintHeld = sprintAction.action != null && sprintAction.action.IsPressed();
    }

    private void FixedUpdate()
    {
        if (controller == null) return;

        // Convert to world space relative to player yaw
        Vector3 forward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 right = new Vector3(transform.right.x, 0f, transform.right.z).normalized;
        Vector3 desired = (forward * moveInput.y + right * moveInput.x);
        float speed = sprintHeld ? sprintSpeed : moveSpeed;

        controller.Move(desired * speed * Time.fixedDeltaTime);

        // Gravity synced to physics
        if (controller.isGrounded)
        {
            if (velocity.y < 0f) velocity.y = groundSnap; // keep grounded
        }
        else
        {
            velocity.y += gravity * Time.fixedDeltaTime;
        }

        controller.Move(velocity * Time.fixedDeltaTime);
    }
}


