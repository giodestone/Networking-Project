using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class that holds the ID of the player for the network.
/// </summary>
public class PlayerID : MonoBehaviour
{
    [SerializeField] //So you can see in editor what is the assigned ID
    int playerId; 

    /// <summary>
    /// Get/Set the PlayerID of the player. Will return -1 if not yet assigned.
    /// </summary>
    public int PlayerId { get => playerId; set => playerId = value; }
}
