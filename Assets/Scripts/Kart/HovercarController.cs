using TMPro;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements

public class StableHovercarController : BaseHovercarController
{
    [Header("UI")]
    public TextMeshProUGUI wrongWayText;

    [Header("Hover Settings")]
    public float hoverHeight = 3.0f;
    public float raycastDistance = 10.0f;
    
    [Tooltip("Layer mask for smooth terrain.")]
    public LayerMask smoothTerrainLayer;
    [Tooltip("Layer mask for bumpy terrain.")]
    public LayerMask bumpyTerrainLayer;

    [Header("Position Adjustment Speeds")]
    [Tooltip("Adjustment speed when hovering over smooth terrain.")]
    public float smoothPositionAdjustmentSpeed = 10.0f;
    [Tooltip("Adjustment speed when hovering over bumpy terrain.")]
    public float bumpyPositionAdjustmentSpeed = 5.0f;

    [Header("Movement Settings")]
    public float movementSpeed = 10.0f;
    public float acceleration = 5.0f;
    public float deceleration = 7.0f;
    public float rotationSpeed = 100.0f;

    [Header("Reset Settings")]
    [Tooltip("Time (in seconds) without track contact before resetting the vehicle.")]
    public float timeBeforeReset = 3.0f;

    [Header("Track Orientation Settings")]
    [Tooltip("Distance to search downward for track (used to align the vehicle’s bottom).")]
    public float trackRaycastDistance = 10.0f;
    [Tooltip("Tag used on the track geometry.")]
    public string trackTag = "Track";

    public AudioClip collisionSound;
    public AudioSource audioSource;

    private float currentSpeed = 0.0f;
    private Rigidbody rb;
    private RacerProgress racerProgress;

    // Timer for how long the raycast has failed to hit ground/track.
    private float noContactTimer = 0.0f;
    // Flag that indicates whether a raycast hit was detected in this physics update.
    private bool groundContact = false;

    // Timer to track how long the next checkpoint is not in front.
    private float wrongWayTimer = 0.0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Disable gravity for hover stability

        racerProgress = GetComponent<RacerProgress>();
        if (racerProgress == null)
        {
            Debug.LogWarning("No RacerProgress component found on this vehicle!");
        }

        if (wrongWayText != null)
        {
            wrongWayText.enabled = false; // Hide the "Wrong Way" message initially
        }
    }

    void FixedUpdate()
    {
        HandleHovering();
        HandleMovement();
        CheckWrongWay();

        if (!groundContact)
        {
            noContactTimer += Time.fixedDeltaTime;
            if (noContactTimer >= timeBeforeReset)
            {
                ResetToCheckpoint();
                noContactTimer = 0.0f;
            }
        }
        else
        {
            noContactTimer = 0.0f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the collided object has the "Racer" tag
        if (collision.gameObject.CompareTag("Racer"))
        {
            // Play the sound effect
            if (collisionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(collisionSound);
            }
        }
    }

    /// <summary>
    /// Checks whether the next checkpoint is in the vehicle's forward hemisphere.
    /// If it isn’t for longer than one second, the "Wrong Way" warning is displayed.
    /// </summary>
    void CheckWrongWay()
    {
        // Validate that we have checkpoints and progress data.
        if (checkpoints == null || racerProgress == null || checkpoints.Length == 0)
        {
            return;
        }

        // Determine the next checkpoint.
        int nextCheckpointIndex = (racerProgress.currentCheckpointIndex + 1) % checkpoints.Length;
        Transform nextCheckpoint = checkpoints[nextCheckpointIndex];

        // Compute the direction from the vehicle to the next checkpoint.
        Vector3 toNextCheckpoint = (nextCheckpoint.position - transform.position).normalized;

        // Check if the next checkpoint is in front.
        // A positive dot product means the checkpoint is in the forward hemisphere.
        if (Vector3.Dot(transform.forward, toNextCheckpoint) > 0)
        {
            // Checkpoint is in front – reset the timer and hide the warning.
            wrongWayTimer = 0.0f;
            if (wrongWayText != null)
            {
                wrongWayText.enabled = false;
            }
        }
        else
        {
            // Checkpoint is behind; accumulate the timer.
            wrongWayTimer += Time.fixedDeltaTime;
            if (wrongWayTimer >= 1.0f)
            {
                if (wrongWayText != null)
                {
                    wrongWayText.enabled = true;
                }
            }
        }
    }

    void HandleHovering()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;
        LayerMask combinedLayer = smoothTerrainLayer | bumpyTerrainLayer;

        if (Physics.Raycast(rayOrigin, -transform.up, out hit, raycastDistance, combinedLayer))
        {
            groundContact = true;

            Mesh mesh = hit.collider.GetComponent<MeshFilter>().mesh;
            int triangleIndex = hit.triangleIndex;
            int vertex1Index = mesh.triangles[triangleIndex * 3 + 0];
            int vertex2Index = mesh.triangles[triangleIndex * 3 + 1];
            int vertex3Index = mesh.triangles[triangleIndex * 3 + 2];

            Vector3 worldVertex1 = hit.collider.transform.TransformPoint(mesh.vertices[vertex1Index]);
            Vector3 worldVertex2 = hit.collider.transform.TransformPoint(mesh.vertices[vertex2Index]);
            Vector3 worldVertex3 = hit.collider.transform.TransformPoint(mesh.vertices[vertex3Index]);

            Vector3 interpolatedPoint = worldVertex1 * hit.barycentricCoordinate.x +
                                        worldVertex2 * hit.barycentricCoordinate.y +
                                        worldVertex3 * hit.barycentricCoordinate.z;

            Vector3 localNormal1 = mesh.normals[vertex1Index];
            Vector3 localNormal2 = mesh.normals[vertex2Index];
            Vector3 localNormal3 = mesh.normals[vertex3Index];
            Vector3 worldNormal1 = hit.collider.transform.TransformDirection(localNormal1);
            Vector3 worldNormal2 = hit.collider.transform.TransformDirection(localNormal2);
            Vector3 worldNormal3 = hit.collider.transform.TransformDirection(localNormal3);
            Vector3 interpolatedNormal = (worldNormal1 * hit.barycentricCoordinate.x +
                                          worldNormal2 * hit.barycentricCoordinate.y +
                                          worldNormal3 * hit.barycentricCoordinate.z).normalized;

            float currentAdjustmentSpeed = smoothPositionAdjustmentSpeed;
            if (IsInLayerMask(hit.collider.gameObject, bumpyTerrainLayer))
            {
                currentAdjustmentSpeed = bumpyPositionAdjustmentSpeed;
            }

            Vector3 targetPosition = interpolatedPoint + interpolatedNormal * hoverHeight;
            rb.MovePosition(Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime * currentAdjustmentSpeed));

            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, interpolatedNormal) * transform.rotation;
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 5.0f);
        }
        else
        {
            groundContact = false;
        }
    }

    void HandleMovement()
    {
        float input = Input.GetAxis("Vertical");
        if (!canAccelerate)
        {
            input = 0;
        }

        float targetSpeed = input * movementSpeed;
        if (input != 0)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime * acceleration);
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.fixedDeltaTime * deceleration);
        }

        rb.linearVelocity = transform.forward * currentSpeed;

        float turn = Input.GetAxis("Horizontal") * rotationSpeed;
        rb.angularVelocity = transform.up * turn * Mathf.Deg2Rad;
    }

    void ResetToCheckpoint()
    {
        if (checkpoints == null || checkpoints.Length == 0 || racerProgress == null)
        {
            return;
        }

        int cpIndex = racerProgress.currentCheckpointIndex;
        if (cpIndex < 0 || cpIndex >= checkpoints.Length)
        {
            return;
        }

        Transform lastCheckpoint = checkpoints[cpIndex];
        int nextCheckpointIndex = (cpIndex + 1) % checkpoints.Length;
        Transform nextCheckpoint = checkpoints[nextCheckpointIndex];

        Vector3 desiredForward = (nextCheckpoint.position - lastCheckpoint.position).normalized;
        Vector3 desiredUp = Vector3.up;
        RaycastHit hit;
        Vector3 rayOrigin = lastCheckpoint.position + Vector3.up * 1.0f;

        if (Physics.Raycast(rayOrigin, -Vector3.up, out hit, trackRaycastDistance) && hit.collider.CompareTag(trackTag))
        {
            desiredUp = -hit.normal;
        }

        desiredForward = Vector3.ProjectOnPlane(desiredForward, desiredUp).normalized;
        Quaternion desiredRotation = Quaternion.LookRotation(desiredForward, desiredUp);

        transform.position = lastCheckpoint.position;
        transform.rotation = desiredRotation;

        currentSpeed = 0f;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // Helper method to determine if a GameObject's layer is in a given LayerMask.
    private bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return ((mask.value & (1 << obj.layer)) != 0);
    }
}
