using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public enum ActionType
{
    SERVER, ENTITY
}

public class ActionHandler : MonoBehaviour
{
    public static Dictionary<int, Action<Entity>> entityActions;
    private static GameObject actionHandler;

    private static ActionPacket GetPacket(System.Type type) => (ActionPacket)actionHandler.GetComponent(type);

    public static void Load()
    {
        actionHandler = new GameObject("ActionHandler");
        new List<Type>()
        {
            typeof(Teleport),
            typeof(OpenChat),
        }.ForEach(type => actionHandler.AddComponent(type));

        entityActions = new Dictionary<int, Action<Entity>>();
    }

    public static Action<Entity> EntityAction(int actionID)
    {
        switch (actionID)
        {
            case 0:
                return new Action<Entity>((target) =>
                {
                    if (Vector3.Distance(EntityHandler.GetEntity<NPC>(0).transform.position, target.transform.position) <= 3f)
                    {
                        ActionPacket packet = (ActionPacket)actionHandler.GetComponent(typeof(OpenChat));

                        if ((target as Player).questStateLib[0] <= 0) GetPacket(typeof(OpenChat)).Invoke(target.id, 0);
                        else if ((target as Player).questStateLib[0] == 1) GetPacket(typeof(OpenChat)).Invoke(target.id, 1);
                    }
                    else EntityHandler.DestroyEntity<Player>(target);
                });

            default:
                return null;
        }

    }


}

