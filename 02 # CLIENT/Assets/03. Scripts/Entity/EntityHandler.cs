using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;

public enum EntityGroup
{
    NPC = 0, 
    PLAYER = 1,
    WORLDOBJECT = 2
}

class EntityHandler : MonoBehaviour
{
    private static Dictionary<EntityGroup, Dictionary<int, Entity>> entitys;
    private static Dictionary<string, GameObject> entityContainers;
    private static string[] requiredEntityKeys = new[] { "id", "model_name", "position", "rotation" };


    #region Loading & Updating

    public static void Load()
    {
        entityContainers = new Dictionary<string, GameObject>();
        GameObject entitys = new GameObject("Entitys");

        foreach (string containerName in Constants.ENTITY_CONTAINER_NAMES.Values)
        {
            GameObject container = new GameObject(containerName);
            container.transform.SetParent(entitys.transform);

            entityContainers.Add(containerName, container);
        }

        EntityHandler.entitys = new Dictionary<EntityGroup, Dictionary<int, Entity>>();
        foreach (EntityGroup group in Enum.GetValues(typeof(EntityGroup)))
        {
            EntityHandler.entitys.Add(group, new Dictionary<int, Entity>());
        }

        DontDestroyOnLoad(entitys);

    }

    public void FixedUpdate()
    {
        StartCoroutine("CheckLoadedEntitys");
    }

    IEnumerator CheckLoadedEntitys()
    {
        yield return new WaitForFixedUpdate();

        if (entitys != null)
        {
            foreach (Character p in GetCharacters().Where(p => p.lastUpdate != null))
            {
                if (p.lastUpdate.AddSeconds(3) < System.DateTime.Now)
                    DestroyEntity(p.type, p.id);
            }
        }
    }
    #endregion

    #region Global entity functions
    public static void AddEntityToContainer(EntityGroup entityType, GameObject entityObject) =>
        entityObject.transform.SetParent(entityContainers[Constants.ENTITY_CONTAINER_NAMES[entityType]].transform);


    public static Entity GetEntity(EntityGroup entityType, int id = -1)
    {
        id = (id != -1 ? id : Client.instance.myId);
        Entity target = null;

        if (!entitys[entityType].TryGetValue(id, out target))
        {
            Misc.PrintDebugLine("EntityHandler", "GetEntity", $"Failed to fetch entity [ID: {id}][Group: {entityType.ToString()}]");
            return null;
        }

        return target;
    }

    public static T GetEntity<T>(EntityGroup entityType, int id = -1)
    {
        return (T)System.Convert.ChangeType(GetEntity(entityType, id), typeof(T));
        
    }

    public static void DestroyEntity(EntityGroup type, int id)
    {
        Entity target;

        if (entitys[type].TryGetValue(id, out target))
        {
            Misc.PrintDebugLine("EntityHandler", $"DestroyEntity<{type.ToString()}>", $"Attempting to destroy entity with id {id}");
            if (target.gameObject != null)
            {
                Destroy(target.gameObject);
            }
            entitys[type].Remove(id);
        }
        else Misc.PrintDebugLine("EntityHandler", $"DestroyEntity<{type.ToString()}>", $"Attempting to destroy an entity that doesnt exist...");
    }

    public static void SpawnEntity(Dictionary<string, object> entityData)
    {

        Type entityType = null;
        if (!entityData.ContainsKey("entity_type")) Debug.Log("[HandlePacket]: Invalid entity spawn received");
        else
        {
            EntityGroup entityGroup = (EntityGroup)entityData["entity_type"];
            switch (entityGroup)
            {
                case EntityGroup.PLAYER:
                case EntityGroup.NPC:
                    entityType = typeof(Character);
                    break;

                case EntityGroup.WORLDOBJECT:
                    entityType = typeof(WorldObject);
                    break;
            }

            if (entityType == null) return;
        }

        Entity entity;
        string[] requiredHits = requiredEntityKeys.Where(key => entityData.Keys.Contains(key)).ToArray();
        if (requiredHits.Length != requiredEntityKeys.Length)
        {
            Misc.PrintDebugLine("EntityHandler", $"SpawnEntity", $"The following required keys where not found within entityData: { string.Join(",", requiredEntityKeys.Where(i => !requiredHits.Contains(i)))}");
            return;
        }

        (EntityGroup group, Type classType) entityGroupAndTypeData = ((EntityGroup)entityData["entity_type"], Constants.ENTITY_CLASS_BINDINGS[(EntityGroup)entityData["entity_type"]]);
        if (entitys[entityGroupAndTypeData.group].TryGetValue(int.Parse(entityData["id"].ToString()), out entity))
        {
            if(entity != null)
            {
                Reflector.DictionaryToInstance(entityData, entity);
                if (entityGroupAndTypeData.classType == typeof(Character))
                {
                    ((Character)entity.GetComponent(typeof(Character))).lastUpdate = System.DateTime.Now;
                }
            }
        }
        else CreateEntity(entityGroupAndTypeData, entityData);
    }
    #endregion

    #region Entity Spawning

    private static void CreateEntity((EntityGroup group, Type classType) entityGroupAndTypeData, Dictionary<string, object> entityData)
    {
        try
        {
            Prefab prefabGroup;

            if (!Constants.PREFAB_BINDINGS.TryGetValue(entityGroupAndTypeData.group, out prefabGroup))
            {
                Misc.PrintDebugLine("EntityHandler", "CreateEntity", $"Failed to create an entity of {entityGroupAndTypeData.group} with the following attributes \"{string.Join(",", entityData.Keys)}\"");
                if (new[] { EntityGroup.PLAYER, EntityGroup.NPC }.Contains(entityGroupAndTypeData.group)) prefabGroup = Prefab.CHARACTER;
                else if (entityGroupAndTypeData.group == EntityGroup.WORLDOBJECT) prefabGroup = Prefab.WORLDOBJECTS;
            }


            GameObject model = PrefabHandler.CreateAsGameObject(prefabType: prefabGroup,
                                                            prefabName: entityData["model_name"].ToString(),
                                                            position: (Vector3)entityData["position"],
                                                            rotation: (Quaternion)entityData["rotation"]);
            
            if(model != null)
            {
                model.AddComponent(entityGroupAndTypeData.classType);
                Component targetGroup = model.GetComponent(entityGroupAndTypeData.classType);
                ((Entity)targetGroup).type = entityGroupAndTypeData.group;
                Reflector.DictionaryToInstance(entityData, targetGroup);

                if (entityGroupAndTypeData.classType == typeof(WorldObject) && entityData.ContainsKey("scale"))
                {
                    model.GetComponent(typeof(WorldObject)).transform.localScale = (Vector3)entityData["scale"];
                }

                AddEntityToContainer(entityGroupAndTypeData.group, model);

                int id;
                if (int.TryParse(entityData["id"].ToString(), out id))
                if (entitys[entityGroupAndTypeData.group].ContainsKey(id))
                {
                    Destroy(entitys[entityGroupAndTypeData.group][id].gameObject);
                    entitys[entityGroupAndTypeData.group][id] = model.GetComponent<Character>();
                }
                else entitys[entityGroupAndTypeData.group].Add(id, model.GetComponent<Character>());
            }





            
        } 
        catch(System.ArgumentNullException ex)
        {
            Misc.PrintDebugLine("EntityHandler", "CreateEntity", $"Exception caputered: {ex.Message}");
        }
    }

    public static List<Character> GetCharacters()
    {
        List<Character> stack = new List<Character>();

        foreach (EntityGroup type in new[] { EntityGroup.PLAYER, EntityGroup.NPC })
            stack.AddRange(entitys[type].Values.Select(i => i as Character));

        return stack;
    }
    #endregion
}