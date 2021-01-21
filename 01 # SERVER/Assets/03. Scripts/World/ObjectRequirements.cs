using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectRequirements : MonoBehaviour
{
    public int quest_id = -1;
    public int requiredQuestState = -1;

    public bool IgnoreColissionAllowed(Player target)
    {
        if (quest_id != -1 && requiredQuestState != -1)
        {
            int clientQuestState;

            if (target.questStateLib.TryGetValue(quest_id, out clientQuestState))
            {
                if (clientQuestState >= this.requiredQuestState)
                {
                    return true;
                }
            }
        };

        return false;
    }

}
 