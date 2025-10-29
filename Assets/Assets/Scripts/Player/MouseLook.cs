using UnityEngine;
using UnityEngine.InputSystem;


public class MouseLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot; 

    [Header("Sensitivity")]
    [SerializeField] private float sensitivity = 0.14f; // multiplier for mouse delta
    [SerializeField] private float minPitch = -75f;
    [SerializeField] private float maxPitch = 75f;

    [Header("Input")] 
    [SerializeField] private InputActionProperty lookAction; // Vector2

    private float pitch;

    private void OnEnable()
    {
        lookAction.action?.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        lookAction.action?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        Vector2 delta = lookAction.action != null ? lookAction.action.ReadValue<Vector2>() : Vector2.zero;
        Vector2 scaled = delta * sensitivity;

        transform.Rotate(Vector3.up, scaled.x, Space.Self);

        // Pitch on camera pivot
        if (cameraPivot != null)
        {
            pitch = Mathf.Clamp(pitch - scaled.y, minPitch, maxPitch);
            cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }
    }
}


