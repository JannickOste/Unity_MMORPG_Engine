using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


class Constants
{
    #region GameLogic
    public const string LOGIN_SERVER = "http://25.57.27.21/CamelotServer/index.php";
    public static readonly System.Text.RegularExpressions.Regex ERROR_REG = new System.Text.RegularExpressions.Regex("[0-9]");
    public static int INTERACTION_DELAY_IN_MS = 300;
    public static readonly Dictionary<string, string> ERROR_CODES = new Dictionary<string, string>()
    {
        { "001", "User is already logged in" },
        { "002", "Disconnected: server shutdown" },
        { "003", "Login server is offline..." },
        { "004", "Invalid client secret, access has been revoked" },
        { "005", "Disconnected: Maleformed packet" },
        { "006", "Error: Unable to connect to game server..." },
        { "007", "Invalid login: {0}" },
        { "008", "Failed to register user: {0}" },
        { "009", "Critical Error occured: Please restart your client." },
        { "010", "You have been logged out." },
        { "011", "Connection lost with server..."}
    };
    public static readonly bool PRINT_DEBUGGING_LOG = true;
    public static readonly string PACKET_SPLIT = "|";
    public const int NPC_RENDER_DISTANCE = 60;
    #endregion GameLogic

    public static readonly Dictionary<Prefab, string> RESOURCE_LOCATIONS = new Dictionary<Prefab, string>()
    {
        {Prefab.CHARACTER, "Characters"},
        {Prefab.ANIMATIONS, "Animations/Controllers"},
        {Prefab.WORLDOBJECTS, "WorldObjects"}
    };

    public static readonly Dictionary<EntityGroup, string> ENTITY_CONTAINER_NAMES = new Dictionary<EntityGroup, string>()
    {
        {EntityGroup.PLAYER, "Players"},
        {EntityGroup.NPC, "NPCs"},
        {EntityGroup.WORLDOBJECT, "WorldObjects"}
    };

    public static readonly Dictionary<EntityGroup, Prefab> PREFAB_BINDINGS = new Dictionary<EntityGroup, Prefab>()
    {
        { EntityGroup.PLAYER, Prefab.CHARACTER },
        { EntityGroup.NPC, Prefab.CHARACTER },
        { EntityGroup.WORLDOBJECT, Prefab.WORLDOBJECTS }
    };

    public static readonly Dictionary<EntityGroup, Type> ENTITY_CLASS_BINDINGS = new Dictionary<EntityGroup, Type>()
    {
        { EntityGroup.PLAYER, typeof(Character) },
        { EntityGroup.NPC, typeof(Character) },
        { EntityGroup.WORLDOBJECT, typeof(WorldObject) }
    };

}


