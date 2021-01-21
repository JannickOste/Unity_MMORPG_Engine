using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using UnityEngine.SceneManagement;
using System.Security.Cryptography;
using System.Collections.Generic;

public class Client : MonoBehaviour
{
    /* Client */
    public static Client instance { get; set; }
    public int myId = 0;
    public bool isConnected = false;

    /* Networking */
    public string ip = "25.57.27.21";
    public int port = 26950;
    public static int dataBufferSize = 4096;
    public TCP tcp { get; set; }
    public UDP udp { get; set; }

    /* Encryption */
    public string loginHash;
    public RSAParameters serverKey { get; set; }
    public (RSAParameters pub, RSAParameters priv) clientKeys { get; set; }


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    private void Start()
    {
        isConnected = false;
        tcp = new TCP();
        udp = new UDP();
    }

    public void InstatiateConnection(Dictionary<string, object> input)
    {
        Client.instance.serverKey = Cryptograph.StringToKey(input["key"].ToString());
        Client.instance.clientKeys = Cryptograph.GenerateKeySet();

        Client.instance.myId = int.Parse(input["id"].ToString());

        StartMenu loginUI = GameManager.instance.GetComponent<UIHandler>().GetCurrentUI(typeof(StartMenu)) as StartMenu;
        PacketHandler.SendPacket(ClientPacket.WELCOME_RECEIVED, new Dictionary<string, object>()
        {
            { "id", Client.instance.myId },
            { "username", loginUI.logNameInput.text },
            { "secret",  Cryptograph.KeyToString(Client.instance.clientKeys.pub) }
        }, useEncrypt: false);

        GameManager.instance.GetComponent<UIHandler>().GetCurrentUI().Destroy();
        GameManager.instance.GetComponent<UIHandler>().SetUI(typeof(Overlay));

        // Now that we have the client's id, connect UDP
        Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);
        Client.instance.isConnected = true;
    }
   
    private void OnApplicationQuit() =>  Disconnect(); // Disconnect when the game is closed

    /// <summary>Attempts to connect to the server.</summary>
    public void ConnectToServer(string client_secret)
    {
        GameManager.instance.gameObject.GetComponent<GameManager>().clientSecret = client_secret;

        tcp.Connect(); // Connect tcp, udp gets connected once tcp is done
    }

    public class TCP
    {
        public TcpClient socket;

        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;

        /// <summary>Attempts to connect to the server via TCP.</summary>
        public void Connect()   
        {
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            receiveBuffer = new byte[dataBufferSize];
            var client = socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);
            bool success = client.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1d));

            if(!success)
            {
                instance.InvokeErrorCode("006");
            }
        }

        /// <summary>Initializes the newly connected client's TCP-related info.</summary>
        private void ConnectCallback(IAsyncResult _result)
        {
            socket.EndConnect(_result);

            if (!socket.Connected)
            {
                return;
            }

            stream = socket.GetStream();

            receivedData = new Packet();

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }

        /// <summary>Sends data to the client via TCP.</summary>
        /// <param name="_packet">The packet to send.</param>
        public void SendData(Packet _packet)
        {
            try
            {
                if (socket != null)
                {
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null); // Send data to server
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to server via TCP: {_ex}");
            }
        }

        /// <summary>Reads incoming data from the stream.</summary>
        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                int _byteLength = stream.EndRead(_result);
                if (_byteLength <= 0)
                {
                    instance.Disconnect();
                    return;
                }

                byte[] _data = new byte[_byteLength];
                Array.Copy(receiveBuffer, _data, _byteLength);

                receivedData.Reset(HandleData(_data)); // Reset receivedData if all data was handled
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch
            {
                Disconnect();
            }
        }

        /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
        /// <param name="_data">The recieved data.</param>
        private bool HandleData(byte[] _data)
        {
            int _packetLength = 0;

            receivedData.SetBytes(_data);

            if (receivedData.UnreadLength() >= 4)
            {
                // If client's received data contains a packet
                _packetLength = receivedData.ReadInt();
                if (_packetLength <= 0)
                {
                    // If packet contains no data
                    return true; // Reset receivedData instance to allow it to be reused
                }
            }

            while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                // While packet contains data AND packet data length doesn't exceed the length of the packet we're reading
                byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                        PacketHandler.ReceivePacket(_packet);
                });

                _packetLength = 0; // Reset packet length
                if (receivedData.UnreadLength() >= 4)
                {
                    // If client's received data contains another packet
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        // If packet contains no data
                        return true; // Reset receivedData instance to allow it to be reused
                    }
                }
            }

            if (_packetLength <= 1)
            {
                return true; // Reset receivedData instance to allow it to be reused
            }

            return false;
        }

        /// <summary>Disconnects from the server and cleans up the TCP connection.</summary>
        private void Disconnect()
        {
            instance.Disconnect();

            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    public class UDP
    {
        public UdpClient socket;
        public IPEndPoint endPoint;

        public UDP()
        {
            endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
        }

        /// <summary>Attempts to connect to the server via UDP.</summary>
        /// <param name="_localPort">The port number to bind the UDP socket to.</param>
        public void Connect(int _localPort)
        {
            socket = new UdpClient(_localPort);

            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallback, null);

            using (Packet _packet = new Packet())
            {
                SendData(_packet);
            }
        }

        /// <summary>Sends data to the client via UDP.</summary>
        /// <param name="_packet">The packet to send.</param>
        public void SendData(Packet _packet)
        {
            try
            {
                _packet.InsertInt(instance.myId); // Insert the client's ID at the start of the packet
                if (socket != null)
                {
                    socket.BeginSend(_packet.ToArray(), _packet.Length(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to server via UDP: {_ex}");
            }
        }

        /// <summary>Receives incoming UDP data.</summary>
        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                byte[] _data = socket.EndReceive(_result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                if (_data.Length < 4)
                {
                    instance.Disconnect();
                    return;
                }

                HandleData(_data);
            }
            catch
            {
                Disconnect();
            }
        }

        /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
        /// <param name="_data">The recieved data.</param>
        private void HandleData(byte[] _data)
        {
            using (Packet _packet = new Packet(_data))
            {
                int _packetLength = _packet.ReadInt();
                _data = _packet.ReadBytes(_packetLength);
            }

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(_data))
                {
                    PacketHandler.ReceivePacket(_packet);
                }
            });
        }

        /// <summary>Disconnects from the server and cleans up the UDP connection.</summary>
        private void Disconnect()
        {
            instance.Disconnect();

            endPoint = null;
            socket = null;
        }
    }

    public void InvokeErrorCode(string code)
    {
        if (SceneManager.GetActiveScene().buildIndex != 0)
        {
            SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().buildIndex);
            SceneManager.LoadSceneAsync(0);

            var errorPrompt = new GameObject();
            errorPrompt.name = $"errorPrompt#{code}";
            DontDestroyOnLoad(errorPrompt);

            Destroy(GameManager.instance.gameObject);
            Destroy(GameObject.Find("Entitys"));
        }
        else GameManager.instance.GetComponent<UIHandler>().SetUI(typeof(StartMenu));
    }

    /// <summary>Disconnects from the server and stops all network traffic.</summary>
    public void Disconnect()
    {
        if (isConnected)
        {
            isConnected = false;
            tcp.socket.Close();
            udp.socket.Close();
        }
    }
}
