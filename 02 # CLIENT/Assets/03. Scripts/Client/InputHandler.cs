using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public enum KEY
{
    FORWARD, BACKWARDS, LEFT, RIGHT, RUNNING, ATTACK, MAINMENU, INTERACT
}

class InputHandler
{
    private static Dictionary<KEY, KeyCode> inputs = new Dictionary<KEY, KeyCode>()
    {
        { KEY.FORWARD, KeyCode.Z },
        { KEY.BACKWARDS, KeyCode.S },
        { KEY.LEFT, KeyCode.Q },
        { KEY.RIGHT, KeyCode.D },
        { KEY.RUNNING, KeyCode.LeftShift },
        { KEY.ATTACK, KeyCode.Mouse0},
        { KEY.MAINMENU, KeyCode.Escape },
        { KEY.INTERACT, KeyCode.E }
    };

    public static bool keyPresed(KEY targetCheck) => inputs.ContainsKey(targetCheck) && Input.GetKey(inputs[targetCheck]);
    public static IEnumerable<bool> GetServerInputs() => new[] { KEY.FORWARD, KEY.BACKWARDS, KEY.LEFT, KEY.RIGHT, KEY.RUNNING, 
                                                                    KEY.ATTACK, KEY.INTERACT}.Select(i => Input.GetKey(inputs[i]));

}