using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Server
{
    /// <summary>
    /// For describing the connection that a client has inside of the server.
    /// </summary>
    public class Connection
    {
        public enum CONNECTION_STATUS
        {
            TO_SEND_CONNECT_SUCCESS_ACK,
            WATITING_FOR_CONNECTION_CONFIRMATION_ACK,
            TO_DISCONNECT,
            CONNECTED
        }

        public enum PLAYER_AWARENESS_STATUS
        {
            UNAWARE,
            AWARE,
            TO_DISCONNECT
        }

        CONNECTION_STATUS connectionStatus; // What stage of connection is the client in

        IPEndPoint clientIp;

        float lastMessageSentTime = -1.0f;
        float lastMessageRecieveTime = 1.0f;

        int ackRetryCount = 0;
        const int maxAckRetryCount = 10;

        /*Connections playerID*/
        bool isPlayerIDAssigned = false;
        int playerID = 0;

        /*Player Awareness*/
        Dictionary<int, PLAYER_AWARENESS_STATUS> playersAwareOf = new Dictionary<int, PLAYER_AWARENESS_STATUS>(); //Stores which clients the player is aware of. Id and isAware.
        float lastSentPlayerAwarenessPacket = -1.0f;
        public const float MaxTimeBetweenSendingPlayerAwarenessPacket = 0.1f; //Retry time for sending a new player in seconds

        /*Connection Related*/
        public float LastMessageSentTime { get => lastMessageSentTime; set => lastMessageSentTime = value; }
        public float LastMessageRecieveTime { get => lastMessageRecieveTime; set => lastMessageRecieveTime = value; }
        public CONNECTION_STATUS ConnectionStatus { get => connectionStatus; set => connectionStatus = value; }
        public const float TimeoutTime = 7.5f;
        public IPEndPoint ClientIp { get => clientIp; set => clientIp = value; }

        public int AckRetryCount { get => ackRetryCount; set => ackRetryCount = value; }
        public static int MaxAckRetryCount => maxAckRetryCount;

        /*Player ID*/
        public bool IsPlayerIDAssigned { get => isPlayerIDAssigned; }
        public int PlayerID
        {
            get
            {
                if (isPlayerIDAssigned == false)
                {
                    throw new Exception("PlayerID retrieved before assigned! At Server.Connection PlayerID getter.");
                }
                else
                    return playerID;
            }
        }

        /*Player Awareness*/
        public Dictionary<int, PLAYER_AWARENESS_STATUS> PlayersAwareOf { get => playersAwareOf; set => playersAwareOf = value; }
        public bool IsAwareOfAllPlayers
        {
            get
            {
                foreach (var playerID in playersAwareOf.Keys)
                    if (playersAwareOf[playerID] == PLAYER_AWARENESS_STATUS.UNAWARE) //If unaware of one of the players return false
                        return false;
                return true;
            }
        }
        public bool IsPlayerToDisconnect
        {
            get
            {
                return playersAwareOf.Values.Contains(PLAYER_AWARENESS_STATUS.TO_DISCONNECT);
            }
        }
        public float LastTimeSentPlayerAwarenessPacket { get => lastSentPlayerAwarenessPacket; set => lastSentPlayerAwarenessPacket = value; }

        /*Constructor*/
        public Connection(IPEndPoint clientIp)
        {
            this.ClientIp = clientIp;
            lastMessageRecieveTime = Time.time;
        }

        /*Functions*/
        /// <summary>
        /// Assign an id to the player.
        /// </summary>
        /// <remarks>
        /// The function doesn't check if the ID is duplicate with other players, probably worth checking beforehand.
        /// </remarks>
        /// <param name="id">The new ID</param>
        public void AssignPlayerID(int id)
        {
            if (isPlayerIDAssigned)
                throw new Exception("Server: Player ID already assigned.");
            playerID = id;
            isPlayerIDAssigned = true;
        }

        /// <summary>
        /// Mark the ID of a player to disconnect.
        /// </summary>
        /// <param name="idOfPlayerToDisconnect"></param>
        public void MarkConnectionToDisconnect(int idOfPlayerToDisconnect)
        {
            playersAwareOf[idOfPlayerToDisconnect] = PLAYER_AWARENESS_STATUS.TO_DISCONNECT;
        }

        /// <summary>
        /// Get ID of a player that has been marked TO_DISCONNECT.
        /// </summary>
        /// <returns>ID of a that has been marked TO_DISCONNECT.</returns>
        public int GetIDOfAPlayerToDisconnect()
        {
            foreach (var player in playersAwareOf)
            {
                if (player.Value == PLAYER_AWARENESS_STATUS.TO_DISCONNECT)
                    return player.Key;
            }

            throw new Exception("Server: Connection " + playerID.ToString() + " has no IDs to disconnect!");
        }
    }
}
