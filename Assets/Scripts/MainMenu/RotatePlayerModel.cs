using UnityEngine;

public class RotatePlayerModel : MonoBehaviour
{
    public float rotationSpeed = 50f;

    void Update()
    {
        // Rotate around the Y-axis at a consistent speed
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
}
