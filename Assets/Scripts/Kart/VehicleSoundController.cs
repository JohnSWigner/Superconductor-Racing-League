using UnityEngine;

public class VehicleSoundController : MonoBehaviour
{
    public AudioSource[] engineAudioSources;  // Array of AudioSources to control
    public float minPitch = 0.8f;  // Idle pitch
    public float maxPitch = 2.0f;  // High-speed pitch
    public float maxSpeed = 20f;   // Adjust based on vehicle speed

    private Rigidbody vehicleRigidbody;

    void Start()
    {
        vehicleRigidbody = GetComponent<Rigidbody>();

        if (engineAudioSources.Length == 0)
        {
            Debug.LogWarning("No AudioSources assigned! Disabling sound control.");
            enabled = false;
        }
    }

    void Update()
    {
        float speed = vehicleRigidbody.linearVelocity.magnitude;

        // Adjust the pitch based on speed (linear interpolation)
        float pitch = Mathf.Lerp(minPitch, maxPitch, speed / maxSpeed);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        foreach (AudioSource audioSource in engineAudioSources)
        {
            if (audioSource != null)
            {
                audioSource.pitch = pitch;

                // Start playing the sound if not already playing
                if (speed > 0.1f && !audioSource.isPlaying)
                {
                    audioSource.Play();
                }
                else if (speed <= 0.1f && audioSource.isPlaying)
                {
                    audioSource.Stop();  // Stop when the vehicle is idle
                }
            }
        }
    }
}
