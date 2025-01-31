using UnityEngine;
using UnityEngine.UI;
using TMPro;  // Import TextMeshPro namespace

public class SpeedometerController : MonoBehaviour
{
    public Image fullSpeedometerImage;      // Reference to the full speedometer UI
    public TextMeshProUGUI speedText;       // Reference to the speed display (TextMeshPro)
    public GameObject playerVehicle;        // Reference to the player's vehicle

    private Rigidbody vehicleRigidbody;     // Reference to the vehicle's Rigidbody
    public float maxSpeed = 10f;          // Maximum speed (adjust as needed)

    void Start()
    {
        // Get the Rigidbody from the assigned player GameObject
        if (playerVehicle != null)
        {
            vehicleRigidbody = playerVehicle.GetComponent<Rigidbody>();
            if (vehicleRigidbody == null)
            {
                Debug.LogError("No Rigidbody found on the player GameObject! Please make sure it has one.");
            }
        }
        else
        {
            Debug.LogError("Player GameObject is not assigned in the Inspector!");
        }
    }

    void Update()
    {
        // Get the vehicle's speed in units per second (you can customize if needed)
        float speed = vehicleRigidbody.linearVelocity.magnitude;  // Convert to km/h if needed

        // Clamp speed to maxSpeed
        float clampedSpeed = Mathf.Clamp(speed, 0, maxSpeed);

        // Update the fill amount (0 to 1)
        fullSpeedometerImage.fillAmount = clampedSpeed / maxSpeed;

        // Update the speed text (e.g., "0V" to "120V")
        speedText.text = Mathf.RoundToInt(clampedSpeed) + "V";
    }
}
