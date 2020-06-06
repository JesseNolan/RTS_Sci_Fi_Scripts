using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationTest : MonoBehaviour
{

    public GameObject target;
    public bool constrainX;
    public bool constrainY;
    public bool constrainZ;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 originalRot = transform.rotation.eulerAngles;

        Quaternion rot = Quaternion.LookRotation(target.transform.position - transform.position, Vector3.up);
        rot *= Quaternion.AngleAxis(-90f, Vector3.right);

        Vector3 newRot = rot.eulerAngles;

        if (constrainX)
            newRot.x = originalRot.x;
        if (constrainY)
            newRot.y = originalRot.y;
        if (constrainZ)
            newRot.z = originalRot.z;

        transform.rotation = Quaternion.Euler(newRot);
    }

   
}
