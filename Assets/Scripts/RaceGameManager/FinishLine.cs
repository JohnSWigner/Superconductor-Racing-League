using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        RacerProgress progress = other.GetComponent<RacerProgress>();
        if (progress != null && RaceManager.Instance != null)
        {
            RaceManager.Instance.CheckFinish(progress);
        }
    }
}
