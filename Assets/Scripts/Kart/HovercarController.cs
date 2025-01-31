using UnityEngine;

public class StableHovercarController : MonoBehaviour
{
    public float hoverHeight = 3.0f;  
    public float positionAdjustmentSpeed = 10.0f;  
    public float raycastDistance = 10.0f;  
    public LayerMask terrainLayer;

    public float movementSpeed = 10.0f;  // Max speed
    public float acceleration = 5.0f;    // How quickly to accelerate
    public float deceleration = 7.0f;    // How quickly to decelerate
    public float rotationSpeed = 100.0f;

    private float currentSpeed = 0.0f;   // Speed that interpolates over time
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;  // Disable gravity for stability
    }

    void FixedUpdate()
    {
        HandleHovering();
        HandleMovement();
    }

    void HandleHovering()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;

        // Cast a ray downward to detect the terrain
        if (Physics.Raycast(rayOrigin, -transform.up, out hit, raycastDistance, terrainLayer))
        {
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
    }

    void HandleMovement()
    {
        // Get player input
        float input = Input.GetAxis("Vertical");

        // Determine target speed based on input
        float targetSpeed = input * movementSpeed;

        if (input != 0)
        {
            // Smooth acceleration
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime * acceleration);
        }
        else
        {
            // Smooth deceleration when no input is present
            currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.fixedDeltaTime * deceleration);
        }

        // Apply movement and rotation
        rb.linearVelocity = transform.forward * currentSpeed;
        float turn = Input.GetAxis("Horizontal") * rotationSpeed;
        rb.angularVelocity = transform.up * turn * Mathf.Deg2Rad;
    }
}
