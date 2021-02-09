using NetworkEventArgs;
using Packets;
using System;
using UnityEngine;

/// <summary>
/// Dispatches game events to the subscribers.
/// </summary>
public class NetworkEventDispatcher : MonoBehaviour
{
    ClientConnectionManager clientConnectionManager;
    PlayerManager playerManager;

    float localTimeConnectedToServer;
    float serverTimeConnectedToServer;

    float lastKnownServerTime;
    float localTimeRecievedLastServerTime;

    /// <summary>
    /// Get the last time the server sent.
    /// </summary>
    public float LastKnownServerTime { get => lastKnownServerTime; }

    /// <summary>
    /// Get the current time accounting for the server time.
    /// </summary>
    public float CurrentTimeRelativeToServer { get { return lastKnownServerTime + (Time.time - localTimeRecievedLastServerTime); } }

    /// <summary>
    /// Is the client connected to the server.
    /// </summary>
    public bool IsConnected
    {
        get
        {
            return clientConnectionManager.IsConnected; //just passes from client connection manager as its more about that
        }
    }

    public float LastTimeRecievedMessageFromServer
    {
        get
        {
            return clientConnectionManager.LastTimeRecievedMessage;
        }
    }

    /*Event handlers for the relevant events*/
    public event EventHandler<PositionPacketEventArgs> PositionPacketRecieveEvent;
    object networkStatusUpdateEventLockObj = new object(); //For avoiding race conditions for accessing NetworkStatusUpdateEvent
    public event EventHandler<NetworkStatusUpdateEventArgs> NetworkStatusUpdateEvent //Accessor for the network event inside of the ClientConnectionManager
    {
        add
        {
            lock (networkStatusUpdateEventLockObj) //So no race conditions happen
            {
                if (clientConnectionManager == null)
                    clientConnectionManager = GameObject.FindGameObjectWithTag("ClientConnectionManager").GetComponent<ClientConnectionManager>();
                clientConnectionManager.NetworkStatusUpdateEvent += value;
            }
        }
        remove
        {
            lock (networkStatusUpdateEventLockObj) //So no race conditions happen
            {
                if (clientConnectionManager == null)
                    clientConnectionManager = GameObject.FindGameObjectWithTag("ClientConnectionManager").GetComponent<ClientConnectionManager>();
                clientConnectionManager.NetworkStatusUpdateEvent -= value;
            }
        }
    }

    /// <summary>
    /// Setup object.
    /// </summary>
    void Start()
    {
        /*Get client connection manager script from parent gameobject*/
        clientConnectionManager = GameObject.FindGameObjectWithTag("ClientConnectionManager").GetComponent<ClientConnectionManager>();

        /*Check if found*/
        if (clientConnectionManager == null)
            throw new Exception("Unable to find ClientConnectionManager script inside of parent! At NetworkEventDispatcher.");

        /*Get player manager*/
        playerManager = GameObject.FindWithTag("PlayerManager").GetComponent<PlayerManager>();

        if (playerManager == null)
            throw new Exception("Unable to find PlayerManager script. At NetworkEventDispatcher (Client).");

        NetworkStatusUpdateEvent += HandleNetworkStatusUpdateEvent;
    }

    /// <summary>
    /// Dipatch an event for the recieved packet.
    /// </summary>
    /// <param name="recievedPacket">The packet that was received.</param>
    public void DispatchEventForPacket(PacketHeader recievedPacket)
    {
        switch (recievedPacket.PacketType)
        {
            case PACKET_TYPE.POSITION:
                RaiseEvent<PositionPacketEventArgs>(PositionPacketRecieveEvent, new PositionPacketEventArgs(recievedPacket as PositionPacket));
                break;
      
            case PACKET_TYPE.ERROR:
                Debug.LogError("Packet sent doesn't have a valid type! at Network Event Dispatcher");
                break;

            default:
                Debug.LogWarning("Packet sent to NetworkEventDispatcher is not recognised! Probably the logic for this packet hasn't been implemented.");
                break;
        }
    }

    /// <summary>
    /// Send a packet to the server.
    /// </summary>
    /// <remarks>
    /// Automatically sets the time sent.
    /// </remarks>
    /// <param name="packet">Packet to send to server.</param>
    public void SendPacket(Packets.PacketHeader packet)
    {
        packet.timeSent = CurrentTimeRelativeToServer;
        clientConnectionManager.QueuePacketToSendToServer(packet);
    }

    /// <summary>
    /// Raise an event with a specific event handler, with event args.
    /// </summary>
    /// <typeparam name="EventArgs">Type Of EventArgs.</typeparam>
    /// <param name="eventHandler">The handler for the type of EventArgs.</param>
    /// <param name="eventArgsToSend">Event args to send.</param>
    void RaiseEvent<EventArgs>(EventHandler<EventArgs> eventHandler, EventArgs eventArgsToSend)
    {
        EventHandler<EventArgs> localEventHandler = eventHandler; //To avoid race conditions (as per MDSN)

        if (localEventHandler == null)
            return; //No subscribers, so can't dispatch event - non fatal

        // Dispatch the event
        localEventHandler(this, eventArgsToSend);
    }

    void HandleNetworkStatusUpdateEvent(object sender, NetworkEventArgs.NetworkStatusUpdateEventArgs args)
    {
        switch (args.NetworkStatusEventType)
        {
            /*Update time if connected to server*/
            case NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.CONNECTED:
                localTimeConnectedToServer = Time.time; //So we know when we established connection.
                serverTimeConnectedToServer = args.NewServerTime;
                goto case NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.SERVER_TIME_SYNC; //Goto the case to update time
            /*Update server time if recieved update.*/
            case NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.SERVER_TIME_SYNC:
                localTimeRecievedLastServerTime = Time.time; //Update when the last time we recieved server time was (it will always be bigger than our time)
                this.lastKnownServerTime = args.NewServerTime; //update the actual server time
                break;
        }
    }
}
