using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// For updating the top left sections text and to display if anything has went wrong.
/// </summary>
public class ConnectionStatusTextScript : MonoBehaviour
{
    Text text;
    [SerializeField]
    Text playerCountText;

    [SerializeField]
    Text lastTimeSentMsgText;

    [SerializeField]
    Text disconnectionText;

    PlayerManager playerManager;

    NetworkEventDispatcher networkEventDispatcher;

    /// <summary>
    /// Check if the text refereneces are set and subscribe to network events.
    /// </summary>
    void Start()
    {
        text = GetComponent<Text>();
        GameObject.FindWithTag("NetworkEventDispatcher").GetComponent<NetworkEventDispatcher>().NetworkStatusUpdateEvent += HandleNetworkStatusUpdateEvent; //subscribe to network events

        if (playerCountText == null)
            throw new System.Exception("Player text not set at ConnectionStatusTextScript (should be set to text that will display how many players there are)!");
        if (lastTimeSentMsgText == null)
            throw new System.Exception("Last time connection text not set at ConnectionStatusTextScript (should be set to text that will display how many players there are)!");

        if (disconnectionText == null)
            throw new System.Exception("Disconneciton text is set to null at ConnectionStatusTextScript (should be set to the text that will trigger once no messages from the server are sent in a few seconds)!");

        disconnectionText.enabled = false;
    }

    /// <summary>
    /// Update text based on network event.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void HandleNetworkStatusUpdateEvent(object sender, NetworkEventArgs.NetworkStatusUpdateEventArgs args)
    {
        switch (args.NetworkStatusEventType)
        {
            case NetworkEventArgs.NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.DISCONNECTED:
                text.text = "Not Connected. Going to attempt to connect in a moment...";
                break;

            case NetworkEventArgs.NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.CONNECTING:
                text.text = "Connecting...";
                break;

            case NetworkEventArgs.NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.CONNECTED:
                text.text = "Connected.";
                Invoke("GetPlayerID", 0.25f); //Give a moment for player to get intialised.
                break;

            case NetworkEventArgs.NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.NEW_PLAYER:
            case NetworkEventArgs.NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.PLAYER_DISCONNECTED:
                Invoke("UpdatePlayerCount", 0.25f);
                break;
                
        }
    }

    /// <summary>
    /// Update the time text and check whether too much time has elapsed
    /// </summary>
    private void LateUpdate()
    {
        if (networkEventDispatcher != null)
        {
            lastTimeSentMsgText.text = "Last msg time: " + networkEventDispatcher.LastTimeRecievedMessageFromServer.ToString() + "\n"
                + "Current Time: " + Time.time.ToString() + "\n" 
                + "Last Known Server Time: " + networkEventDispatcher.LastKnownServerTime.ToString() + "\n"
                + "Current time compensated for server time: " + networkEventDispatcher.CurrentTimeRelativeToServer.ToString();

            disconnectionText.enabled = Time.time - networkEventDispatcher.LastTimeRecievedMessageFromServer > Server.Connection.TimeoutTime;
        }
    }

    /// <summary>
    /// Update the positon of the player ID and set component references. Delayed as the player is instanced next frame.
    /// </summary>
    void GetPlayerID()
    {
        text.text += " ID: " + GameObject.FindWithTag("LocalPlayer").GetComponentInChildren<PlayerID>().PlayerId.ToString();
        playerManager = GameObject.FindWithTag("PlayerManager").GetComponent<PlayerManager>();
        networkEventDispatcher = GameObject.Find("NetworkEventDispatcher").GetComponent<NetworkEventDispatcher>();

        if (GameObject.FindGameObjectWithTag("NetworkConfig") != null)
        {
            if (GameObject.FindGameObjectWithTag("NetworkConfig").GetComponent<NetworkConfigScript>().IsServer)
                text.text += " (Server).";
            else
                text.text += ".";
        }
        else
        {
            text.text += " (Server).";
        }

    }

    /// <summary>
    /// Update how many players are connected.
    /// </summary>
    void UpdatePlayerCount()
    {
        playerCountText.text = "Players: " + playerManager.NumberOfPlayers;
    }
}
