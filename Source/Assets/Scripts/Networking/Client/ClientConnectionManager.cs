using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// For managing the connection that the client has to the server.
/// </summary>
public class ClientConnectionManager : MonoBehaviour
{
    public enum ClientConnectionState
    {
        NOT_CONNECTED,
        CONNECTING_WANT_CONNECT,
        CONNECTING_TO_RECIEVE_ACK,
        CONNECTING_TO_CONFIRM_ACK,
        CONNECTED
    }

    /*Prefabs*/
    [SerializeField]
    GameObject errorBoxPrefab;

    /*Events*/
    public event EventHandler<NetworkEventArgs.NetworkStatusUpdateEventArgs> NetworkStatusUpdateEvent;

    /*Connection Related*/
    const float maxWaitForAckTimeout = 2.0f;
    float timeWaitedForAck = 0.0f;

    ClientConnectionState connectionState = ClientConnectionState.NOT_CONNECTED;

    const int noOfPacketsToRecieve = 10; //How many packets to receive per frame.

    IPEndPoint serverEndPoint;
    IPEndPoint clientEndPoint;
    Socket clientSocket;

    Queue<Packets.PacketHeader> packetsToSend = new Queue<Packets.PacketHeader>();

    NetworkEventDispatcher networkEventDispatcher;

    float lastTimeRecievedMessage = -1.0f;

    /*Getters and Setters*/
    public bool IsConnected
    {
        get
        {
            return connectionState == ClientConnectionState.CONNECTED;
        }
    }

    public float LastTimeRecievedMessage { get => lastTimeRecievedMessage; set => lastTimeRecievedMessage = value; }

    /*References*/
    NetworkConfigScript networkConfigScript;

    /// <summary>
    /// Parse server IP and port, and start a socket. Displays error box if parse fails. Throws exceptions for these error boxes too.
    /// </summary>
    void Start()
    {
        var networkConfigObject = GameObject.FindGameObjectWithTag("NetworkConfig");
        networkConfigScript = networkConfigObject == null ? null : networkConfigObject.GetComponent<NetworkConfigScript>();

        /*Parse server IP and port*/
        IPAddress serverIp;
        int serverPort;
        ParseServerIpAndPort(out serverIp, out serverPort);

        /*Initialise the socket*/
        serverEndPoint = new IPEndPoint(serverIp, serverPort);
        try
        {
            clientSocket = new Socket(serverEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.IP);
        }
        catch (Exception e)
        {
            DisplayErrorBox("Failed to create socket, maybe a server is already running?.");
            throw new Exception("Failed to create socket! At ClientConnectionManager. " + e.ToString());
        }

        /*Set reference to nework event dispatcher*/
        this.networkEventDispatcher = GameObject.FindGameObjectWithTag("NetworkEventDispatcher").GetComponent<NetworkEventDispatcher>();

        /* Check if the network event dispatcher isnt null */
        if (networkEventDispatcher == null)
            throw new Exception("NetowrkEventDispatcher not found! at ClientConnectionManager!");

        if (errorBoxPrefab == null)
            throw new Exception("Disconnection UI Box not found! At ClientConnectionManager!");
    }

    /// <summary>
    /// Parse the server IP and port. The port and ip will be set to loopback at port 55123 if no NetworkConfig object is found - i.e. laucnhed from editor.
    /// </summary>
    /// <param name="serverIp"></param>
    /// <param name="serverPort"></param>
    private void ParseServerIpAndPort(out IPAddress serverIp, out int serverPort)
    {
        if (networkConfigScript == null)
        {
            /*If we are in the unity editor - assume we want to connect to ourselves*/
            IPAddress.TryParse("127.0.0.1", out serverIp);
            int.TryParse("55123", out serverPort);
        }
        else
        {
            if (!IPAddress.TryParse(networkConfigScript.IPAddress, out serverIp))
            {
                DisplayErrorBox("Unable to parse IP.");
                throw new Exception("Unable to parse server IP.");
            }

            if (!int.TryParse(networkConfigScript.Port, out serverPort))
            {
                DisplayErrorBox("Unable to parse port.");
                throw new Exception("Unable to parse server port.");
            }
        }
    }

    /// <summary>
    /// Queue a packet that will be send to the server.
    /// </summary>
    /// <param name="packetToSend">Packet, dont send just packet header - should be derived type.</param>
    public void QueuePacketToSendToServer(Packets.PacketHeader packetToSend)
    {
        packetsToSend.Enqueue(packetToSend);
    }

    /// <summary>
    /// Send a packet to the server.
    /// </summary>
    /// <param name="packetToSend">Packet to send. Note: do not send packets just with type header.</param>
    /// <remarks>Do not send packets of type packet header.</remarks>
    void SendPacket(Packets.PacketHeader packetToSend)
    {
        if (packetToSend.timeSent > 0.001f) packetToSend.timeSent = networkEventDispatcher.CurrentTimeRelativeToServer;

        /*Serialize the packet*/
        byte[] packetBytes = NetworkingHelpers.EncodePacketIntoBytes(packetToSend);

        /*Send to server*/
        clientSocket.SendTo(packetBytes, packetBytes.Length, SocketFlags.None, serverEndPoint); //Dont need to bind because we are not a server - we don't expect a specific place to connect to us

        if (packetToSend.ToString() != "Packets.PositionPacket")
            Debug.Log("Client sent packet of type: " + packetToSend.ToString());
    }

    /// <summary>
    /// Recieve packets and send from/to the server
    /// </summary>
    void Update()
    {
        /*Send packets*/
        SendPackets();

        /*Recieve Packets*/
        RecievePackets();
    }

    /// <summary>
    /// Receive a certain amount of packets from the port.
    /// </summary>
    void RecievePackets()
    {
        for (int i = 0; i < noOfPacketsToRecieve; ++i)
        {
            if (clientSocket.Available > 0)
            {
                /*Recieve the packet*/
                EndPoint senderEndPoint;
                int recievedCount = 0;
                bool wasRecieveSuccessful;
                SocketException se;
                byte[] packetBytes = NetworkingHelpers.RecieveBytes(ref clientSocket, out senderEndPoint, out recievedCount, out wasRecieveSuccessful, out se);

                if (!wasRecieveSuccessful) //verify that recieve was successful 
                {
                    Debug.LogWarning("Recieve unsuccessful! at ClientConnectionManager.");
                    continue;
                }

                if (((IPEndPoint)senderEndPoint).Address != serverEndPoint.Address && ((IPEndPoint)senderEndPoint).Port != serverEndPoint.Port) //Verify senders identity
                {
                    Debug.LogWarning("Server was not sender!  at ClientConnectionManager.");
                    continue;
                }

                /*Decode packet*/
                bool wasDecodeSuccessful = false;
                Packets.PacketHeader recievedPacket = NetworkingHelpers.DecodePacketHeaderFromBytes(packetBytes, ref wasDecodeSuccessful);

                if (!wasDecodeSuccessful)
                {
                    Debug.LogWarning("Decoding the packet was unsuccessful!");
                    continue;
                }

                LastTimeRecievedMessage = Time.time;

                /*Perform checks on the packet. This class is responsible for network events specifically*/
                switch (recievedPacket.PacketType)
                {
                    /*A packet for connection*/
                    case Packets.PACKET_TYPE.CONNECTION:
                        Packets.ConnectionPackets.ConnectionStatePacket connStatePacket = (Packets.ConnectionPackets.ConnectionStatePacket)recievedPacket;
                        HandleConnectionPacket(connStatePacket);
                        break;

                    /*Time sync packet*/
                    case Packets.PACKET_TYPE.TIME_SYNC:
                        var timeSyncArgs = new NetworkEventArgs.NetworkStatusUpdateEventArgs(NetworkEventArgs.NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.SERVER_TIME_SYNC);
                        timeSyncArgs.NewServerTime = ((Packets.TimeSyncPacket)recievedPacket).ServerTime;
                        RaiseNetworkEvent(timeSyncArgs);
                        break;

                    /*New player packet*/
                    case Packets.PACKET_TYPE.NEW_PLAYER:
                        /*Inform of a new player connected.*/
                        var newPlayerNetworkEventArgs = new NetworkEventArgs.NetworkStatusUpdateEventArgs(NetworkEventArgs.NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.NEW_PLAYER);
                        newPlayerNetworkEventArgs.NewPlayerID = ((Packets.NewPlayerPacket)recievedPacket).NewPlayerID;
                        RaiseNetworkEvent(newPlayerNetworkEventArgs); //tell everyone that a new player has connected (dual connections/not recieved ack packets should be handled by recipient)
                        Debug.Log("Client: Got packet informing of new player, informed event recipients.");

                        /*Send back new that this packet was recieved.*/
                        var newPlayerAckPacket = new Packets.NewPlayerAcknowledgementPacket(((Packets.NewPlayerPacket)recievedPacket).NewPlayerID);
                        SendPacket(newPlayerAckPacket);
                        Debug.Log("Client: Sent acknowledgement packet.");
                        break;

                    /*Player has disconnected*/
                    case Packets.PACKET_TYPE.PLAYER_DISCONNECTED:
                        /*Inform recipients that a player has disconnected.*/
                        var playerDisconnectedNetworkEventArgs = new NetworkEventArgs.NetworkStatusUpdateEventArgs(NetworkEventArgs.NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.PLAYER_DISCONNECTED);
                        playerDisconnectedNetworkEventArgs.PlayerDisconnectedID = ((Packets.DisconnectedPlayerPacket)recievedPacket).PlayerIDDisconnected;
                        RaiseNetworkEvent(playerDisconnectedNetworkEventArgs);
                        Debug.Log("Client: Got packet inforing of player disconnect.");

                        /*Send back new packet.*/
                        var playerDisconnectAckPacket = new Packets.DisconnectedPlayerAcknowledgemmentPacket(((Packets.DisconnectedPlayerPacket)recievedPacket).PlayerIDDisconnected);
                        SendPacket(playerDisconnectAckPacket);
                        Debug.Log("Client: Sent back acknowledgmenet packet.");
                        break;

                    default:
                        networkEventDispatcher.DispatchEventForPacket(recievedPacket);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Send out packets depending on connection state
    /// </summary>
    void SendPackets()
    {
        switch (connectionState)
        {
            /*Not connected - so try to connect.*/
            case ClientConnectionState.NOT_CONNECTED:
                var disconnectedNetworkStatusArgs = new NetworkEventArgs.NetworkStatusUpdateEventArgs(NetworkEventArgs.NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.DISCONNECTED); //send out an event that current state is disconnected
                RaiseNetworkEvent(disconnectedNetworkStatusArgs);
                connectionState = ClientConnectionState.CONNECTING_WANT_CONNECT;
                break;

            /*We want to connect*/
            case ClientConnectionState.CONNECTING_WANT_CONNECT:
                var connectingStatusArgs = new NetworkEventArgs.NetworkStatusUpdateEventArgs(NetworkEventArgs.NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.CONNECTING); //send out an event that conneciton is attempting to be established
                RaiseNetworkEvent(connectingStatusArgs);
                var WantConnectPacket = new Packets.ConnectionPackets.ConnectionStatePacket();
                WantConnectPacket.ConnectionState = Packets.ConnectionPackets.CONNECTION_STATE.WANT_CONNECT;
                connectionState = ClientConnectionState.CONNECTING_TO_RECIEVE_ACK;
                SendPacket(WantConnectPacket);
                timeWaitedForAck = 0.0f;
                Debug.Log("Client wanting to connect, sent server request. At ClientConnectionManager.");
                break;

            /*Waiting to recieve packet*/
            case ClientConnectionState.CONNECTING_TO_RECIEVE_ACK:
                timeWaitedForAck += Time.deltaTime;
                if (timeWaitedForAck > maxWaitForAckTimeout)
                {
                    //Not recieved ack, send it again
                    connectionState = ClientConnectionState.CONNECTING_WANT_CONNECT;
                    Debug.LogWarning("Client not recieved connection ack from server, trying to send connection request again. At ClientConnectionManager.");
                }
                break;

            /*Need to say that we recieved the acknowledgement.*/
            case ClientConnectionState.CONNECTING_TO_CONFIRM_ACK:
                var confirmConnectionAckPacket = new Packets.ConnectionPackets.ConnectionStatePacket();
                confirmConnectionAckPacket.ConnectionState = Packets.ConnectionPackets.CONNECTION_STATE.ACK_CONNECTION_RESPONSE;
                SendPacket(confirmConnectionAckPacket);
                Debug.Log("Sent connection acknowledgement, setting state to connected and beginning sending stuff. At ClientConnectionManager");
                connectionState = ClientConnectionState.CONNECTED; //Assume that all is well and we are connected.
                break;

            case ClientConnectionState.CONNECTED:
                if (packetsToSend.Count > 0)
                    SendPacket(packetsToSend.Dequeue());
                break;
        }
    }

    /// <summary>
    /// Code for handling connection packets.
    /// </summary>
    /// <param name="connStatePacket"></param>
    void HandleConnectionPacket(Packets.ConnectionPackets.ConnectionStatePacket connStatePacket)
    {
        switch (connectionState)
        {
            /*We are waiting to recieve acknowledgement of connection request*/
            case ClientConnectionState.CONNECTING_TO_RECIEVE_ACK:
                {
                    switch (connStatePacket.ConnectionState)
                    {
                        /*Connection was approved*/
                        case Packets.ConnectionPackets.CONNECTION_STATE.APPROVE_CONNECT:
                            connectionState = ClientConnectionState.CONNECTING_TO_CONFIRM_ACK; //Should confirm that we got the packet
                            Debug.Log("Client: Server approved clients' connection request. At ClientConnectionManager.");
                            
                            /*We're basically connected!*/
                            var connectionApprovedNetworkStatusUpdateArgs = new NetworkEventArgs.NetworkStatusUpdateEventArgs(NetworkEventArgs.NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.CONNECTED);
                            connectionApprovedNetworkStatusUpdateArgs.NewServerTime = ((Packets.ConnectionPackets.ApproveConnectPacket)connStatePacket).ServerTime;
                            connectionApprovedNetworkStatusUpdateArgs.NewPlayerID = ((Packets.ConnectionPackets.ApproveConnectPacket)connStatePacket).AssignedPlayerID;
                            RaiseNetworkEvent(connectionApprovedNetworkStatusUpdateArgs);
                            break;

                        /*Connection was declined*/
                        case Packets.ConnectionPackets.CONNECTION_STATE.DECLINE_CONNECT:
                            Debug.LogWarning("Client: Server refused connection. At ClientConnectionManager.");
                            DisplayErrorBox("Server declined connection - could be full."); //Quit to main menu
                            break;
                    }
                }
                break;
            default:
                Debug.LogError("Unrecognised connection state packet! At ClientConnectionManager.");
                break;
        }
    }

    /// <summary>
    /// Display an error box with message.
    /// </summary>
    /// <param name="disconnectionMsg">Message that the box has.</param>
    void DisplayErrorBox(string disconnectionMsg)
    {
        var box = GameObject.Instantiate(errorBoxPrefab, GameObject.FindGameObjectWithTag("UI").transform);
        box.GetComponent<Button>().gameObject.GetComponentInChildren<Text>().text = disconnectionMsg;
        connectionState = ClientConnectionState.NOT_CONNECTED;
    }

    /// <summary>
    /// Raise a network event.
    /// </summary>
    /// <param name="networkStatusUpdateEventArgs">What sort of event</param>
    void RaiseNetworkEvent(NetworkEventArgs.NetworkStatusUpdateEventArgs networkStatusUpdateEventArgs)
    {
        EventHandler<NetworkEventArgs.NetworkStatusUpdateEventArgs> localEventHandler = NetworkStatusUpdateEvent; //To avoid race conditions (as per MDSN)

        if (localEventHandler == null)
            return; //No subscribers, so can't dispatch event - non fatal

        // Dispatch the event
        localEventHandler(this, networkStatusUpdateEventArgs);
    }
}
