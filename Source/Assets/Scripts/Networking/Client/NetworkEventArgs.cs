using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using Packets;

/// <summary>
/// Contains all args for packet events.
/// </summary>
/// <remarks>
/// Classes because structs are a pain to deal with in C#.
/// </remarks>
namespace NetworkEventArgs
{
    public class PositionPacketEventArgs : EventArgs
    {
        public PositionPacket PositionPacket;

        public PositionPacketEventArgs(PositionPacket pp)
        {
            PositionPacket = pp;
        }
    }

    /// <summary>
    /// For signalling network status updates such as connection, disconnection, server time sync etc.
    /// </summary>
    public class NetworkStatusUpdateEventArgs : EventArgs
    {
        /// <summary>
        /// What sort of event is the network status event about.
        /// </summary>
        public enum NETWORK_STATUS_EVENT_TYPE
        {
            CONNECTED,
            CONNECTING,
            DISCONNECTED,
            SERVER_TIME_SYNC,
            NEW_PLAYER,
            PLAYER_DISCONNECTED
        }

        public NETWORK_STATUS_EVENT_TYPE NetworkStatusEventType;

        ///[Enhancement] TODO: Move into its own event args type, inhertiting from this one (safety and neatness!).
        public float NewServerTime = -1.0f;
        public int NewPlayerID = -1;
        public int PlayerDisconnectedID = -1;

        public NetworkStatusUpdateEventArgs(NETWORK_STATUS_EVENT_TYPE statusUpdateType)
        {
            NetworkStatusEventType = statusUpdateType;
        }
    }
}
