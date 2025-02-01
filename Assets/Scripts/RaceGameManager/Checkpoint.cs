using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Tooltip("The order index of this checkpoint along the track.")]
    public int checkpointIndex = 0;

    private void OnTriggerEnter(Collider other)
    {
        RacerProgress progress = other.GetComponent<RacerProgress>();
        if (progress != null && RaceManager.Instance != null)
        {
            // Only update if the racer is expecting this checkpoint next.
            if (progress.currentCheckpointIndex == checkpointIndex)
            {
                int totalCheckpoints = RaceManager.Instance.numberOfCheckpoints;
                progress.currentCheckpointIndex = (checkpointIndex + 1) % totalCheckpoints;
                // If the racer just passed the final checkpoint, increment the lap count.
                if (checkpointIndex == totalCheckpoints - 1)
                    progress.lapCount++;
            }
        }
    }
}
