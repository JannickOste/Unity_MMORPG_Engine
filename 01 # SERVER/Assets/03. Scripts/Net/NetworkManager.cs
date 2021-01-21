using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{

    public static NetworkManager instance { get; set; }
    public static GameObject playerContainer { get; set; }

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

        playerContainer = GameObject.Find("Players");

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
        Server.Start(50, 26950);
    }

    private void OnApplicationQuit()
    {
        foreach (Client client in Server.clients.Values.Where(i => i != null && i.player != null))
            PacketHandler.SendPacket(client.id, ServerPacket.MESSAGE, new Dictionary<string, object>() { { "message", "002" } });

        Server.Stop();
    }

    public Player InstantiatePlayer(string _username)
    {
        var player = Instantiate(Resources.Load("Prefabs/Characters/Player") as GameObject, Vector3.zero, Quaternion.identity);
        var playerdata = player.GetComponent<Player>();
        playerdata.name = _username;

        return playerdata;
    }

    public void VerifyUser(int fromClient, Dictionary<string, object> userData)
    {

        int _clientId = int.Parse(userData["id"].ToString());
        string _username = userData["username"].ToString();
        string _secret = userData["secret"].ToString();

        try
        {
            if (fromClient != _clientId)
            {
                Debug.Log($"Player \"{_username}\" (ID: {fromClient}) has assumed the wrong client ID ({_clientId})!");
            }
            else
            {
                Server.clients[_clientId].remoteKey = Cryptograph.StringToKey(_secret);

                Debug.Log($"{Server.clients[_clientId].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_clientId}.");

                Misc.MakeWebRequest(
                    this,
                    new Action<object, object>((webout, isError) =>
                    {
                        if (!(bool)isError)
                        {
                            Server.clients[_clientId].loginHash = webout.ToString();
                        }
                    }), new Tuple<string, string>[]
                    {
                         new Tuple<string, string>("target", "getHash"),
                         new Tuple<string, string>("username", _username)
                    });

                EntityHandler.InstantiateEntity<Player>(_clientId, _username);
            }
        }
        catch (System.NullReferenceException ex)
        {
            Debug.Log($"An error ocurred during parsing some packet data ex: {ex.Message}");
        }


    }


}
