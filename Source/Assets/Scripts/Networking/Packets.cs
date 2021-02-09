using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Packets
{
    public enum PACKET_TYPE : byte
    {
        NOT_SET,
        ERROR,
       
        CONNECTION,

        POSITION,

        TIME_SYNC,

        NEW_PLAYER,
        NEW_PLAYER_ACK,

        PLAYER_DISCONNECTED,
        PLAYER_DISCONNECTED_ACK,
    }

    /// <summary>
    /// Header for all packets.
    /// </summary>
    [Serializable]
    public class PacketHeader : IComparable
    {
        private PACKET_TYPE packetType = PACKET_TYPE.NOT_SET;
        public float timeSent = 0.0f;

        /// <summary>
        /// What type is the packet - should be set by the inheriting packet
        /// </summary>
        public PACKET_TYPE PacketType { get => packetType; protected set => packetType = value; }

        public void ErrorDecoding()
        {
            packetType = PACKET_TYPE.ERROR;
        }

        /// <summary>
        /// Provide sorting capabilities for C#. Sorts Descending (newest packet first).
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (((PacketHeader)obj).timeSent > timeSent)
                return 1;
            else if (((PacketHeader)obj).timeSent < timeSent)
                return -1;
            else
                return 0;
        }
    }

    namespace ConnectionPackets
    {
        /// <summary>
        /// For telling which type of connection packet it is (I am aware of reflection capabilities but this allows neater sorting of packets using switches)
        /// </summary>
        public enum CONNECTION_STATE
        {
            ERROR_OR_NOT_SET,
            WANT_CONNECT,
            APPROVE_CONNECT,
            DECLINE_CONNECT, ///TODO: Implement declining a connection
            ACK_CONNECTION_RESPONSE,
            CONNECTED,
        }

        /// <summary>
        /// For telling the server that the client wants to connect.
        /// </summary>
        [Serializable]
        public class ConnectionStatePacket : PacketHeader
        {
            public CONNECTION_STATE ConnectionState;

            public ConnectionStatePacket()
            {
                PacketType = PACKET_TYPE.CONNECTION;
            }
        }

        /// <summary>
        /// A connection state packet that contains the assigned player ID and initial time.
        /// </summary>
        [Serializable]
        public class ApproveConnectPacket : ConnectionStatePacket
        {
            public int AssignedPlayerID;
            public float ServerTime;

            public ApproveConnectPacket(int assignedPlayerID, float serverTime)
            {
                this.AssignedPlayerID = assignedPlayerID;
                this.ServerTime = serverTime;
                ConnectionState = CONNECTION_STATE.APPROVE_CONNECT;
            }
        }
    }

    /// <summary>
    /// Packet for sending a position update.
    /// </summary>
    [Serializable]
    public class PositionPacket : PacketHeader
    {
        public int PlayerID;

        public float speed;

        public float x;
        public float y;

        public bool isMoving;

        public PositionPacket()
        {
            this.PacketType = PACKET_TYPE.POSITION;
        }

        public Vector2 GetPositionVector2()
        {
            return new Vector2(x, y);
        }

        public Vector3 GetPositionVector3()
        {
            return new Vector3(x, y, 0.0f);
        }
    }

    /// <summary>
    /// For signalling a time sync.
    /// </summary>
    [Serializable]
    public class TimeSyncPacket : PacketHeader
    {
        public float ServerTime;

        public TimeSyncPacket(float serverTime)
        {
            this.PacketType = PACKET_TYPE.TIME_SYNC;
            this.ServerTime = serverTime;
        }
    }

    /// <summary>
    /// New player packet. For informing the client that a new player has connected.
    /// </summary>
    [Serializable]
    public class NewPlayerPacket : PacketHeader
    {
        public int NewPlayerID;

        public NewPlayerPacket(int newPlayerID)
        {
            PacketType = PACKET_TYPE.NEW_PLAYER;
            this.NewPlayerID = newPlayerID;
        }
    }

    /// <summary>
    /// New player acknowledgment packet. For acknowledging when a NewPlayerPacket arrives.
    /// </summary>
    [Serializable]
    public class NewPlayerAcknowledgementPacket : PacketHeader
    {
        public int NewPlayerIDRecieved;

        public NewPlayerAcknowledgementPacket(int newPlayerIDRecieved)
        {
            PacketType = PACKET_TYPE.NEW_PLAYER_ACK;
            this.NewPlayerIDRecieved = newPlayerIDRecieved;
        }
    }

    /// <summary>
    /// Packet that signals that a player has disconnected.
    /// </summary>
    [Serializable]
    public class DisconnectedPlayerPacket : PacketHeader
    {
        public int PlayerIDDisconnected;

        public DisconnectedPlayerPacket(int playerIDDisconnected)
        {
            PacketType = PACKET_TYPE.PLAYER_DISCONNECTED;
            this.PlayerIDDisconnected = playerIDDisconnected;
        }

    }

    /// <summary>
    /// Confirmation that DisconnectedPlayerPacket was recieved.
    /// </summary>
    [Serializable]
    public class DisconnectedPlayerAcknowledgemmentPacket : PacketHeader
    {
        public int PlayerIDDisconnectedRecieved;

        public DisconnectedPlayerAcknowledgemmentPacket(int playerIDDisconnected)
        {
            PacketType = PACKET_TYPE.PLAYER_DISCONNECTED_ACK;
            this.PlayerIDDisconnectedRecieved = playerIDDisconnected;
        }

    }
}

[Serializable]
struct SerializableVector2
{
    public float x;
    public float y;

    public Vector2 GetVector2()
    {
        return new Vector2(x, y);
    }
}