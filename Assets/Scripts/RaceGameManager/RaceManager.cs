using UnityEngine;
using System.Collections.Generic;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    [Header("Race Settings")]
    [Tooltip("Total number of laps needed to finish the race.")]
    public int totalLaps = 3;
    [Tooltip("Total number of checkpoints in the race.")]
    public int numberOfCheckpoints = 0; // Set this in the Inspector to match your track setup

    [Header("Finish Line Settings")]
    [Tooltip("Assign the finish line object (with a trigger collider) here.")]
    public Transform finishLine;

    private List<RacerProgress> racers = new List<RacerProgress>();
    public bool raceFinished = false;
    public string winnerName = "";

    void Awake()
    {
        // Basic singleton setup.
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // Optionally, automatically find all racers by tag.
        GameObject[] racerObjects = GameObject.FindGameObjectsWithTag("Racer");
        foreach (GameObject racer in racerObjects)
        {
            RacerProgress progress = racer.GetComponent<RacerProgress>();
            if (progress != null)
                racers.Add(progress);
        }
    }

    /// <summary>
    /// Returns the highest “progress value” among all racers.
    /// (Calculated as: lap count * numberOfCheckpoints + current checkpoint index.)
    /// </summary>
    public float GetLeaderProgress()
    {
        float leaderProgress = 0f;
        foreach (RacerProgress rp in racers)
        {
            float progressValue = rp.lapCount * numberOfCheckpoints + rp.currentCheckpointIndex;
            if (progressValue > leaderProgress)
                leaderProgress = progressValue;
        }
        return leaderProgress;
    }

    /// <summary>
    /// Call this when a racer crosses the finish line.
    /// </summary>
    public void CheckFinish(RacerProgress rp)
    {
        if (rp.lapCount >= totalLaps && !raceFinished)
        {
            raceFinished = true;
            winnerName = rp.gameObject.name;
            Debug.Log("Race Finished! Winner: " + winnerName);
            // Here you might trigger UI updates, stop all racers, etc.
        }
    }
}
