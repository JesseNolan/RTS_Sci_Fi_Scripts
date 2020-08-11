using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GO_Spawner : MonoBehaviour
{
    private static GO_Spawner _instance;

    public static GO_Spawner Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public GameObject explosion;


}
