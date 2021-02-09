using Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// Server Class, keeps track of connections, relays packets.
/// </summary>
public class ServerNetworkManager
{
    IPAddress serverIPAddress;
    Socket serverSocket;
    IPEndPoint serverIPEndPoint;

    const int maxPacketsToRecieve = 20; // Max packets per update.
    const int maxConnections = 32; // Maximum connections.

    Dictionary<IPEndPoint, Server.Connection> connections = new Dictionary<IPEndPoint, Server.Connection>();
    List<Packets.PacketHeader> packetsToSendToEveryone = new List<PacketHeader>(); // For one time packets (don't care if lost)

    const float TimeSyncInterval = 0.250f; //every 250ms.
    float lastTimeSentTimeSync = -1.0f;

    /// <summary>
    /// Start a server socket with following ardugments.
    /// </summary>
    /// <remarks>
    /// Throws exception if parse failed, or fails to initilaise socket (client will display error box if parse fails).
    /// </remarks>
    /// <param name="serverIpStr"></param>
    /// <param name="portStr"></param>
    public ServerNetworkManager(string serverIpStr, string portStr)
    {
        /* Parse custom IP Address */
        if (!IPAddress.TryParse(serverIpStr, out serverIPAddress))
        {
            FailedStartup("Failed to parse IP Address string.");
            return;
        }

        /* Parse port */
        int port;
        if (!int.TryParse(portStr, out port))
        {
            FailedStartup("Failed to parse server socket string.");
            return;
        }

        /* Try to make IPEndPoint */
        try
        {
            serverIPEndPoint = new IPEndPoint(serverIPAddress, port);
        }
        catch (Exception e)
        {
            FailedStartup("Failed to assign IPEndpoint. " + e.ToString());
            return;
        }

        /* Try to make and bind server socket */
        try
        {
            serverSocket = new Socket(serverIPAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            serverSocket.Bind(serverIPEndPoint);
        }
        catch (Exception e)
        {
            FailedStartup("Failed to bind socket. " + e.ToString());
            return;
        }
    }

    /// <summary>
    /// Run the network update receiving, updating etc. Needs to be called every frame.
    /// </summary>
    public void ProcessNetwork()
    {
        //Recieve some amount of packets
        RecievePackets();

        //Send out packets.
        SendPackets();

        /*Kick players out if they timed out*/
        KickTimedOutPlayers();
    }

    /// <summary>
    /// Mark for disconection players that have timed out.
    /// </summary>
    void KickTimedOutPlayers()
    {
        if (connections.Count > 0)
        {
            var connectionTimedOut = connections.Values.ToList().Find(player => Time.time >= player.LastMessageRecieveTime + Server.Connection.TimeoutTime);
            if (connectionTimedOut != null) //actually one to throw out
            {
                MakeConnectionsAwareOfDisconnect(connectionTimedOut.PlayerID);
                Debug.Log("Server: Removed Client ID: " + connectionTimedOut.PlayerID + " due to timeout.");
                connections.Remove(connectionTimedOut.ClientIp);

            }
        }
    }

    /// <summary>
    /// Send out packets that have to be sent out.
    /// </summary>
    void SendPackets()
    {
        /*Send time sync to everyone if its time to send time sync*/
        if (Time.time >= lastTimeSentTimeSync + TimeSyncInterval)
        {
            Packets.TimeSyncPacket timeSyncPacket = new TimeSyncPacket(Time.time);
            packetsToSendToEveryone.Add(timeSyncPacket);
            lastTimeSentTimeSync = Time.time;
        }

        foreach (var connectionIP in connections.Keys)
        {
            Server.Connection connection = connections[connectionIP];

            switch (connection.ConnectionStatus)
            {
                /*Its connected.*/
                case Server.Connection.CONNECTION_STATUS.CONNECTED:
                    /*Message it any other packets.*/
                    foreach (var packet in packetsToSendToEveryone)
                    {
                        SendPacket(packet, connectionIP);
                    }

                    CheckAwarenessOfOtherPlayers(connectionIP, connection);

                    /*Check if there are any players that have been disconnected that the player has to be made aware of*/
                    DisconnectPlayers(connectionIP, connection);

                    

                    break;

                /*Need to acknowledge the connection request for this connection.*/
                case Server.Connection.CONNECTION_STATUS.TO_SEND_CONNECT_SUCCESS_ACK:
                    Packets.ConnectionPackets.ApproveConnectPacket ackPacket = new Packets.ConnectionPackets.ApproveConnectPacket(AssignPlayerID(connection), Time.time);
                    connection.LastMessageSentTime = Time.time;
                    if (!connection.IsPlayerIDAssigned)
                        ackPacket.AssignedPlayerID = AssignPlayerID(connection);
                    else
                        ackPacket.AssignedPlayerID = connection.PlayerID;
                    connection.ConnectionStatus = Server.Connection.CONNECTION_STATUS.WATITING_FOR_CONNECTION_CONFIRMATION_ACK;
                    SendPacket(ackPacket, connectionIP);
                    Debug.Log("Acknowledging Client " + connectionIP.Address + ":" + connectionIP.Port + " connection request. At ServerNetworkManager.");
                    break;

            }
        }
        packetsToSendToEveryone.Clear();
    }

    /// <summary>
    /// Disconnect any players if there are any to disconnect for this connection.
    /// </summary>
    /// <param name="connectionIP">IP of the connection</param>
    /// <param name="connection">Connection object</param>
    void DisconnectPlayers(IPEndPoint connectionIP, Server.Connection connection)
    {
        if (connection.IsPlayerToDisconnect && (Time.time >= connection.LastTimeSentPlayerAwarenessPacket + Server.Connection.MaxTimeBetweenSendingPlayerAwarenessPacket))
        {
            var playerToDisconnectPacket = new Packets.DisconnectedPlayerPacket(connection.GetIDOfAPlayerToDisconnect());
            SendPacket(playerToDisconnectPacket, connectionIP);
            connection.LastTimeSentPlayerAwarenessPacket = Time.time;
        }
    }

    /// <summary>
    /// Check if the player is aware of all other players.
    /// </summary>
    /// <param name="connectionIP">IP of the connection to check.</param>
    /// <param name="connection">Connection object whose player awareness to check.</param>
    void CheckAwarenessOfOtherPlayers(IPEndPoint connectionIP, Server.Connection connection)
    {
        if (!connection.IsAwareOfAllPlayers && (Time.time >= connection.LastTimeSentPlayerAwarenessPacket + Server.Connection.MaxTimeBetweenSendingPlayerAwarenessPacket)) //if they're not dont spam them!
        {
            /*Get id of player that the conneciton is unaware of*/
            int idOfPlayerUnawareOf = -1;
            foreach (var id in connection.PlayersAwareOf.Keys)
            {
                if (connection.PlayersAwareOf[id] == Server.Connection.PLAYER_AWARENESS_STATUS.UNAWARE)
                {
                    idOfPlayerUnawareOf = id;
                    break;
                }
            }

            if (idOfPlayerUnawareOf == -1)
                throw new Exception("Server: Player already aware of all other players!");

            /*Send a packet to them telling them that theyre unaware.*/
            var newConnectionPacket = new Packets.NewPlayerPacket(idOfPlayerUnawareOf);
            SendPacket(newConnectionPacket, connectionIP);
            connection.LastTimeSentPlayerAwarenessPacket = Time.time;
        }
    }

    /// <summary>
    /// Recieve packets.
    /// </summary>
    void RecievePackets()
    {
        for (int i = 0; i < maxPacketsToRecieve; ++i)
        {
            if (serverSocket.Available > 0) //Check that there is something to recieve
            {
                PacketHeader decodedRecievedPacket;
                EndPoint senderEndPoint;
                bool wasRecieveAndDecodeSuccessful;
                RecieveAndDecodePacket(out senderEndPoint, out decodedRecievedPacket, out wasRecieveAndDecodeSuccessful);
                if (!wasRecieveAndDecodeSuccessful)
                {
                    Debug.LogWarning("Server: Recieve and Decode was unsuccessful!");
                    continue;
                }

                Server.Connection connectionSendTimeToUpdate;
                if (connections.TryGetValue((IPEndPoint)senderEndPoint, out connectionSendTimeToUpdate))
                    connectionSendTimeToUpdate.LastMessageRecieveTime = Time.time;

                /*Check what paccket it was*/
                switch (decodedRecievedPacket.PacketType)
                {
                    /*A packet regarding the connection - new or ack*/
                    case PACKET_TYPE.CONNECTION:
                        ProcessConnectionPacket(decodedRecievedPacket, senderEndPoint);
                        break;

                    /*Acknolidging sending a new player connect*/
                    case PACKET_TYPE.NEW_PLAYER_ACK:
                        Server.Connection connThatSentNewPlayerAck;
                        connections.TryGetValue((IPEndPoint)senderEndPoint, out connThatSentNewPlayerAck);

                        /*Change awareness status.*/
                        bool isAlreadyAware = connThatSentNewPlayerAck.PlayersAwareOf[((NewPlayerAcknowledgementPacket)decodedRecievedPacket).NewPlayerIDRecieved] == Server.Connection.PLAYER_AWARENESS_STATUS.AWARE;

                        if (isAlreadyAware) //if already aware
                        {
                            Debug.LogWarning("Server: Client " + ((IPEndPoint)senderEndPoint).Address + ":" + ((IPEndPoint)senderEndPoint).Port + " already sent acknowledgement of connection. Probably just the network acting up.");
                        }
                        else //if unaware, update the connection because we know its aware.
                        {
                            connThatSentNewPlayerAck.PlayersAwareOf[((NewPlayerAcknowledgementPacket)decodedRecievedPacket).NewPlayerIDRecieved] = Server.Connection.PLAYER_AWARENESS_STATUS.AWARE;
                        }

                        break;

                    /*Acknowledgemnet of player disconnection*/
                    case PACKET_TYPE.PLAYER_DISCONNECTED_ACK:
                        var connThatSendPlayerDisconnectAck = connections[(IPEndPoint)senderEndPoint];

                        /*Delete Player ID*/
                        if (connThatSendPlayerDisconnectAck.PlayersAwareOf.ContainsKey(((Packets.DisconnectedPlayerAcknowledgemmentPacket)decodedRecievedPacket).PlayerIDDisconnectedRecieved))
                        {
                            //if not yet deleted, delete
                            connThatSendPlayerDisconnectAck.PlayersAwareOf.Remove(((Packets.DisconnectedPlayerAcknowledgemmentPacket)decodedRecievedPacket).PlayerIDDisconnectedRecieved);
                        }
                        else
                        {
                            //deleted, but the client sent one - probably a network thingy as last sent ack was sent
                            Debug.LogWarning("Server: Client " + ((IPEndPoint)senderEndPoint).Address + ":" + ((IPEndPoint)senderEndPoint).Port + " already sent acknowledgement of disconnection. Probably just the network acting up.");
                        }
                        break;

                    /*Otherwise its a gameplay related packet*/
                    default:
                        /*Vertify that the client is connected*/
                        if (connections.ContainsKey((IPEndPoint)senderEndPoint))
                        {
                            Server.Connection connection;
                            connections.TryGetValue((IPEndPoint)senderEndPoint, out connection);

                            /* Since handshake isn't guaranteed to arrive, if the client just starts sending just make them connected. */
                            if (connection.ConnectionStatus == Server.Connection.CONNECTION_STATUS.WATITING_FOR_CONNECTION_CONFIRMATION_ACK) //if waiting for ack or connected
                            {
                                //connection.ConnectionStatus = Server.Connection.CONNECTION_STATUS.CONNECTED; //set tstatus to connected and dispatch event
                                SetupNewPlayer(senderEndPoint);
                            }

                            if (connection.ConnectionStatus == Server.Connection.CONNECTION_STATUS.CONNECTED)
                            {
                                /*Process packets that have been recieved from the client.*/
                                packetsToSendToEveryone.Add(decodedRecievedPacket);
                            }
                            else
                            {
                                Debug.LogWarning("Client not confirmed connection yet, dropping sent packet (sent packet was not a confirmation packet). At ServerNetworkManager.");
                                break; //otherwise just ignore it
                            }
                        }
                        else
                        {
                            //If not connected just drop the packet.
                            Debug.LogWarning("Dropped packet from " + ((IPEndPoint)senderEndPoint).Address + ":" + ((IPEndPoint)senderEndPoint).Port + " as they're not connected. At ServerNetworkManager");
                        }
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Process the packet which signals connection of a new player.
    /// </summary>
    /// <param name="decodedRecievedPacket"></param>
    /// <param name="senderEndPoint"></param>
    void ProcessConnectionPacket(PacketHeader decodedRecievedPacket, EndPoint senderEndPoint)
    {
        switch (((Packets.ConnectionPackets.ConnectionStatePacket)decodedRecievedPacket).ConnectionState)
        {
            /*This is a new client thats wants to connect*/
            case Packets.ConnectionPackets.CONNECTION_STATE.WANT_CONNECT:
                /*Verify that there are slots avaliable*/
                if (connections.Count > maxConnections)
                {
                    Packets.ConnectionPackets.ConnectionStatePacket connectionStatePacket = new Packets.ConnectionPackets.ConnectionStatePacket();
                    connectionStatePacket.ConnectionState = Packets.ConnectionPackets.CONNECTION_STATE.DECLINE_CONNECT;
                    SendPacket(connectionStatePacket, (IPEndPoint)senderEndPoint);
                    Debug.LogWarning("Client " + ((IPEndPoint)senderEndPoint).Address + ":" + ((IPEndPoint)senderEndPoint).Port + " sent connection declined, as max clients reached! At ServerNetworkManager.");
                    break;
                }

                /*Check if the client is already connected*/
                if (connections.ContainsKey((IPEndPoint)senderEndPoint))
                {
                    Packets.ConnectionPackets.ConnectionStatePacket connectionStatePacket = new Packets.ConnectionPackets.ConnectionStatePacket();
                    connectionStatePacket.ConnectionState = Packets.ConnectionPackets.CONNECTION_STATE.ERROR_OR_NOT_SET;
                    SendPacket(connectionStatePacket, (IPEndPoint)senderEndPoint);
                    Debug.LogWarning("Client " + ((IPEndPoint)senderEndPoint).Address + ":" + ((IPEndPoint)senderEndPoint).Port + " already connected, sent error packet! At ServerNetworkManager.");
                    break;
                }

                /*Otherwise connect the client*/
                connections.Add((IPEndPoint)senderEndPoint, new Server.Connection((IPEndPoint)senderEndPoint));

                Server.Connection newConnection;
                connections.TryGetValue((IPEndPoint)senderEndPoint, out newConnection);
                newConnection.ConnectionStatus = Server.Connection.CONNECTION_STATUS.TO_SEND_CONNECT_SUCCESS_ACK;
                Debug.Log("Client " + ((IPEndPoint)senderEndPoint).Address + ":" + ((IPEndPoint)senderEndPoint).Port + " requested new connection, approved. At ServerNetworkManager.");
                break;

            /*Client acknowledged connection response*/
            case Packets.ConnectionPackets.CONNECTION_STATE.ACK_CONNECTION_RESPONSE:
                if (connections.ContainsKey((IPEndPoint)senderEndPoint)) //if connection exists
                {
                    Server.Connection conn;
                    connections.TryGetValue((IPEndPoint)senderEndPoint, out conn);
                    if (conn.ConnectionStatus != Server.Connection.CONNECTION_STATUS.CONNECTED) //Setup new player as long as they're not already connected
                        SetupNewPlayer(senderEndPoint);
                    else
                        Debug.LogWarning("Server: Player already connected by the time ACK_CONNECTION_RESPONSE was sent.");
                }
                else
                {
                    Debug.LogWarning("Client didn't ask to connect first... At ServerNetworkManager.");
                }
                break;
        }
    }

    /// <summary>
    /// Assign the connection an ID and notify others that a new player connected.
    /// </summary>
    /// <param name="senderEndPoint"></param>
    void SetupNewPlayer(EndPoint senderEndPoint)
    {
        Server.Connection endPointConnectionObj;
        connections.TryGetValue((IPEndPoint)senderEndPoint, out endPointConnectionObj);
        Debug.Log("Client " + ((IPEndPoint)senderEndPoint).Address + ":" + ((IPEndPoint)senderEndPoint).Port + " acknowledged connection response, is now connected. At ServerNetworkManager.");

        MakeConnectionsAwareOfNewPlayer();

        endPointConnectionObj.LastMessageSentTime = Time.time;
        endPointConnectionObj.ConnectionStatus = Server.Connection.CONNECTION_STATUS.CONNECTED;
    }

    /// <summary>
    /// Receive a packet and decode it into a PacketHeader.
    /// </summary>
    /// <param name="senderEndPoint">Will give out who sent it.</param>
    /// <param name="decodedRecievedPacket">Will give decoded packet.</param>
    /// <param name="recieveDecodeSuccessful">Will give status of whether the decode was successful or not.</param>
    void RecieveAndDecodePacket(out EndPoint senderEndPoint, out PacketHeader decodedRecievedPacket, out bool recieveDecodeSuccessful)
    {
        recieveDecodeSuccessful = true;
        /*Recieve the packet*/
        int recievedCount = 0;
        bool wasRecieveSuccessful;
        SocketException se;
        byte[] packetBytes = NetworkingHelpers.RecieveBytes(ref serverSocket, out senderEndPoint, out recievedCount, out wasRecieveSuccessful, out se);

        if (!wasRecieveSuccessful)
        {
            Debug.LogWarning("Recieve unsuccessful! at ServerNetworkManager.");

            /*Check what exception it was.*/
            switch(se.SocketErrorCode)
            {
                case SocketError.SocketError:
                case SocketError.ConnectionReset:
                    if (senderEndPoint != null && ((IPEndPoint)senderEndPoint).Address != IPAddress.Any) //Disconnect the ruffain
                    {
                        connections[(IPEndPoint)senderEndPoint].ConnectionStatus = Server.Connection.CONNECTION_STATUS.TO_DISCONNECT;
                        if (connections[(IPEndPoint)senderEndPoint].IsPlayerIDAssigned) //only disconnect if they connected
                            MakeConnectionsAwareOfDisconnect(connections[(IPEndPoint)senderEndPoint].PlayerID);
                    }
                    break;
            }

            senderEndPoint = null;
            decodedRecievedPacket = null;
            recieveDecodeSuccessful = false;
            return;
        }

        //Now try to deserialize the serialized packet!
        bool wasDecodeSuccessful = false;

        decodedRecievedPacket = NetworkingHelpers.DecodePacketHeaderFromBytes(packetBytes, ref wasDecodeSuccessful);

        if (!wasDecodeSuccessful)
        {
            Debug.LogWarning("Failed to decode packet! at ServerNetworkManager.");
            senderEndPoint = null;
            decodedRecievedPacket = null;
            recieveDecodeSuccessful = false;
            return;
        }

        if (decodedRecievedPacket.ToString() != "Packets.PositionPacket") //this would just end up spamming console.
            Debug.Log("Server: Got packet. Packet: " + decodedRecievedPacket.ToString() + " at ServerNetworkManager.");
    }

    /// <summary>
    /// Send a packet to the server using UDP. Encodes the packet.
    /// </summary>
    /// <param name="packetToSend"></param>
    /// <param name="clientToSendTo"></param>
    public void SendPacket(Packets.PacketHeader packetToSend, IPEndPoint clientToSendTo)
    {
        packetToSend.timeSent = Time.time;

        connections[clientToSendTo].LastMessageSentTime = Time.time;

        /*Serialize the packet*/
        byte[] packetBytes = NetworkingHelpers.EncodePacketIntoBytes(packetToSend);

        /*Send to server*/
        serverSocket.SendTo(packetBytes, packetBytes.Length, SocketFlags.None, clientToSendTo); //Dont need to bind because we are not a server - we don't expect a specific place to connect to us

        if (packetToSend.ToString() != "Packets.PositionPacket")
            Debug.Log("Server sent packet of type: " + packetToSend.ToString() + " to " + clientToSendTo.Address + ":" + clientToSendTo.Port);
    }

    /// <summary>
    /// Throws an exception with a reason. Specifically for use inside of the startup function.
    /// </summary>
    /// <param name="reason">Why startup has failed.</param>
    void FailedStartup(string reason)
    {
        throw new Exception(reason);
    }

    /// <summary>
    /// Assigns a new playerID as long as its not duplicate.
    /// </summary>
    /// <param name="connectionToAssignIDTo"></param>
    /// <returns>The assigned player ID.</returns>
    int AssignPlayerID(Server.Connection connectionToAssignIDTo)
    {
        /*Find non duplicate ID*/
        int id;
        do
        {
            System.Random r = new System.Random();
            id = r.Next(0, int.MaxValue - 1);
        } while (IsPlayerIDDuplicate(id));

        connectionToAssignIDTo.AssignPlayerID(id); //assign to object

        return id;
    }

    /// <summary>
    /// Check if the playerID is duplicate inside of connections.
    /// </summary>
    /// <param name="id">The ID to check</param>
    /// <returns></returns>
    bool IsPlayerIDDuplicate(int id)
    {
        foreach (var connection in connections.Values)
        {
            if (connection.IsPlayerIDAssigned)
            {
                if (connection.PlayerID == id)
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Update other connections players to make them aware of the new one.
    /// </summary>
    void MakeConnectionsAwareOfNewPlayer()
    {
        /*
         * While specifying a new ID would theretically work, it doesn't notify any new players if a new
         * connection is in progress. This is why here we check for new connections verusus other connections
         * to match up all of the list. Its probably a tad easier than keeping a master player list and verifying
         * that each player has all players. Although this is quite computationally expensive.
         * /

        /*Find all ID's*/
        HashSet<int> allIds = new HashSet<int>();
        foreach (var connection in connections.Values)
        {
            if (connection.IsPlayerIDAssigned) //if the player actually connected
            {
                if (!allIds.Contains(connection.PlayerID) && 
                    connection.ConnectionStatus != Server.Connection.CONNECTION_STATUS.TO_DISCONNECT) //dont add a player that is due to disconnect, 
                {
                    allIds.Add(connection.PlayerID);
                }
            }

        }

        /*Now make sure each connection has the same player list*/
        foreach (var connection in connections.Values)
        {
            foreach (var id in allIds)
            {
                if (!connection.PlayersAwareOf.Keys.Contains(id))
                {
                    connection.PlayersAwareOf.Add(id, Server.Connection.PLAYER_AWARENESS_STATUS.UNAWARE);
                }
            }
        }
    }


    /// <summary>
    /// Make all of the connectons aware that someone disconnected.
    /// </summary>
    /// <param name="disconnectedID">PlayerID of the disconnected client.</param>
    void MakeConnectionsAwareOfDisconnect(int disconnectedID)
    {
        foreach (var connection in connections.Values)
        {
            connection.MarkConnectionToDisconnect(disconnectedID);
        }
    }

}
