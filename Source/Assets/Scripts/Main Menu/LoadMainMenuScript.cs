using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Contains a funciton to load the main menu, just for a button.
/// </summary>
public class LoadMainMenuScript : MonoBehaviour
{
    public void LoadMainMenu()
    {
        var networkConfigObj = GameObject.FindGameObjectWithTag("NetworkConfig");
        if (networkConfigObj != null)
            Destroy(networkConfigObj); // Destroy network config as it will be loaded again in the main menu.

        SceneManager.LoadScene("Scenes/Main Menu");
    }
}
