using UnityEngine;

public class MainMenuCameraMove : MonoBehaviour
{
    public Vector3 startPosition;  // Starting position of the camera
    public Vector3 targetPosition;  // Final position (where the player is centered)
    public float moveDuration = 2f;  // How long the camera takes to move

    private float elapsedTime = 0f;
    private bool shouldMove = true;

    void Start()
    {
        // Set the camera’s starting position
        transform.position = startPosition;
    }

    void Update()
    {
        if (shouldMove)
        {
            elapsedTime += Time.deltaTime;

            // Calculate smooth progress using SmoothStep
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / moveDuration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            if (elapsedTime >= moveDuration)
            {
                shouldMove = false;
            }
        }
    }
}
