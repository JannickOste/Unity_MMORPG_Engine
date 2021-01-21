using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class ServerInit : MonoBehaviour
{

    private void Start()
    {
        this.gameObject.AddComponent<WorldHandler>();
        WorldHandler wHandler = this.gameObject.GetComponent<WorldHandler>();
        wHandler.LoadWorld();
    }
}
