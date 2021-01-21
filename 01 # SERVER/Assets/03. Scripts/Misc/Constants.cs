using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Constants
{
    #region GameLogic
    public const int TICKS_PER_SEC = 30; // How many ticks per second
    public const float MS_PER_TICK = 1000f / TICKS_PER_SEC; // How many milliseconds per tick
    public const string LOGINSERVER = "http://localhost/CamelotServer/index.php";
    public const float ENTITY_RENDER_RADIUS = 20f;
    #endregion

    public const string LOGIN_SERVER = "http://25.57.27.21/CamelotServer/index.php";
    public const string NPC_CONFIGURATION = "Configuration/NPCS.json";
    public const string OBJECT_CONFIGURATION = "Configuration/Objects.json";
    public const bool PRINT_DEBUGGING_LOG = true;
    public static readonly string PACKET_SPLIT = "|";
}