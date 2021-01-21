using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

public enum Prefab
{
    CHARACTER, ANIMATIONS, WORLDOBJECTS
}

public class PrefabHandler : MonoBehaviour
{
    private static Dictionary<Prefab, Dictionary<string, UnityEngine.Object>> prefabCollections;
    private static List<System.Type> noneConvertables = new List<System.Type>()
    {
        typeof(RuntimeAnimatorController)
    };

    public static void Load()
    {
        if (prefabCollections == null)
        {
            Misc.PrintDebugLine("PrefabHandler", "Load", "Starting procedure...");
            Misc.PrintDebugLine("PrefabHandler", "Load", "Starting loading resources...");

            prefabCollections = new Dictionary<Prefab, Dictionary<string, UnityEngine.Object>>();
            foreach (KeyValuePair<Prefab, string> resourceLocation in Constants.RESOURCE_LOCATIONS)
            {
                Misc.PrintDebugLine("PrefabHandler", "Load->ResourceLoad", $"Loading resources for prefab group: {resourceLocation.Key}");

                Dictionary<string, Object> currentCollection = new Dictionary<string, Object>();
                foreach (UnityEngine.Object obj in Resources.LoadAll(resourceLocation.Value))
                    currentCollection.Add(obj.name, obj);

                Misc.PrintDebugLine("PrefabHandler", "Load->ResourceLoad", $"Loaded a total of #{currentCollection.Count()} assets within group");
                prefabCollections.Add(resourceLocation.Key, currentCollection);
            }
        }
        else Misc.PrintDebugLine("PrefabHandler", "Load", "Attempting to start asset load when already assigned...");
    }

    public static T Get<T>(Prefab p, string name)
    {
        if (noneConvertables.Contains(typeof(T)))
        {
            Misc.PrintDebugLine("PrefabHandler", $"Get<{typeof(T)}>", "Conversion currently not supported using T cast for this type");
            return (T)System.Convert.ChangeType(null, typeof(T));
        }

        Misc.PrintDebugLine("PrefabHandler", $"Get<{typeof(T)}>", $"Attempting to fetch prefab of group \"{p.ToString()}\" with name \"{name}\"");
        Object output = Get(p, name);
        return (T)System.Convert.ChangeType(output, typeof(T));
    }

    public static Object Get(Prefab p, string name)
    {
        Object output;
        Dictionary<string, Object> objectRoot;
        name = name.Contains("/") ? name.Split('/').Last() : name;

        if (prefabCollections.TryGetValue(p, out objectRoot))
        {
            if (objectRoot.TryGetValue(name, out output)) return output;
            else Misc.PrintDebugLine("PrefabHandler", "Get", $"Unable to fetch prefab of type \"{p.ToString()}\" named \"{name}\"");
        }
        else Misc.PrintDebugLine("PrefabHandler", "Get", $"Unable to fetch prefab group: \"{p.ToString()}\"");

        throw new System.ArgumentNullException();
    }
    
    public static GameObject CreateAsGameObject(Prefab prefabType, string prefabName, Vector3 position, Quaternion rotation, string objectName = null)
    {
        Misc.PrintDebugLine("PrefabHandler", "CreateAsGameObject", $"Attempting to create a game object with following attributes [TYPE: {prefabType.ToString()}][NAME: {prefabName}]");
        
        GameObject output = Instantiate(Get<GameObject>(prefabType, prefabName), position, rotation);
        if(objectName != null) output.name = objectName;

        return output;
    }
}