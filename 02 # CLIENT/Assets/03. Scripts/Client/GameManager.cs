using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;


    private static int loaded_scene;
    private static AsyncOperation load;

    public string clientSecret;

    public List<(int scene_id, Vector3 position, int render_offset)> mapData;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    private void Start()
    {
        PrefabHandler.Load();
        loadScenePositions();

        this.gameObject.AddComponent<Client>();
        this.gameObject.AddComponent<ThreadManager>();
        this.gameObject.AddComponent<UIHandler>();

        this.gameObject.GetComponent<UIHandler>().SetUI(typeof(StartMenu));
    }

    private void FixedUpdate()
    {
        if (mapData != null) StartCoroutine("CheckSceneDistance");
    }

    private void loadScenePositions()
    {
        GameObject mappingObject = GameObject.Find("Mapper");
        mapData = new List<(int scene_id, Vector3 position, int render_offset)>();

        foreach (Terrain terrain in GameObject.FindObjectsOfType<Terrain>())
        {
            TerrainData data = terrain.GetComponent<TerrainData>();
            // Calculate the center of the terrain.
            Vector3 size = terrain.GetComponent<Terrain>().terrainData.size;

            Vector3 position = terrain.transform.position;
            position.x += size.x / 2;
            position.z += size.z / 2;

            mapData.Add((data.scene_id, position, data.render_offset));
        }

        Destroy(mappingObject);
    }

    IEnumerator CheckSceneDistance()
    {
        if (load == null)
        {
            foreach (var data in GameManager.instance.mapData)
            {
                try
                {
                    if (data.scene_id <= SceneManager.sceneCountInBuildSettings)
                    {
                        if (Vector3.Distance(data.position, EntityHandler.GetEntity(EntityGroup.PLAYER).transform.position) <= 150 + data.render_offset)
                        {
                            if (!SceneManager.GetSceneByBuildIndex(data.scene_id).isLoaded)
                            {
                                load = SceneManager.LoadSceneAsync(data.scene_id, LoadSceneMode.Additive);
                                loaded_scene = data.scene_id;
                            }
                        }
                        else if (SceneManager.GetSceneByBuildIndex(data.scene_id).isLoaded)
                        {
                            if (SceneManager.GetSceneByBuildIndex(data.scene_id).isLoaded)
                            {
                                SceneManager.UnloadSceneAsync(data.scene_id);
                            }
                        }
                    }
                }
                catch { }

            }
        }
        else if (load.isDone) load = null;

        yield return new WaitForSeconds(2);
    }
}
