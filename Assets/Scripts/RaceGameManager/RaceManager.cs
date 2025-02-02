using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;

public class RaceManager : MonoBehaviour
{
    public static RaceManager Instance;

    [Header("Race Settings")]
    public int totalLaps = 3;
    public int numberOfCheckpoints = 0;

    [Header("Countdown Settings")]
    public int preliminaryCounts = 3;
    public float delayBetweenCounts = 1.0f;
    public AudioSource songAudioSource;
    public AudioSource countAudioSource;
    public AudioSource startSignalAudioSource;
    public TextMeshProUGUI countdownText;

    [Header("Finish Line Settings")]
    public Transform finishLine;

    [Header("UI Elements")]
    public TextMeshProUGUI playerPositionText;
    public TextMeshProUGUI lapsLeftText;
    public GameObject playerVehicle;

    [Header("Post-Race Settings")]
    public CinemachineCamera victoryCamera;
    public CinemachineCamera playerCamera;
    public GameObject postRaceCanvas;
    public TextMeshProUGUI victoryText;
    public GameObject playerUI;

    private List<RacerProgress> racers = new List<RacerProgress>();
    public bool raceFinished = false;
    public bool raceStarted = false;
    public string winnerName = "";

    private RacerProgress playerProgress;
    private string[] placeMapping = { "1st", "2nd", "3rd" };

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

        if (playerVehicle != null) 
        {
            playerProgress = playerVehicle.GetComponent<RacerProgress>();
        }

        if (postRaceCanvas != null)
            postRaceCanvas.SetActive(false);

        LockRacers();
        StartCoroutine(RaceCountdown());
    }

    void Update()
    {
        if (playerProgress != null && raceStarted && !raceFinished)
        {
            int playerPosition = GetPlayerPosition();
            UpdatePositionUI(playerPosition);
            UpdateLapsLeftUI();
        }
    }

    private IEnumerator RaceCountdown()
    {
        if (countdownText != null) countdownText.gameObject.SetActive(true);

        for (int i = preliminaryCounts; i > 0; i--)
        {
            if (countAudioSource != null) countAudioSource.Play();
            if (countdownText != null) countdownText.text = i.ToString();
            yield return new WaitForSeconds(delayBetweenCounts);
        }

        if (startSignalAudioSource != null) startSignalAudioSource.Play();
        if (countdownText != null) countdownText.text = "GO!";
        
        yield return new WaitForSeconds(1f);  // Display "GO!" briefly
        if (countdownText != null) countdownText.gameObject.SetActive(false);

        UnlockRacers();
        raceStarted = true;
        songAudioSource.Play();
    }

    private void LockRacers()
    {
        foreach (RacerProgress racer in racers)
        {
            BaseHovercarController controller = racer.GetComponent<BaseHovercarController>();
            if (controller != null)
            {
                controller.enabled = false; // Disable the movement script.
            }

            Rigidbody rb = racer.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
            }
        }
    }

    private void UnlockRacers()
    {
        foreach (RacerProgress racer in racers)
        {
            BaseHovercarController controller = racer.GetComponent<BaseHovercarController>();
            if (controller != null)
            {
                controller.enabled = true; // Disable the movement script.
            }

            racer.GetComponent<BaseHovercarController>().canAccelerate = true;
            Rigidbody rb = racer.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.None;
            }
        }
    }

    private int GetPlayerPosition()
    {
        racers.Sort((r1, r2) => GetRacerProgressValue(r2).CompareTo(GetRacerProgressValue(r1)));
        for (int i = 0; i < racers.Count; i++)
        {
            if (racers[i] == playerProgress)
                return i + 1;
        }
        return racers.Count;
    }

    private void UpdatePositionUI(int position)
    {
        playerPositionText.text = GetPrettyPosition(position);
    }

    private string GetPrettyPosition(int position)
    {
        if (position < 4) 
        {
            return placeMapping[position - 1];
        } 
        else 
        {
            return position + "th";
        }
    }

    private void UpdateLapsLeftUI()
    {
        int lapsLeft = Mathf.Max(totalLaps - playerProgress.lapCount, 0);
        lapsLeftText.text = "Laps Left: " + lapsLeft;
    }

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
            TriggerPostRaceEvents();
        }
    }

    private void TriggerPostRaceEvents()
    {
        LockRacers();
        if (victoryCamera != null && playerVehicle != null && playerCamera != null)
        {
            victoryCamera.gameObject.SetActive(true);
            playerCamera.gameObject.SetActive(false);
        }

        if (postRaceCanvas != null) 
        {
            int playerPosition = GetPlayerPosition();
            postRaceCanvas.SetActive(true);
            if (playerPosition == 1) {
                victoryText.text = "You win";
            }
            else 
            {
                victoryText.text = "you placed " + GetPrettyPosition(playerPosition);
            }
        }
        if (playerUI != null) playerUI.SetActive(false);
    }
}
