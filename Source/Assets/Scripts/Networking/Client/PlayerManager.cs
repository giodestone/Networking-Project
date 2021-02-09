using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// For managing the players clientside.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();

    public int NumberOfPlayers
    {
        get
        {
            return players.Count();
        }
    }

    bool isLocalPlayerInstanced = false;

    [SerializeField]
    GameObject playerPrefab;

    NetworkEventDispatcher networkEventDispatcher;

    /// <summary>
    /// Setup references, subscribe to event, make sure playerprefab is set.
    /// </summary>
    void Start()
    {
        /*Make sure that there is a player prefab*/
        if (playerPrefab == null)
            throw new Exception("Player prefab is null!");

        networkEventDispatcher = GameObject.FindWithTag("NetworkEventDispatcher").GetComponent<NetworkEventDispatcher>();
        if (networkEventDispatcher == null)
            throw new Exception("Network event dispatcher not found! At PlayerManager.");

        networkEventDispatcher.NetworkStatusUpdateEvent += HandleNetworkStatusUpdateEvent;
    }

    /// <summary>
    /// Handle network status events.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void HandleNetworkStatusUpdateEvent(object sender, NetworkEventArgs.NetworkStatusUpdateEventArgs args)
    {
        switch (args.NetworkStatusEventType)
        {
            /*Instance local player on connect*/
            case NetworkEventArgs.NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.CONNECTED:
                if (!isLocalPlayerInstanced)
                {
                    InstanceLocalPlayer(args.NewPlayerID);
                    isLocalPlayerInstanced = true;
                    Debug.Log("Client: PlayerManager Instanced player on initial connection! Local ID: " + args.NewPlayerID + ".");
                }
                break;

            /*Instance a new network player*/
            case NetworkEventArgs.NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.NEW_PLAYER:
                if (!players.ContainsKey(args.NewPlayerID)) //check if the player exists (as you can get duplicate packets if the acknowledgment gets lost in transit).
                {
                    InstanceNetworkPlayer(args.NewPlayerID);
                    Debug.Log("Client: PlayerManager instanced new networked player with id " + args.NewPlayerID + ".");
                }
                break;
            
            /*Delete a network player.*/
            case NetworkEventArgs.NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.PLAYER_DISCONNECTED:
                if (players.ContainsKey(args.PlayerDisconnectedID)) //if player exists (packet may have been sent before)
                {
#if DEBUG
                    if (args.PlayerDisconnectedID == -1)
                        throw new Exception("Client: PlayerManager misconfigured PlayerDisconnedID in recieved NetworkEvent.");
#endif              
                    //destroy player at the player disconnected ID
                    GameObject.Destroy(players[args.PlayerDisconnectedID]);
                    players.Remove(args.PlayerDisconnectedID);
                }
                break;
        }
    }

    /// <summary>
    /// Instance a new player.
    /// </summary>
    void InstanceNetworkPlayer(int id)
    {
        var playerObj = GameObject.Instantiate(playerPrefab);
        playerObj.tag = "NetworkPlayer";
        playerObj.name = "NetworkPlayer " + id.ToString();
        playerObj.GetComponentInChildren<PlayerID>().PlayerId = id; //Update ID
        playerObj.AddComponent<NetworkPlayerInput>(); //Add network controller.

        players.Add(id, playerObj);
    }

    /// <summary>
    /// Instance the local player.
    /// </summary>
    /// <param name="id"></param>
    void InstanceLocalPlayer(int id)
    {
        var localPlayerObj = GameObject.Instantiate(playerPrefab);
        localPlayerObj.tag = "LocalPlayer";
        localPlayerObj.name = "Local Player (" + id.ToString() + ")";
        localPlayerObj.GetComponentInChildren<PlayerID>().PlayerId = id;
        localPlayerObj.AddComponent<KeyboardPlayerInput>();

        players.Add(id, localPlayerObj);
    }
}
