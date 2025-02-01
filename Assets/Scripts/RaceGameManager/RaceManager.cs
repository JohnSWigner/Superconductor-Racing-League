using UnityEngine;
using TMPro;  // Import TextMeshPro namespace
using System.Collections.Generic;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    [Header("Race Settings")]
    [Tooltip("Total number of laps needed to finish the race.")]
    public int totalLaps = 3;
    [Tooltip("Total number of checkpoints in the race.")]
    public int numberOfCheckpoints = 0;

    [Header("Finish Line Settings")]
    [Tooltip("Assign the finish line object (with a trigger collider) here.")]
    public Transform finishLine;

    [Header("UI Elements")]
    [Tooltip("Assign the Text UI component to display player's position.")]
    public TextMeshProUGUI playerPositionText;

    [Tooltip("Assign the Text UI component to display laps left.")]
    public TextMeshProUGUI lapsLeftText;

    [Tooltip("Reference to the player's vehicle.")]
    public GameObject playerVehicle;

    private List<RacerProgress> racers = new List<RacerProgress>();
    public bool raceFinished = false;
    public string winnerName = "";

    private RacerProgress playerProgress;

    private string[] placeMapping = { "1st", "2nd", "3rd"};


    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        GameObject[] racerObjects = GameObject.FindGameObjectsWithTag("Racer");
        foreach (GameObject racer in racerObjects)
        {
            RacerProgress progress = racer.GetComponent<RacerProgress>();
            if (progress != null)
            {
                racers.Add(progress);
            }
        }

        if (playerVehicle != null) {
            playerProgress = playerVehicle.GetComponent<RacerProgress>();
        }

        // Initialize UI
        if (playerProgress != null)
            UpdateLapsLeftUI();
    }

    void Update()
    {
        if (playerProgress != null)
        {
            int playerPosition = GetPlayerPosition();
            UpdatePositionUI(playerPosition);
            UpdateLapsLeftUI();  // Continuously update the lap counter
        }
    }

    /// <summary>
    /// Returns the player's current position among all racers.
    /// </summary>
    private int GetPlayerPosition()
    {
        racers.Sort((r1, r2) => GetRacerProgressValue(r2).CompareTo(GetRacerProgressValue(r1)));

        for (int i = 0; i < racers.Count; i++)
        {
            if (racers[i] == playerProgress)
                return i + 1;  // Position is 1-based
        }
        return racers.Count;
    }

    /// <summary>
    /// Updates the UI element to show the player's current race position.
    /// </summary>
    private void UpdatePositionUI(int position)
    {
        if (position < 4) {
            playerPositionText.text = placeMapping[position-1];
        } else {
            playerPositionText.text = position + "th";
        }
    }

    /// <summary>
    /// Updates the laps left UI element based on the player's current progress.
    /// </summary>
    private void UpdateLapsLeftUI()
    {
        int lapsLeft = Mathf.Max(totalLaps - playerProgress.lapCount, 0);
        lapsLeftText.text = "Laps Left: " + lapsLeft;
    }

    /// <summary>
    /// Calculates the progress value for a racer based on laps and checkpoints.
    /// </summary>
    private float GetRacerProgressValue(RacerProgress rp)
    {
        return rp.lapCount * numberOfCheckpoints + rp.currentCheckpointIndex;
    }

    public void CheckFinish(RacerProgress rp)
    {
        if (rp.lapCount >= totalLaps && !raceFinished)
        {
            raceFinished = true;
            winnerName = rp.gameObject.name;
            Debug.Log("Race Finished! Winner: " + winnerName);
        }
    }
}
