using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomFly : MonoBehaviour
{

    public float walkRadius = 50f;
    private Vector3 destPos;
    public float speed = 500f;

    // Start is called before the first frame update
    void Start()
    {
        
        destPos = Random.insideUnitSphere * walkRadius + gameObject.transform.position;    
    }

    // Update is called once per frame
    void Update()
    {

        if (gameObject.transform.position == destPos)
        {
            destPos = Random.insideUnitSphere * walkRadius + gameObject.transform.position;
        }

        gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, destPos, 3f);

        
    }
}
