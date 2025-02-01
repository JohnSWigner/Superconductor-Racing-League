using UnityEngine;

public class StableHovercarController : BaseHovercarController
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

    [Header("Reset Settings")]
    [Tooltip("Time (in seconds) without track contact before resetting the vehicle.")]
    public float timeBeforeReset = 3.0f;

    [Header("Track Orientation Settings")]
    [Tooltip("Distance to search downward for track (used to align the vehicle’s bottom).")]
    public float trackRaycastDistance = 10.0f;
    [Tooltip("Tag used on the track geometry.")]
    public string trackTag = "Track";

    private float currentSpeed = 0.0f;
    private Rigidbody rb;
    private RacerProgress racerProgress;

    // Timer for how long the raycast has failed to hit ground/track
    private float noContactTimer = 0.0f;
    // Flag that indicates whether a raycast hit was detected in this physics update.
    private bool groundContact = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Disable gravity for hover stability

        racerProgress = GetComponent<RacerProgress>();
        if (racerProgress == null)
        {
            Debug.LogWarning("No RacerProgress component found on this vehicle!");
        }
    }

    void FixedUpdate()
    {
        HandleHovering();
        HandleMovement();

        // If we aren’t detecting ground/track contact, count up.
        if (!groundContact)
        {
            noContactTimer += Time.fixedDeltaTime;
            if (noContactTimer >= timeBeforeReset)
            {
                ResetToCheckpoint();
                noContactTimer = 0.0f; // Reset timer after repositioning
            }
        }
        else
        {
            // Reset the timer when ground/track is detected.
            noContactTimer = 0.0f;
        }
    }

    void HandleHovering()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;

        // Cast a ray downward to detect the terrain (or track geometry).
        if (Physics.Raycast(rayOrigin, -transform.up, out hit, raycastDistance, terrainLayer))
        {
            // We have ground contact.
            groundContact = true;

            // Determine the triangle on the mesh that was hit.
            Mesh mesh = hit.collider.GetComponent<MeshFilter>().mesh;
            int triangleIndex = hit.triangleIndex;
            int vertex1Index = mesh.triangles[triangleIndex * 3 + 0];
            int vertex2Index = mesh.triangles[triangleIndex * 3 + 1];
            int vertex3Index = mesh.triangles[triangleIndex * 3 + 2];

            // Convert the triangle vertices to world space.
            Vector3 worldVertex1 = hit.collider.transform.TransformPoint(mesh.vertices[vertex1Index]);
            Vector3 worldVertex2 = hit.collider.transform.TransformPoint(mesh.vertices[vertex2Index]);
            Vector3 worldVertex3 = hit.collider.transform.TransformPoint(mesh.vertices[vertex3Index]);

            // Interpolate to find the hit point on the triangle.
            Vector3 interpolatedPoint = worldVertex1 * hit.barycentricCoordinate.x +
                                        worldVertex2 * hit.barycentricCoordinate.y +
                                        worldVertex3 * hit.barycentricCoordinate.z;

            // Similarly, interpolate the normals.
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

            // Compute the target hover position.
            Vector3 targetPosition = interpolatedPoint + interpolatedNormal * hoverHeight;
            rb.MovePosition(Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime * positionAdjustmentSpeed));

            // Smoothly rotate the vehicle to align with the terrain.
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, interpolatedNormal) * transform.rotation;
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 5.0f);
        }
        else
        {
            // No ground detected.
            groundContact = false;
        }
    }

    void HandleMovement()
    {
        // Get player input.
        float input = Input.GetAxis("Vertical");

        // Calculate target speed.
        float targetSpeed = input * movementSpeed;

        // Smooth acceleration/deceleration.
        if (input != 0)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime * acceleration);
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.fixedDeltaTime * deceleration);
        }

        // Apply forward movement.
        rb.linearVelocity = transform.forward * currentSpeed;

        // Apply rotation.
        float turn = Input.GetAxis("Horizontal") * rotationSpeed;
        rb.angularVelocity = transform.up * turn * Mathf.Deg2Rad;
    }

    /// <summary>
    /// Resets the vehicle’s position to the last checkpoint reached (from RacerProgress) and orients it so that:
    /// - Its front faces the nearest (other) checkpoint.
    /// - Its bottom is aligned to the track (using a raycast and the specified track tag).
    /// </summary>
    void ResetToCheckpoint()
    {
        // Make sure we have checkpoints.
        if (checkpoints == null || checkpoints.Length == 0)
        {
            Debug.LogWarning("No checkpoints have been assigned in the Inspector.");
            return;
        }
        if (racerProgress == null)
        {
            Debug.LogWarning("RacerProgress component is missing. Cannot reset to checkpoint.");
            return;
        }
        int cpIndex = racerProgress.currentCheckpointIndex;
        if (cpIndex < 0 || cpIndex >= checkpoints.Length)
        {
            Debug.LogWarning("Invalid checkpoint index in RacerProgress!");
            return;
        }

        // Position: use the checkpoint that the racer last reached.
        Transform lastCheckpoint = checkpoints[cpIndex];

        // Find the "other" checkpoint nearest to the last checkpoint.
        // (This will be used to determine the forward direction.)
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

        // Determine desired forward:
        // If we found another checkpoint, aim toward it.
        // (Project the vector onto the plane defined by the desired up.)
        Vector3 desiredForward = (nearestOther != null)
            ? (nearestOther.position - lastCheckpoint.position).normalized
            : transform.forward;

        // Determine the track’s surface normal so we can align the vehicle’s bottom.
        // We cast a ray downward from a point just above the checkpoint.
        Vector3 desiredUp = Vector3.up; // Fallback if no track is detected.
        RaycastHit hit;
        Vector3 rayOrigin = lastCheckpoint.position + Vector3.up * 1.0f;
        if (Physics.Raycast(rayOrigin, -Vector3.up, out hit, trackRaycastDistance))
        {
            if (hit.collider.CompareTag(trackTag))
            {
                // To have the vehicle’s bottom (–transform.up) flush with the track,
                // we set our desired up vector to be the inverse of the track’s normal.
                desiredUp = -hit.normal;
            }
        }

        // Now adjust the desired forward so that it is perpendicular to the desired up.
        desiredForward = Vector3.ProjectOnPlane(desiredForward, desiredUp).normalized;

        // Build the final rotation.
        Quaternion desiredRotation = Quaternion.LookRotation(desiredForward, desiredUp);

        // Reset position and orientation.
        transform.position = lastCheckpoint.position;
        transform.rotation = desiredRotation;

        // Clear any existing motion.
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Debug.Log("Vehicle reset to checkpoint: " + lastCheckpoint.name +
                  (nearestOther != null ? " with front facing: " + nearestOther.name : ""));
    }
}
