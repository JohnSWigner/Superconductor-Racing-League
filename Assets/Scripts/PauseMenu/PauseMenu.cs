using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;  // Drag the PauseMenuCanvas here.
    [SerializeField] private GameObject gameUI;  // Drag the UICanvas here.

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetButtonDown("Pause"))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void ResumeGame()
    {
        gameUI.SetActive(true);
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;  // Resume time
        isPaused = false;
    }

    private void PauseGame()
    {
        gameUI.SetActive(false);
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;  // Pause time
        isPaused = true;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;  // Reset time in case it's paused
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;  // Reset time
        SceneManager.LoadScene("Menu");  // Replace with your main menu scene name
    }

        // Called when the Controls button is clicked
    public void ShowControls()
    {
        // Show the controls UI or panel (handled in the next step)
        controlsPanel.SetActive(true);
    }

    // Reference to the controls panel
    [Tooltip("The instance of the Controls Panel")]
    public GameObject controlsPanel;

    // Hide the panel when returning to the main menu
    public void HideControls()
    {
        controlsPanel.SetActive(false);
    }

       // Called when the Quit button is clicked
    public void QuitGame()
    {
        Debug.Log("Quitting the game...");  // For testing in the editor
        Application.Quit();  // Only works in a built game
    }
}
