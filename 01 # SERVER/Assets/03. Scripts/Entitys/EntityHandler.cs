using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    private static Dictionary<EntityGroup, GameObject> entityContainers;
    private static Dictionary<EntityGroup, UnityEngine.Object> defaultPrefabs;

    public static void SetEntityToContainer(EntityGroup type, GameObject entity) =>
        entity.transform.SetParent(entityContainers[type].transform);

    private static IEnumerable<Type> GetEntityTypes() => Assembly.GetAssembly(typeof(Entity)).GetTypes().Where(type => type.IsInstanceOfType(typeof(Entity)));
    public static Dictionary<int, Entity> GetEntitysObjectsInGroup(EntityGroup group) => entitys[group];



    public static Entity GetEntity(EntityGroup type, int id)
    {
        Entity entity = null;
        entitys[type].TryGetValue(id, out entity);

        return entity;
    }

    public static T GetEntity<T>(int id)
    {
        foreach (EntityGroup group in Enum.GetValues(typeof(EntityGroup)))
        {
            Entity target = entitys[group].Where(i => i.Value.id == id).Select(i => i.Value).FirstOrDefault();
            if(target != null && (target.GetType() == typeof(T))) 
                return (T)System.Convert.ChangeType(target, typeof(T));
        }

        throw new ArgumentNullException();
    }

    public static void DestroyEntity<T>(Entity target)
    {
        if(typeof(T) == typeof(Player))
        {
            DataHandler.UpdatePlayerData(target as Player);
        } 
        
        entitys[target.entityType].Remove(target.id);
        Destroy(target.gameObject);
    }

    public static void InstantiateEntity<T>(int id, string username = null, params string[] parameters)
    {

        new Dictionary<Type, Action>()
        {
            {
                typeof(Player), new Action(() =>
                {
                    var model = Instantiate(Resources.Load("Prefabs/Characters/Player") as GameObject, Vector3.zero, Quaternion.identity);

                    Player player = model.GetComponent(typeof(Player)) as Player;
                    player.Initialize(id, username);

                    entitys[EntityGroup.PLAYER].Add(id, player);
                    SetEntityToContainer(EntityGroup.PLAYER, model);
                })
            },

            {
                typeof(NPC), new Action(() =>
                {
                    GameObject model = Instantiate(Resources.Load("Prefabs/NPC"), Vector3.zero, Quaternion.identity) as GameObject;
                    model.AddComponent<NPC>();
                    NPC target = model.GetComponent<NPC>();

                    target.Initialize(id);

                    entitys[EntityGroup.NPC].Add(id, target);
                    SetEntityToContainer(EntityGroup.NPC, model);
                })
            },

            {
                typeof(WorldObject), new Action(() =>
                {
                    if(parameters.Length >= 2)
                    {
                        GameObject model = Instantiate(Resources.Load($"WorldObjects/{parameters[0]}/{parameters[1]}"), Vector3.zero, Quaternion.identity) as GameObject;
                        model.AddComponent<WorldObject>();
                        WorldObject target = model.GetComponent<WorldObject>();
                        target.Initialize(id);
                        Debug.Log(target.model_name);

                        entitys[EntityGroup.WORLDOBJECT].Add(id, target);
                        SetEntityToContainer(EntityGroup.WORLDOBJECT, model);
                    }
                })
            },
            {
                typeof(Entity), new Action(() => throw new NotImplementedException())
            }
        }[typeof(T)]();
    }

    public static void Load()
    {
        entityContainers = new Dictionary<EntityGroup, GameObject>();
        entitys = new Dictionary<EntityGroup, Dictionary<int, Entity>>();

        GameObject containerRoot = new GameObject("Entitys");
        foreach (EntityGroup type in Enum.GetValues(typeof(EntityGroup)))
        {
            entitys.Add(type, new Dictionary<int, Entity>());
            entityContainers.Add(type, new GameObject(type.ToString()));

            entityContainers[type].transform.SetParent(containerRoot.transform);
        }

        DataHandler.GetNPCData().Keys.ToList().ForEach(id => InstantiateEntity<NPC>(id));
        DataHandler.GetObjectData().ToList().ForEach(pair => InstantiateEntity<WorldObject>(pair.Key, parameters: pair.Value.model_name.Split('/')));

    }

}