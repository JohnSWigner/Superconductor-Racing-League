using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Tooltip("The name of the scene that is loaded when Start button is pressed")]
    public string scene_to_load = "SampleScene";

    // Called when the Start button is clicked
    public void StartGame()
    {
        SceneManager.LoadScene(scene_to_load);
    }

    // Called when the Quit button is clicked
    public void QuitGame()
    {
        Debug.Log("Quitting the game...");  // For testing in the editor
        Application.Quit();  // Only works in a built game
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
}
