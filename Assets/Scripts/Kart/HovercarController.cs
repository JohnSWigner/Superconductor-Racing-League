using UnityEngine;

public class StableHovercarController : BaseHovercarController
{
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

    // Timer for how long the raycast has failed to hit ground/track.
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

        // Combine both terrain layers for the raycast.
        LayerMask combinedLayer = smoothTerrainLayer | bumpyTerrainLayer;

        // Cast a ray downward to detect the terrain.
        if (Physics.Raycast(rayOrigin, -transform.up, out hit, raycastDistance, combinedLayer))
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
            Vector3 interpolatedNormal = (worldNormal1 * hit.barycentricCoordinate.x +
                                         worldNormal2 * hit.barycentricCoordinate.y +
                                         worldNormal3 * hit.barycentricCoordinate.z).normalized;

            // Choose the appropriate adjustment speed based on which terrain layer was hit.
            float currentAdjustmentSpeed = smoothPositionAdjustmentSpeed;
            if (IsInLayerMask(hit.collider.gameObject, bumpyTerrainLayer))
            {
                currentAdjustmentSpeed = bumpyPositionAdjustmentSpeed;
            }

            // Compute the target hover position.
            Vector3 targetPosition = interpolatedPoint + interpolatedNormal * hoverHeight;
            rb.MovePosition(Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime * currentAdjustmentSpeed));

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

void ResetToCheckpoint()
{
    // Ensure checkpoints and progress tracking are valid.
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

    // Get the last checkpoint.
    Transform lastCheckpoint = checkpoints[cpIndex];

    // Determine the next checkpoint in sequence.
    int nextCheckpointIndex = (cpIndex + 1) % checkpoints.Length;
    Transform nextCheckpoint = checkpoints[nextCheckpointIndex];

    // Determine desired forward direction toward the next checkpoint.
    Vector3 desiredForward = (nextCheckpoint.position - lastCheckpoint.position).normalized;

    // Determine the track’s surface normal using a raycast.
    Vector3 desiredUp = Vector3.up;  // Fallback if no track surface is detected.
    RaycastHit hit;
    Vector3 rayOrigin = lastCheckpoint.position + Vector3.up * 1.0f;
    if (Physics.Raycast(rayOrigin, -Vector3.up, out hit, trackRaycastDistance))
    {
        if (hit.collider.CompareTag(trackTag))
        {
            desiredUp = -hit.normal;
        }
    }

    // Adjust the forward vector to be perpendicular to the track's surface.
    desiredForward = Vector3.ProjectOnPlane(desiredForward, desiredUp).normalized;

    // Build the final rotation.
    Quaternion desiredRotation = Quaternion.LookRotation(desiredForward, desiredUp);

    // Reset position and orientation.
    transform.position = lastCheckpoint.position;
    transform.rotation = desiredRotation;

    // Clear any existing velocity.
    currentSpeed = 0f;
    rb.linearVelocity = Vector3.zero;
    rb.angularVelocity = Vector3.zero;

    Debug.Log("Vehicle reset to checkpoint: " + lastCheckpoint.name +
              " with front facing: " + nextCheckpoint.name);
}


    // Helper method to determine if a GameObject's layer is in a given LayerMask.
    private bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return ((mask.value & (1 << obj.layer)) != 0);
    }
}
