using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldHandler : MonoBehaviour
{
    private GameObject npcPrefab;
    private bool scenes_loaded = false;
    private bool frame_updated = false;
    List<AsyncOperation> SceneLoadStack;

    private void FixedUpdate()
    {
        /* Load objects requires frame to have been updated after load, otherwise objects wont be able to be detected */
        if(SceneLoadStack.Count(i => i.isDone) == SceneManager.sceneCountInBuildSettings-1)
        {
            Debug.Log("# Starting server...");
            GameObject networkManager = new GameObject("Network Manager");
            networkManager.AddComponent<ThreadManager>();
            networkManager.AddComponent<NetworkManager>();
            DontDestroyOnLoad(networkManager);

            Destroy(this.gameObject);
        }
    }

    public void LoadWorld()
    {
        SceneLoadStack = new List<AsyncOperation>();
        foreach (int scene_id in Enumerable.Range(1, SceneManager.sceneCountInBuildSettings-1))
        {
            SceneLoadStack.Add(SceneManager.LoadSceneAsync(scene_id, LoadSceneMode.Additive));
        }

        if(ConfigurationPathsValid())
        {
            LoadPrefabs();
            ActionHandler.Load();
            DataHandler.Load();
            EntityHandler.Load();
        }
    }

    private bool ConfigurationPathsValid()
    {
        string[] configuration_paths = new[]
        {
            Constants.NPC_CONFIGURATION,
            Constants.OBJECT_CONFIGURATION
        };

        bool error = false;
        foreach(string configuration_path in configuration_paths)
        {
            List<string> directory_path = configuration_path.Split('/').ToList();

            if(!System.IO.Directory.Exists($"{Application.dataPath}/{string.Join(",", directory_path.GetRange(0, directory_path.Count()-1))}") | !System.IO.File.Exists($"{Application.dataPath}/{configuration_path}"))
            {
                Debug.Log($"# Unable to detect {configuration_path}, Aborting server load...");
                error = true;
            }
        }

        return !error;
    }

    private void LoadPrefabs()
    {
        Debug.Log("# Loading prefabs...");
        npcPrefab = Resources.Load("Prefabs/NPC") as GameObject;

    }

}
