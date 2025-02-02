using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Tooltip("The order index of this checkpoint along the track.")]
    public int checkpointIndex = 0;

    [Tooltip("The amount of checkpoints the player can skip without penalty.")]
    public int skippableCheckpointsQty = 5;

    private void OnTriggerEnter(Collider other)
    {
        RacerProgress progress = other.GetComponent<RacerProgress>();
        if (progress != null && RaceManager.Instance != null)
        {
            int totalCheckpoints = RaceManager.Instance.numberOfCheckpoints;

            // Check if this checkpoint is within the skippable range or expected next.
            int distanceToCheckpoint = (checkpointIndex - progress.currentCheckpointIndex + totalCheckpoints) % totalCheckpoints;

            if (distanceToCheckpoint == 0 || (distanceToCheckpoint <= skippableCheckpointsQty + 1))
            {
                // Update the current checkpoint index
                progress.currentCheckpointIndex = (checkpointIndex + 1) % totalCheckpoints;

                // If the racer just passed the final checkpoint, increment the lap count.
                if (checkpointIndex == totalCheckpoints - 1)
                {
                    progress.lapCount++;
                }
            }
        }
    }
}
