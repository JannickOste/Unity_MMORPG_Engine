using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class DataHandler
{
    private static Dictionary<int, NPCData> npcData;
    private static Dictionary<string, string> npcDialogs;
    private static Dictionary<int, WorldObjectData> worldObjects;

    private static string save_path = System.IO.Path.Combine(Application.dataPath, "Configuration", "Users");

    public static void Load()
    {
        npcData = LoadNPCData();
        npcDialogs = JObject.Parse(System.IO.File.ReadAllText($"{Application.dataPath}/Configuration/Dialogs.json")).ToObject<Dictionary<string, string>>();
        worldObjects = LoadWorldObjectData();
    }

    #region worldObjects
    public static WorldObjectData GetObjectData(int id) => worldObjects[id];
    public static Dictionary<int, WorldObjectData> GetObjectData() => worldObjects;

    private static Dictionary<int, WorldObjectData> LoadWorldObjectData()
    {
        string configuration_path = $"{Application.dataPath}/{Constants.OBJECT_CONFIGURATION}";

        Dictionary<int, WorldObjectData> dataLib = new Dictionary<int, WorldObjectData>();
        var npc_config = JObject.Parse(System.IO.File.ReadAllText(configuration_path));

        foreach (KeyValuePair<string, JToken> pair in npc_config)
        {
            int object_id;
            if (int.TryParse(pair.Key, out object_id))
            {
                
                dataLib.Add(object_id, JsonUtility.FromJson<WorldObjectData>(pair.Value.ToString()));
                Debug.Log(dataLib[object_id].model_name);
            }
        }

        return dataLib;
    }

    #endregion


    #region npc
    private static Dictionary<int, NPCData> LoadNPCData()
    {
        string configuration_path = $"{Application.dataPath}/{Constants.NPC_CONFIGURATION}";

        Dictionary<int, NPCData> dataLib = new Dictionary<int, NPCData>();
        var npc_config = JObject.Parse(System.IO.File.ReadAllText(configuration_path));

        foreach (KeyValuePair<string, JToken> pair in npc_config)
        {
            int NPCID;
            if(int.TryParse(pair.Key, out NPCID))
                dataLib.Add(NPCID, JsonUtility.FromJson<NPCData>(pair.Value.ToString()));
        }

        return dataLib;
    }

    public static Dictionary<int, NPCData> GetNPCData() => npcData;
    public static NPCData GetNPCData(int id) => npcData[id];
    public static string GetNPCDialog(int id) => npcDialogs[id.ToString()];

    #endregion

    #region player

    private static PlayerData loadPlayerData(string username)
    {
        return JsonUtility.FromJson<PlayerData>(System.IO.File.ReadAllText(System.IO.Path.Combine(save_path, $"{username}.json")));
    }

    public static PlayerData GetPlayerData(string username)
    {
        if (!System.IO.File.Exists(System.IO.Path.Combine(save_path, $"{username}.json"))) createNewPlayerData(username);

        return loadPlayerData(username);
    }


    private static void createNewPlayerData(string username)
    {
        var data = new PlayerData
        {
            username = username,
            model_name = "LocalPlayer",
            scene_id = 1,
            position = new Vector3(125f, 1.5f, 125f),
            hitpoints = 100,
            questStates = JObject.FromObject(QuestHandler.GetQuestsList()).ToString()
        };

        System.IO.File.WriteAllText(System.IO.Path.Combine(save_path, $"{username}.json"), JsonUtility.ToJson(data));
    }


    public static void UpdatePlayerData(Player player)
    {
        var data = new PlayerData
        {
            username = player.name,
            model_name = player.model_name,
            hitpoints = player.hitpoints,
            position = player.transform.position,
            rotation = player.transform.rotation,
            questStates = JObject.FromObject(player.questStateLib.ToDictionary(k => k.Key.ToString(), k => k.Value.ToString())).ToString()
        };

        System.IO.File.WriteAllText(System.IO.Path.Combine(save_path, $"{player.name}.json"), JsonUtility.ToJson(data));
    }

    #endregion
}

