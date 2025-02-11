using UnityEngine;

public class FreeCamera : MonoBehaviour
{
    [Header("Movement Settings")]
    public float movementSpeed = 10f;            // Base movement speed.
    public float fastMovementMultiplier = 3f;    // Speed multiplier when Shift is held.

    [Header("Look Settings")]
    public float lookSpeed = 2f;                 // Mouse look sensitivity.
    public float maxPitch = 90f;                 // Maximum up/down rotation angle.

    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        // Lock and hide the cursor to keep the view focused.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize yaw and pitch based on the current rotation.
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    /// <summary>
    /// Handles looking around via mouse movement.
    /// </summary>
    void HandleMouseLook()
    {
        // Get mouse inputs.
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        // Update yaw and pitch.
        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        // Apply the rotation.
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// <summary>
    /// Handles movement in the scene.
    /// </summary>
    void HandleMovement()
    {
        Vector3 direction = Vector3.zero;

        // Forward/backward.
        if (Input.GetKey(KeyCode.W))
            direction += transform.forward;
        if (Input.GetKey(KeyCode.S))
            direction -= transform.forward;

        // Left/right.
        if (Input.GetKey(KeyCode.A))
            direction -= transform.right;
        if (Input.GetKey(KeyCode.D))
            direction += transform.right;

        // Up/down.
        if (Input.GetKey(KeyCode.E))
            direction += transform.up;
        if (Input.GetKey(KeyCode.Q))
            direction -= transform.up;

        // Apply a speed boost if Shift is held.
        float speed = movementSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed *= fastMovementMultiplier;

        // Move the camera.
        transform.position += direction * speed * Time.deltaTime;
    }
}
