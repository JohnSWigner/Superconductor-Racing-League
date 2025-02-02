using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        RacerProgress progress = other.GetComponent<RacerProgress>();
        if (progress != null && RaceManager.Instance != null)
        {
            // Increment lap count and reset checkpoint index
            if (progress.currentCheckpointIndex == other.GetComponent<BaseHovercarController>().checkpoints.Length)
            {
                progress.lapCount++;
                progress.currentCheckpointIndex = 0;
            }
            

            RaceManager.Instance.CheckFinish(progress);
        }
    }
}
