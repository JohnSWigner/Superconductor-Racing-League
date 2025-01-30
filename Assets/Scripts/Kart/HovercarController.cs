using UnityEngine;

public class StableHovercarController : MonoBehaviour
{
    public float hoverHeight = 3.0f;  // Fixed height above terrain
    public float positionAdjustmentSpeed = 10.0f;  // How quickly the vehicle adjusts its position
    public float raycastDistance = 10.0f;  // How far the raycast searches for terrain
    public LayerMask terrainLayer;
    public float movementSpeed = 10.0f;
    public float rotationSpeed = 100.0f;

    public float leanLimit = 10.0f;
    public float leanTime = 100.0f;

    public GameObject playerModel = null;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;  // Disable gravity for stability
    }

    void FixedUpdate()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position;

        // Cast a ray downward to detect the terrain
        if (Physics.Raycast(rayOrigin, -transform.up, out hit, raycastDistance, terrainLayer))
        {
            // Get the mesh and triangle vertices from the hit object
            Mesh mesh = hit.collider.GetComponent<MeshFilter>().mesh;
            int triangleIndex = hit.triangleIndex;
            int vertex1Index = mesh.triangles[triangleIndex * 3 + 0];
            int vertex2Index = mesh.triangles[triangleIndex * 3 + 1];
            int vertex3Index = mesh.triangles[triangleIndex * 3 + 2];

            // Get the vertices of the triangle in world space
            Vector3 localVertex1 = mesh.vertices[vertex1Index];
            Vector3 localVertex2 = mesh.vertices[vertex2Index];
            Vector3 localVertex3 = mesh.vertices[vertex3Index];
            Vector3 worldVertex1 = hit.collider.transform.TransformPoint(localVertex1);
            Vector3 worldVertex2 = hit.collider.transform.TransformPoint(localVertex2);
            Vector3 worldVertex3 = hit.collider.transform.TransformPoint(localVertex3);

            // Interpolate the exact hover point using barycentric coordinates
            Vector3 interpolatedPoint = worldVertex1 * hit.barycentricCoordinate.x +
                                        worldVertex2 * hit.barycentricCoordinate.y +
                                        worldVertex3 * hit.barycentricCoordinate.z;

            // Interpolate the surface normal using barycentric coordinates
            Vector3 localNormal1 = mesh.normals[vertex1Index];
            Vector3 localNormal2 = mesh.normals[vertex2Index];
            Vector3 localNormal3 = mesh.normals[vertex3Index];
            Vector3 worldNormal1 = hit.collider.transform.TransformDirection(localNormal1);
            Vector3 worldNormal2 = hit.collider.transform.TransformDirection(localNormal2);
            Vector3 worldNormal3 = hit.collider.transform.TransformDirection(localNormal3);
            Vector3 interpolatedNormal = worldNormal1 * hit.barycentricCoordinate.x +
                                         worldNormal2 * hit.barycentricCoordinate.y +
                                         worldNormal3 * hit.barycentricCoordinate.z;
            interpolatedNormal.Normalize();  // Ensure the normal is properly normalized

            // Calculate the target hover position above the terrain
            Vector3 targetPosition = interpolatedPoint + interpolatedNormal * hoverHeight;

            // Smoothly move the vehicle to the target hover position
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime * positionAdjustmentSpeed);

            // Align the vehicle's up direction with the interpolated surface normal
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, interpolatedNormal) * transform.rotation;

            // Smoothly rotate the vehicle to align with the surface
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 5.0f);
        }

        // Basic movement controls (WSAD / arrow keys)
        float move = Input.GetAxis("Vertical") * movementSpeed;
        float turn = Input.GetAxis("Horizontal") * rotationSpeed;

        rb.linearVelocity = transform.forward * move;
        rb.angularVelocity = transform.up * turn * Mathf.Deg2Rad;
    }
}
