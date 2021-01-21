using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ClientPacket
{
    WELCOME_RECEIVED = 0, PLAYER_INPUT, ACTION_REQUEST
}

public enum ServerPacket
{
    SPAWN_ENTITY = 0, OPEN_CHAT, UPDATE_COMPONENT, MESSAGE, WELCOME
}


public class PacketHandler
{
    private static readonly Dictionary<ServerPacket, System.Action<Dictionary<string, object>>> ServerPacketHandlers = 
        new Dictionary<ServerPacket, System.Action<Dictionary<string, object>>>()
        {
            { ServerPacket.SPAWN_ENTITY,     EntityHandler.SpawnEntity },
            { ServerPacket.WELCOME,          Client.instance.InstatiateConnection },
            { ServerPacket.OPEN_CHAT,        GameManager.instance.GetComponent<UIHandler>().HandleUIPacket },
            // Procs instatation error if not wrapped in action.
            { ServerPacket.UPDATE_COMPONENT, new System.Action<Dictionary<string, object>>((inp) => EntityHandler.GetEntity<Character>(EntityGroup.PLAYER, Client.instance.myId).UpdateComponent(inp)) },
            { ServerPacket.MESSAGE,          new System.Action<Dictionary<string, object>>((inp) => 
                                        {
                                            string _msg = inp["message"].ToString();

                                            if (Constants.ERROR_REG.IsMatch(_msg)) Client.instance.InvokeErrorCode(_msg);
                                        })
            }
        };

    private static Dictionary<string, System.Func<object, object>> ReceiveParsers = new Dictionary<string, System.Func<object, object>>()
    {
        { "UnityEngine.Quaternion", new System.Func<object, object>((input) =>  new Quaternion(((System.Single[])input)[0], ((System.Single[])input)[1],((System.Single[])input)[2],((System.Single[])input)[3]))},
        { "UnityEngine.Vector3",    new System.Func<object, object>((input) =>  new Vector3(((System.Single[])input)[0], ((System.Single[])input)[1],((System.Single[])input)[2]))},
        { "System.Boolean[]",       new System.Func<object, object>((input) =>  input.ToString().Split(',').Select(i => bool.Parse(i)).ToArray())}
    };

    private static Dictionary<string, System.Func<object, string>> SendParsers = new Dictionary<string, System.Func<object, string>>()
    {
        { "UnityEngine.Quaternion", new System.Func<object, string>((input) =>  string.Join(",", new System.Single[4] { ((Quaternion)input).x, ((Quaternion)input).y,((Quaternion)input).z, ((Quaternion)input).w }) )},
        { "UnityEngine.Vector3",    new System.Func<object, string>((input) =>  string.Join(",", new System.Single[3] { ((Vector3)input).x, ((Vector3)input).y,((Vector3)input).z }) )},
        { "System.Single[]",        new System.Func<object, string>((input) =>  string.Join(",", (System.Single[])input)) },
        { "System.Boolean[]",       new System.Func<object, string>((input) =>  string.Join(",", (System.Boolean[])input)) }
    };



    #region Parsing
    private static object ParseReceiveObject(string toType, object sourceObject)
    {
        if (ReceiveParsers.ContainsKey(toType)) return ReceiveParsers[toType](sourceObject.ToString().Split(',').Select(i => float.Parse(i)).ToArray());
        System.Type convert = System.Type.GetType(toType);

        if (convert != null)
        {
            if (convert.IsArray)
            {
                System.Type baseType = System.Type.GetType(toType.Substring(0, toType.IndexOf('[')));

                return sourceObject.ToString().Split(',').Select(i => System.Convert.ChangeType(sourceObject, baseType)).ToArray();
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

                    System.Type parsedType = System.Type.GetType(sliceType);
                    if (parsedObject == null && parsedType != null) parsedObject = System.Convert.ChangeType(sliceData, parsedType);
                }

                if (parsedObject != null)
                    packetData.Add(sliceName, parsedObject);
                else Debug.Log($"Parsed object with name \"{sliceName}\" is null, {sliceName}:{sliceData}:{sliceType}");
            }
            catch (System.Exception ex)
            {
                Debug.Log($"Failed to parse packet piece with name \"{sliceName}\" -> {ex.Message}");
            }
        }
        return packetData;
    }
    #endregion


    #region Receive
    public static void ReceivePacket(Packet packet)
    {
        try
        {
            bool useEncrypt = packet.ReadBool();
            ServerPacket packetType = (ServerPacket)System.Enum.Parse(typeof(ServerPacket), packet.ReadString());

            int lineCount = packet.ReadInt();
            string[] lineBuff = new string[lineCount];

            for (int i = 0; i < lineCount; i++)
                lineBuff[i] = (useEncrypt ? Cryptograph.Decrypt(Client.instance.clientKeys.priv, Cryptograph.SHA256Decrypt(packet.ReadString(), Client.instance.loginHash)) : packet.ReadString());

            System.Action<Dictionary<string, object>> handler;
            if (ServerPacketHandlers.TryGetValue(packetType, out handler))
            {
                handler(ParseReceivedPacket(lineBuff));
            }
            else Debug.Log("Failed to get packetHandler");
        }
        catch (System.NullReferenceException ex)
        {
            Debug.Log($"An error ocurred during parsing some packet data ex: {ex.Message}");
        }
    }
    #endregion

    #region Send
    /// <summary>Sends a packet to the server via TCP.</summary>
    /// <param name="_packet">The packet to send to the sever.</param>
    private static void SendTCPData(Packet _packet)
    {
        _packet.WriteLength();
        Client.instance.tcp.SendData(_packet);
    }

    /// <summary>Sends a packet to the server via UDP.</summary>
    /// <param name="_packet">The packet to send to the sever.</param>
    private static void SendUDPData(Packet _packet)
    {
        _packet.WriteLength();
        Client.instance.udp.SendData(_packet);
    }

    /// <summary> Send a packet to endpoint </summary>
    /// <param name="action">Packet ID</param>
    /// <param name="packetData">Packet content</param>
    /// <param name="udp">UDP Enabled</param>
    /// <param name="useEncrypt">Use RSA/SHA256 encryption</param>
    public static void SendPacket(ClientPacket action, Dictionary<string, object> packetData, bool udp = false, bool useEncrypt = true)
    {
        string clientHash = Client.instance.loginHash;
        System.Security.Cryptography.RSAParameters clientKey = Client.instance.serverKey;

        using (Packet packet = new Packet((int)ClientPackets.welcomeReceived))
        {
            // SEND: Encryption enabled / packet ID.
            packet.Write(useEncrypt);
            packet.Write(action.ToString());

            // SEND: Data count & data converted in string format.
            packet.Write(packetData.Count());
            foreach(KeyValuePair<string, object> pair in packetData)
            {
                string packet_line = string.Join("|", new string[3] { pair.Key, ParseSendObject(pair.Value), pair.Value.GetType().ToString() });
                Debug.Log(packet_line);
                packet.Write(!useEncrypt ? packet_line : Cryptograph.Encrypt(clientKey, Cryptograph.SHA256Encrypt(packet_line, clientHash)));
            }

            // SEND: With UDP style traffic if boolean "udp" is true, otherwise TCP.
            if (udp) SendUDPData(packet);
            else SendTCPData(packet);
        }
    }
    #endregion
}