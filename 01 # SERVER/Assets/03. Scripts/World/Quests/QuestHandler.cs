using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

class QuestHandler : MonoBehaviour
{
    private static Dictionary<int, Quest> quests = new Dictionary<int, Quest>()
        {
            { 0, new BirthOfAHero() }
        };

    public static Dictionary<int, int> GetQuestsList()
    {
        return Enumerable.Range(0, quests.Count()).ToDictionary(k => k, v => -1);
    }
}

