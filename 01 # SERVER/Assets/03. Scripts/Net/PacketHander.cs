using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using UnityEngine;

public enum ClientPacket { WELCOME_RECEIVED, PLAYER_INPUT, ACTION_REQUEST }

public enum ServerPacket { SPAWN_ENTITY, OPEN_CHAT, UPDATE_COMPONENT, MESSAGE, WELCOME }

public class PacketHandler
{
    private static readonly string packet_slice_delimiter = "|";
    private static readonly string packet_datastring_delimiter = ",";

    private static readonly Dictionary<ClientPacket, System.Action<int, Dictionary<string, object>>> ClientPacketHandlers = new Dictionary<ClientPacket, System.Action<int, Dictionary<string, object>>>()
    {
        { ClientPacket.WELCOME_RECEIVED, NetworkManager.instance.VerifyUser },
        { ClientPacket.PLAYER_INPUT, new System.Action<int, Dictionary<string, object>>((fromClient, inp) => EntityHandler.GetEntity<Player>(fromClient).SetInput(inp)) }
    };


    #region Parsing
    private static Dictionary<string, System.Func<object, object>> ReceiveParsers = new Dictionary<string, System.Func<object, object>>()
    {
        { "System.Boolean[]",       new System.Func<object, object>((input) =>  input.ToString().Split(',').Select(i => bool.Parse(i)).ToArray())},
        { "UnityEngine.Quaternion", new System.Func<object, object>((input) =>
        {
            input = input.ToString().Split(',').Select(i => float.Parse(i)).ToArray();
            return new Quaternion(((System.Single[])input)[0], ((System.Single[])input)[1],((System.Single[])input)[2],((System.Single[])input)[3]);
        })},
        { "UnityEngine.Vector3",    new System.Func<object, object>((input) =>
        {
            input = input.ToString().Split(',').Select(i => float.Parse(i)).ToArray();
            return new Vector3(((System.Single[])input)[0], ((System.Single[])input)[1],((System.Single[])input)[2]);
        })}
    };

    private static Dictionary<string, System.Func<object, string>> SendParsers = new Dictionary<string, System.Func<object, string>>()
    {
        { "UnityEngine.Quaternion", new System.Func<object, string>((input) =>  string.Join(packet_datastring_delimiter, new System.Single[4] { ((Quaternion)input).x, ((Quaternion)input).y,((Quaternion)input).z, ((Quaternion)input).w }) )},
        { "UnityEngine.Vector3",    new System.Func<object, string>((input) =>  string.Join(packet_datastring_delimiter, new System.Single[3] { ((Vector3)input).x, ((Vector3)input).y,((Vector3)input).z }) )},
        { "System.Single[]",        new System.Func<object, string>((input) =>  string.Join(packet_datastring_delimiter, (System.Single[])input)) },
        { "System.Boolean[]",       new System.Func<object, string>((input) =>  string.Join(packet_datastring_delimiter, (System.Boolean[])input)) }
    };


    /// <summary> Parse a received datastring to desired object</summary>
    /// <param name="toType">Type to cast to</param>
    /// <param name="dataString">Object data</param>
    /// <returns></returns>
    private static object ParseReceiveObject(string toType, string dataString)
    {
        if (ReceiveParsers.ContainsKey(toType)) return ReceiveParsers[toType](dataString);
        System.Type convert = System.Type.GetType(toType);

        if (convert != null)
        {
            if (convert.IsArray)
            {
                System.Type baseType = System.Type.GetType(toType.Substring(0, toType.IndexOf('[')));

                return dataString.ToString().Split(',').Select(i => System.Convert.ChangeType(dataString, baseType)).ToArray();
            }
        }

        return null;
    }

    /// <summary> Prepare an object for transmission to endpoint</summary>
    /// <param name="tObject">Object to send</param>
    /// <returns>Prepared object</returns>
    private static string ParseSendObject(object tObject)
    {
        string typeName = tObject.GetType().ToString();
        if (SendParsers.ContainsKey(typeName)) return SendParsers[typeName](tObject);

        if (tObject.GetType().IsArray)
        {
            try
            {
                IEnumerable<object> objectEnumerable = ((object[])tObject);

                if (objectEnumerable != null)
                    return string.Join(",", objectEnumerable);
            }
            catch (System.InvalidCastException ex)
            {
                Debug.Log($"[ParseSendObject::ExceptionCaught]: (\"Custom set for type: {tObject.GetType()} required\") -> {ex.Message}");
            }
        }

        return tObject.ToString();
    }


    private static Dictionary<string, object> ParseReceivedPacket(string[] packetLines)
    {
        string[][] dataLines = new string[packetLines.Length][];
        for (int i = 0; i < dataLines.Length; i++) dataLines[i] = packetLines[i].Trim().Split(Constants.PACKET_SPLIT.ToCharArray());

        Dictionary<string, object> packetData = new Dictionary<string, object>(packetLines.Length);
        foreach (string[] packetSet in dataLines)
        {
            if (packetSet.Length != 3)
            {
                Debug.Log($"Packet with invalid structure supplied: {string.Join(",", packetSet)}");
                continue;
            }

            // Extract packet values.
            string sliceName = packetSet.ElementAt(0);
            string sliceData = packetSet.ElementAt(1);
            string sliceType = packetSet.ElementAt(2);


            // Parse packet to correct type.
            try
            {
                object parsedObject = null;

                IEnumerable<System.Type> enumType = Reflector.GetEnumTypes().Where(enumtype => sliceType.Contains(enumtype.FullName));

                if (enumType.Count() > 0) parsedObject = System.Enum.Parse(enumType.First(), sliceData);
                else
                {
                    parsedObject = ParseReceiveObject(sliceType, sliceData);
                    if (sliceName == "input")
                    {
                        Debug.Log(sliceType);
                        Debug.Log(sliceData);
                        Debug.Log(parsedObject);
                    }
                    System.Type parsedType = System.Type.GetType(sliceType);
                    if (parsedObject == null && parsedType != null) parsedObject = System.Convert.ChangeType(sliceData, parsedType);
                }

                if (parsedObject != null)
                    packetData.Add(sliceName, parsedObject);
                else Debug.Log($"Parsed object with name \"{sliceName}\" is null, {sliceName}:{sliceData}:{sliceType}");
            }
            catch(System.Exception ex )
            {
                Debug.Log($"Failed to parse packet piece with name \"{sliceName}\" -> {ex.Message}");
            }
        }
        return packetData;
    }
    #endregion

    #region Sending/Receiving
    /// <summary>Sends a packet to a client via TCP.</summary>
    /// <param name="_toClient">The client to send the packet the packet to.</param>
    /// <param name="_packet">The packet to send to the client.</param>
    private static void SendTCPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.clients[_toClient].tcp.SendData(_packet);
    }

    /// <summary>Sends a packet to a client via UDP.</summary>
    /// <param name="_toClient">The client to send the packet the packet to.</param>
    /// <param name="_packet">The packet to send to the client.</param>
    private static void SendUDPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.clients[_toClient].udp.SendData(_packet);
    }

    /// <summary>Sends a packet to all clients except one via TCP.</summary>
    /// <param name="_exceptClient">The client to NOT send the data to.</param>
    /// <param name="_packet">The packet to send.</param>
    private static void SendTCPDataToAll(Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].tcp.SendData(_packet);
        }
    }


    public static void SendPacket(int toClient, ServerPacket type, Dictionary<string, object> packetData, bool udp = false, bool useEncrypt = true)
    {
        string clientHash = Server.clients[toClient].loginHash;
        RSAParameters clientKey = Server.clients[toClient].remoteKey;
        using (Packet packet = new Packet())
        {
            packet.Write(useEncrypt);

            // SEND: Packet subtype and quantity of data within packet.
            packet.Write(type.ToString());
            packet.Write(packetData.Count());

            // SEND: Packet data.
            foreach (KeyValuePair<string, object> pair in packetData)
            {
                string packet_line = $"{pair.Key}|{ParseSendObject(pair.Value)}|{pair.Value.GetType()}";
                packet.Write(!useEncrypt ? packet_line : Cryptograph.Encrypt(clientKey, Cryptograph.SHA256Encrypt(packet_line, clientHash)));
            }

            if (udp) SendUDPData(toClient, packet);
            else SendTCPData(toClient, packet);
        }
    }

    public static void ReceivePacket(int _fromClient, Packet packet)
    {
        try
        {
            // RECEIVE: Encryption enabled / packet ID.
            bool useEncrypt = packet.ReadBool();
            ClientPacket packetType = (ClientPacket)System.Enum.Parse(typeof(ClientPacket), packet.ReadString());

            // RECEIVE: Data count & data converted in string format.
            string[] lineBuff = new string[packet.ReadInt()];
            for (int i = 0; i < lineBuff.Length; i++)
            {
                string line = packet.ReadString();
                if (useEncrypt) line = Cryptograph.Decrypt(Server.clients[_fromClient].clientKeys.privateKey, Cryptograph.SHA256Decrypt(packet.ReadString(), Server.clients[_fromClient].loginHash));

                lineBuff[i] = line;
            }

            // HANDLE: Packet data and pass data to handler.
            Dictionary<string, object> packetData = ParseReceivedPacket(lineBuff);
            if (packetData != null)
            {
                System.Action<int, Dictionary<string, object>> handler;
                if (ClientPacketHandlers.TryGetValue(packetType, out handler))
                    handler(_fromClient, packetData);
                else Debug.Log($"Failed to get packetHandler of type {packetType}");
            }
            else Debug.Log("Failed to read packet data");
        }
        catch (System.NullReferenceException ex)
        {
            Debug.Log($"An error ocurred during parsing some packet data ex: {ex.Message}");
        }
    }

    #endregion
}