using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Contains all the classes for Main Menu UI, as to be called by buttons.
/// </summary>
public class MainMenuButtonEvents : MonoBehaviour
{
    enum WHAT_TO_START
    {
        NOTHING,
        CLIENT,
        SERVER
    }

    [SerializeField]
    GameObject clientPrefab;
    [SerializeField]
    GameObject serverPrefab;

    [SerializeField]
    string defaultIP = "127.0.0.1";
    [SerializeField]
    string defaultPort = "55123";

    bool hasButtonBeenClicked = false;

    /// <summary>
    /// Code for starting the client script.
    /// </summary>
    public void StartClient()
    {
        if (!hasButtonBeenClicked)
        {
            hasButtonBeenClicked = true; // Prevent any button from being clicked again

            /*Set IP and Port to default if they're not set*/
            var ipStr = GameObject.FindGameObjectWithTag("IPInputField").GetComponent<Text>().text == "" ? defaultIP : GameObject.FindGameObjectWithTag("IPInputField").GetComponent<Text>().text;
            var portStr = GameObject.FindGameObjectWithTag("PortInputField").GetComponent<Text>().text == "" ? defaultPort : GameObject.FindGameObjectWithTag("PortInputField").GetComponent<Text>().text;

            /*Set the variable that will be passed to the next scene*/
            GameObject.FindGameObjectWithTag("NetworkConfig").GetComponent<NetworkConfigScript>().IPAddress = ipStr;
            GameObject.FindGameObjectWithTag("NetworkConfig").GetComponent<NetworkConfigScript>().Port = portStr;
            GameObject.FindGameObjectWithTag("NetworkConfig").GetComponent<NetworkConfigScript>().IsServer = false;

            SceneManager.LoadScene("Scenes/Main Scene");
        }
    }

    /// <summary>
    /// Code for the button to start the server.
    /// </summary>
    public void StartServer()
    {
        if (!hasButtonBeenClicked)
        {
            hasButtonBeenClicked = true; // Prevent any button from being clicked again.

            var ipStr = GameObject.FindGameObjectWithTag("IPInputField").GetComponent<Text>().text == "" ? defaultIP : GameObject.FindGameObjectWithTag("IPInputField").GetComponent<Text>().text;
            var portStr = GameObject.FindGameObjectWithTag("PortInputField").GetComponent<Text>().text == "" ? defaultPort : GameObject.FindGameObjectWithTag("PortInputField").GetComponent<Text>().text;

            GameObject.FindGameObjectWithTag("NetworkConfig").GetComponent<NetworkConfigScript>().IPAddress = ipStr;
            GameObject.FindGameObjectWithTag("NetworkConfig").GetComponent<NetworkConfigScript>().Port = portStr;
            GameObject.FindGameObjectWithTag("NetworkConfig").GetComponent<NetworkConfigScript>().IsServer = true;

            SceneManager.LoadScene("Scenes/Main Scene");
        }
    }

    /// <summary>
    /// Code for the exit button.
    /// </summary>
    public void ExitGame()
    {
        Application.Quit();
    }
}
