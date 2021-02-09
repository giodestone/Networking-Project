using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For passing the network config from the main menu to the running game.
/// </summary>
public class NetworkConfigScript : MonoBehaviour
{
    [SerializeField]
    public string IPAddress = "127.0.0.1";
    [SerializeField]
    public string Port = "55123";
    [SerializeField]
    public bool IsServer;

    void Awake()
    {
        DontDestroyOnLoad(this);
    }
}
