using UnityEngine;

public class ObjectBanking : MonoBehaviour
{
    [Header("Banking Configuration")]
    [Tooltip("The speed at which the object banks left or right.")]
    public float bankingSpeed = 5f;

    [Tooltip("The maximum angle the object can bank to either side.")]
    public float maxBankingAngle = 30f;

    // Internal variable to keep track of current bank angle
    private float currentBankAngle = 0f;

    void Update()
    {
        // Get input from Horizontal axis (e.g., keyboard or controller)
        float horizontalInput = Input.GetAxis("Horizontal");

        // Target angle is proportional to input and max banking angle
        float targetBankAngle = horizontalInput * maxBankingAngle;

        // Smoothly interpolate current angle towards the target
        currentBankAngle = Mathf.Lerp(currentBankAngle, targetBankAngle, Time.deltaTime * bankingSpeed);

        // Apply the rotation (banking along the Z-axis)
        transform.localRotation = Quaternion.Euler(0f, 0f, -currentBankAngle);
    }
}
