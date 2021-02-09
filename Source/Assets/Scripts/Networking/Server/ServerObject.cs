using System;
using UnityEngine;

/// <summary>
/// An object wrapper around the ServerNetworkManager class.
/// </summary>
public class ServerObject : MonoBehaviour
{
    //Serve Config
    [SerializeField]
    public string serverIPStr = "127.0.0.1";
    [SerializeField]
    public string serverSocketStr = "55123";

    [SerializeField]
    GameObject playerPrefab;

    [SerializeField]
    GameObject errorBoxPrefab;

    ServerNetworkManager serverNetworkManager;

    NetworkConfigScript networkConfigScript;

    /// <summary>
    /// Setup the server if the NetworkConfig object doesn't exist, or says that we are.
    /// </summary>
    void Start()
    {
        var networkConfigObj = GameObject.FindGameObjectWithTag("NetworkConfig");
        if (networkConfigObj == null)
        {
            /*If NetworkConfig isnt found the game is being ran from inside editor - so launch the server*/
            networkConfigScript = null;
            SetupServer();
        }
        else if (networkConfigObj != null)
        {
            networkConfigScript = networkConfigObj.GetComponent<NetworkConfigScript>();
            /*Otherwise we are launching from the main menu!*/
            if (networkConfigScript.IsServer)
            {
                /*And a server is supposed to exist!*/
                networkConfigScript = networkConfigObj.GetComponent<NetworkConfigScript>();
                SetupServer();
            }
        }
    }
    
    /// <summary>
    /// Start the server on the specified host.
    /// </summary>
    void SetupServer()
    {
        /*Setup reference to network event dispatcher*/
        var networkEventDispatcherScript = GameObject.FindWithTag("NetworkEventDispatcher").GetComponent<NetworkEventDispatcher>();

        if (networkEventDispatcherScript == null)
        {
            //Serious fault, thorw exception!
            throw new Exception("NetworkEventDispatcher not found or not initialised (in which case change order)! at ServerObject.");
        }

        try
        {
            /*Start server*/
            string ip = "127.0.0.1";
            string port = "55123";

            if (networkConfigScript != null) // Launch with default options if network config isn't found.
            {
                ip = networkConfigScript.IPAddress;
                port = networkConfigScript.Port;
            }

            serverNetworkManager = new ServerNetworkManager(ip, port);
        }
        catch (Exception e)
        {
            var errorBox = Instantiate(errorBoxPrefab, GameObject.FindGameObjectWithTag("UI").transform);
            errorBox.GetComponentInChildren<UnityEngine.UI.Text>().text = "Failed to start server, check IP and port. Maybe a server already exists?";
            throw new Exception("Failed to start server. " + e.ToString());
        }
    }

    /// <summary>
    /// Recieve packets, as long as we are allowed to do so.
    /// </summary>
    void Update()
    {
        if (networkConfigScript != null && networkConfigScript.IsServer)
            serverNetworkManager.ProcessNetwork();
        else if (networkConfigScript == null)
            serverNetworkManager.ProcessNetwork();

    }

    void OnDestroy()
    {
        //TODO: Graceful disconnect
    }
}
