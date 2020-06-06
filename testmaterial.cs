using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testmaterial : MonoBehaviour
{
    Material[] mat;
    int count = 0;
    int value = 0;

    // Start is called before the first frame update
    void Start()
    {
        mat = gameObject.GetComponent<MeshRenderer>().materials;
    }

    // Update is called once per frame
    void Update()
    {
        if (count == 10)
        {
            mat[1].SetInt("_Enable", value);

            if (value == 1)
                value = 0;
            else if (value == 0)
                value = 1;


            count = 0;
        }

        count++;
    }
}
