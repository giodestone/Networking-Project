using Packets;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

/// <summary>
/// Contains useful networking helpers.
/// </summary>
static class NetworkingHelpers
{
    /// <summary>
    /// Encode a packet into bytes
    /// </summary>
    /// <typeparam name="PacketType">The type of the packet.</typeparam>
    /// <param name="packetToSerialize">Packet to serliaze.</param>
    /// <returns>Byte array containing the bytes.</returns>
    public static byte[] EncodePacketIntoBytes<PacketType>(PacketType packetToSerialize)
    {
        MemoryStream ms = new MemoryStream();
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(ms, packetToSerialize);
        return ms.ToArray();
    }

    /// <summary>
    /// Decode some bytes into a packet.
    /// </summary>
    /// <param name="packetBytes"></param>
    /// <returns>Decoded packet header.</returns>
    public static PacketHeader DecodePacketHeaderFromBytes(byte[] packetBytes, ref bool decodeSuccessful)
    {
        decodeSuccessful = true;
        MemoryStream ms = new MemoryStream();
        ms.Write(packetBytes, 0, packetBytes.Length);
        ms.Position = 0;
        BinaryFormatter bf = new BinaryFormatter();

        //Try to decode it (packet *could* be mangled)
        PacketHeader decodedRecievedPacket;
        try
        {
            decodedRecievedPacket = bf.Deserialize(ms) as PacketHeader;
        }
        catch
        {
            decodeSuccessful = false;
            decodedRecievedPacket = new PacketHeader();
            decodedRecievedPacket.ErrorDecoding();
        }

        return decodedRecievedPacket;
    }

    /// <summary>
    /// Receive some bytes from a socket.
    /// </summary>
    /// <param name="socket">Socket to receive bytes from.</param>
    /// <param name="senderEndPoint">Will give who sent them.</param>
    /// <param name="recievedCount">Will give how many were received.</param>
    /// <param name="wasSuccessful">Will give whether the recieve was successful.</param>
    /// <param name="socketException">Will give the socket exception if there is any.</param>
    /// <returns></returns>
    public static byte[] RecieveBytes(ref Socket socket, out EndPoint senderEndPoint, out int recievedCount, out bool wasSuccessful, out SocketException socketException)
    {
        wasSuccessful = true;
        socketException = null;

        //Read whole packet REMEMBER YOU EITHER HAVE A PACKET OR YOU DONT! THE SIZE READ REPRESENTS THE SIZE OF THE PACKET! YOU JUST NEED TO READ ONCE! SEE NOtes!
        byte[] packetBytes = new byte[2048];
        recievedCount = 0;
        senderEndPoint = new IPEndPoint(IPAddress.Any, 0);

        try
        {
            recievedCount = socket.ReceiveFrom(packetBytes, ref senderEndPoint);
        }
        catch (SocketException se)
        {
            ///TODO: HANDLE DISCONNECT!
            wasSuccessful = false;
            Debug.LogWarning("Failed to Recieve. Exception: " + se.ToString());
            socketException = se;
        }
        catch (Exception e)
        {
            ///TODO: HANDLE ANY OTHER EXCEPTIONS!
            wasSuccessful = false;
            Debug.LogError("Other error occurred! " + e.ToString());
        }

        return packetBytes;
    }
}
