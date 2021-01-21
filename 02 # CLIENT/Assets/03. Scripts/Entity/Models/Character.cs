using System.Collections.Generic;
using UnityEngine;

class Character : Entity
{
    public string username;
    public int hitpoints;
    public string state;
    public Dictionary<int, int> questStates;
    public System.DateTime lastUpdate;


    private void Start()
    {
        switch(this.type)
        {
            case EntityGroup.PLAYER:
                if (this.id == Client.instance.myId)
                {
                    this.gameObject.AddComponent<CharacterController>();
                }
                else Destroy(this.GetComponentInChildren<Camera>().gameObject);
                this.gameObject.name = username;
                break;
            case EntityGroup.NPC:
                this.name = this.id.ToString();
                break;
        }

        SetAnimationController();
    }

    public void UpdateComponent(Dictionary<string, object> componentValues)
    {
        if (componentValues.ContainsKey("target"))
        {
            string targetName = componentValues["target"].ToString();
            switch (targetName.ToUpper())
            {
                case "QUESTS":
                    Debug.Log("Quest update disabled");
                    /*EntityHandler.GetEntity<Character>(EntityGroup.PLAYER, Client.instance.myId).questStates = targetName
                                                                                                                .Split('|')
                                                                                                                .ToDictionary(s => int.Parse(s.Split(':').First()),
                                                                                                                              s => int.Parse(s.Split(':').Last()));
                   ¨*/
                    break;
            }
        }
    }

    private void SetAnimationController()
    {
        Animator animatorTarget;
        if ((animatorTarget = this.GetComponent<Animator>()) != null)
        {
            try
            {
                RuntimeAnimatorController controller = PrefabHandler.Get(Prefab.ANIMATIONS, "PlayerAnimations") as RuntimeAnimatorController;
                animatorTarget.runtimeAnimatorController = controller;
            }
            catch (System.ArgumentNullException ex)
            {
                Debug.Log(ex.Message);
            }
        }
    }
}