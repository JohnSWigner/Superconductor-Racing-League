using UnityEngine;

public class AIHovercarController : MonoBehaviour
{
    [Header("Hover Settings")]
    public float hoverHeight = 3.0f;
    public float positionAdjustmentSpeed = 10.0f;
    public float raycastDistance = 10.0f;
    public LayerMask terrainLayer;

    [Header("Movement Settings")]
    public float movementSpeed = 10.0f;  // Max speed
    public float acceleration = 5.0f;    // How quickly to accelerate
    public float deceleration = 7.0f;    // How quickly to decelerate
    public float rotationSpeed = 100.0f;

    [Header("Checkpoint Navigation")]
    [Tooltip("Ordered list of checkpoints for the AI to follow.")]
    public Transform[] checkpoints;
    [Tooltip("Distance from a checkpoint at which the AI switches to the next one.")]
    public float checkpointThreshold = 5.0f;
    private int currentCheckpointIndex = 0;

    [Header("Random Steering Settings")]
    [Tooltip("Probability per second that a random steering offset is applied (0 = never, 1 = every second on average).")]
    public float randomSteeringProbability = 0.2f;
    [Tooltip("Maximum random steering offset added (in normalized input units; e.g., 0.2 means up to ±20% extra steering).")]
    public float randomSteeringMaxOffset = 0.2f;

    [Header("Slowdown Settings")]
    [Tooltip("Distance before a checkpoint at which the AI begins to slow down.")]
    public float slowdownDistance = 10f;
    [Tooltip("Strength of the slowdown (0 = no slowdown, 1 = full slowdown to minimum throttle at the checkpoint).")]
    public float slowdownStrength = 0.5f;

    [Header("Reset Settings")]
    [Tooltip("Time (in seconds) without track contact before resetting the vehicle.")]
    public float timeBeforeReset = 3.0f;

    [Header("Track Orientation Settings")]
    [Tooltip("Distance to search downward for track (used to align the vehicle’s bottom).")]
    public float trackRaycastDistance = 10.0f;
    [Tooltip("Tag used on the track geometry.")]
    public string trackTag = "Track";

    [Header("Avoidance Settings")]
    [Tooltip("Radius within which the AI will try to avoid other racers.")]
    public float avoidanceRadius = 5.0f;
    [Tooltip("Strength of the steering adjustment to avoid collisions.")]
    public float avoidanceSteeringStrength = 0.5f;
    [Tooltip("Throttle multiplier applied when avoiding other racers (0 to 1, where lower means more slowdown).")]
    public float avoidanceSlowdownMultiplier = 0.5f;
    [Tooltip("Tag used to identify other racers for avoidance.")]
    public string racerTag = "Racer";

    private float currentSpeed = 0.0f;
    private Rigidbody rb;

    // Timer for lost ground/track contact
    private float noContactTimer = 0.0f;
    // Flag indicating if the hover raycast hit the terrain/track this physics frame
    private bool groundContact = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    void FixedUpdate()
    {
        HandleHovering();
        HandleMovement();

        // If no ground/track contact is detected, count up the timer.
        if (!groundContact)
        {
            noContactTimer += Time.fixedDeltaTime;
            if (noContactTimer >= timeBeforeReset)
            {
                ResetToCheckpoint();
                noContactTimer = 0.0f; // Reset timer after repositioning.
            }
        }
        else
        {
            // Reset the timer when ground/track contact is re-established.
            noContactTimer = 0.0f;
        }
    }

    /// <summary>
    /// Hover logic: cast a ray downward, interpolate the hit point and normal on the terrain,
    /// and adjust the vehicle's position and rotation so it hovers along the track.
    /// Also sets the groundContact flag for reset timing.
    /// </summary>
    void HandleHovering()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;

        if (Physics.Raycast(rayOrigin, -transform.up, out hit, raycastDistance, terrainLayer))
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
            Vector3 interpolatedNormal = worldNormal1 * hit.barycentricCoordinate.x +
                                         worldNormal2 * hit.barycentricCoordinate.y +
                                         worldNormal3 * hit.barycentricCoordinate.z;
            interpolatedNormal.Normalize();

            Vector3 targetPosition = interpolatedPoint + interpolatedNormal * hoverHeight;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime * positionAdjustmentSpeed);

            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, interpolatedNormal) * transform.rotation;
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 5.0f);
        }
        else
        {
            groundContact = false;
        }
    }

    /// <summary>
    /// Simulates input by computing throttle and steering based on the AI's orientation relative to the next checkpoint.
    /// New additions:
    /// - Random steering offset.
    /// - Throttle reduction when turning sharply and when approaching a checkpoint.
    /// - Avoidance: steer away from and slow down for nearby racers.
    /// </summary>
    void HandleMovement()
    {
        // Default simulated inputs.
        float verticalInput = 1f; // full throttle by default
        float horizontalInput = 0f;

        if (checkpoints != null && checkpoints.Length > 0)
        {
            Transform targetCheckpoint = checkpoints[currentCheckpointIndex];
            Vector3 toCheckpoint = targetCheckpoint.position - transform.position;
            float distanceToCheckpoint = toCheckpoint.magnitude;
            Vector3 toCheckpointNormalized = toCheckpoint.normalized;

            // If the target checkpoint is behind the vehicle, switch to the next one immediately.
            if (Vector3.Dot(transform.forward, toCheckpointNormalized) < 0)
            {
                currentCheckpointIndex = (currentCheckpointIndex + 1) % checkpoints.Length;
            }
            else
            {
                // Compute steering input based on the angle between the vehicle's forward and the direction to the checkpoint.
                Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, transform.up).normalized;
                Vector3 projectedTarget = Vector3.ProjectOnPlane(toCheckpointNormalized, transform.up).normalized;
                float angle = Vector3.SignedAngle(projectedForward, projectedTarget, transform.up);
                horizontalInput = Mathf.Clamp(angle / 45f, -1f, 1f);

                // Add a random steering offset occasionally.
                if (Random.value < randomSteeringProbability * Time.fixedDeltaTime)
                {
                    float randomOffset = Random.Range(-randomSteeringMaxOffset, randomSteeringMaxOffset);
                    horizontalInput += randomOffset;
                    horizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f);
                }

                // Reduce throttle when turning sharply.
                verticalInput = Mathf.Clamp(1f - (Mathf.Abs(horizontalInput) * 0.5f), 0f, 1f);

                // Additional slowdown when approaching a checkpoint.
                if (distanceToCheckpoint < slowdownDistance)
                {
                    float slowdownFactor = Mathf.Lerp(1 - slowdownStrength, 1f, distanceToCheckpoint / slowdownDistance);
                    verticalInput *= slowdownFactor;
                }

                // Switch to the next checkpoint when close enough.
                if (distanceToCheckpoint < checkpointThreshold)
                {
                    currentCheckpointIndex = (currentCheckpointIndex + 1) % checkpoints.Length;
                }
            }
        }

        // --- Avoidance Logic ---
        // Look for nearby racers and, if any are detected, adjust steering and throttle.
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, avoidanceRadius);
        Vector3 avoidanceVector = Vector3.zero;
        int avoidanceCount = 0;
        foreach (Collider col in nearbyColliders)
        {
            if (col.gameObject != gameObject && col.CompareTag(racerTag))
            {
                Vector3 diff = transform.position - col.transform.position;
                if (diff.magnitude > 0)
                {
                    // Closer objects contribute more.
                    avoidanceVector += diff.normalized / diff.magnitude;
                    avoidanceCount++;
                }
            }
        }
        if (avoidanceCount > 0)
        {
            avoidanceVector /= avoidanceCount;
            // Project onto the horizontal plane.
            avoidanceVector = Vector3.ProjectOnPlane(avoidanceVector, transform.up).normalized;
            // Determine steering adjustment from avoidance.
            float avoidanceAngle = Vector3.SignedAngle(transform.forward, avoidanceVector, transform.up);
            float avoidanceInput = Mathf.Clamp(avoidanceAngle / 45f, -1f, 1f) * avoidanceSteeringStrength;
            horizontalInput += avoidanceInput;
            horizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f);
            // Slow down throttle when avoiding.
            verticalInput *= avoidanceSlowdownMultiplier;
        }
        // --- End Avoidance Logic ---

        // Apply movement: accelerate/decelerate toward the target speed, then update velocity and angular velocity.
        float targetSpeed = verticalInput * movementSpeed;
        if (verticalInput != 0)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime * acceleration);
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.fixedDeltaTime * deceleration);
        }

        rb.linearVelocity = transform.forward * currentSpeed;
        float turn = horizontalInput * rotationSpeed;
        rb.angularVelocity = transform.up * turn * Mathf.Deg2Rad;
    }

    /// <summary>
    /// Resets the AI's position to the last checkpoint it passed and reorients it so that:
    /// - Its front faces toward the nearest (other) checkpoint.
    /// - Its bottom is aligned with the nearby track surface (determined via a raycast and track tag).
    /// </summary>
    void ResetToCheckpoint()
    {
        if (checkpoints == null || checkpoints.Length == 0)
        {
            Debug.LogWarning("No checkpoints assigned in the Inspector.");
            return;
        }

        // Determine the last checkpoint passed.
        int lastCheckpointIndex = currentCheckpointIndex - 1;
        if (lastCheckpointIndex < 0)
        {
            lastCheckpointIndex = checkpoints.Length - 1;
        }
        Transform lastCheckpoint = checkpoints[lastCheckpointIndex];

        // Find the nearest other checkpoint (to set the forward direction).
        Transform nearestOther = null;
        float nearestDistance = Mathf.Infinity;
        foreach (Transform cp in checkpoints)
        {
            if (cp == lastCheckpoint)
                continue;
            float d = Vector3.Distance(lastCheckpoint.position, cp.position);
            if (d < nearestDistance)
            {
                nearestDistance = d;
                nearestOther = cp;
            }
        }

        // Determine desired forward direction.
        Vector3 desiredForward = (nearestOther != null)
            ? (nearestOther.position - lastCheckpoint.position).normalized
            : transform.forward;

        // Determine the track’s surface normal to align the vehicle’s bottom.
        // Raycast downward from a point just above the checkpoint.
        Vector3 desiredUp = Vector3.up; // Fallback
        RaycastHit hit;
        Vector3 rayOrigin = lastCheckpoint.position + Vector3.up * 1.0f;
        if (Physics.Raycast(rayOrigin, -Vector3.up, out hit, trackRaycastDistance))
        {
            if (hit.collider.CompareTag(trackTag))
            {
                // Use the inverted hit normal so the vehicle’s bottom (–transform.up) is flush with the track.
                desiredUp = -hit.normal;
            }
        }

        // Adjust desired forward so that it is perpendicular to desired up.
        desiredForward = Vector3.ProjectOnPlane(desiredForward, desiredUp).normalized;

        Quaternion desiredRotation = Quaternion.LookRotation(desiredForward, desiredUp);

        // Reset position and orientation.
        transform.position = lastCheckpoint.position;
        transform.rotation = desiredRotation;

        // Clear any existing motion.
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Debug.Log("AI reset to checkpoint: " + lastCheckpoint.name +
                  (nearestOther != null ? " with front facing: " + nearestOther.name : ""));
    }
}
