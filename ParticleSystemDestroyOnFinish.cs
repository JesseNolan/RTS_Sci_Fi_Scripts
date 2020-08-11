using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystemDestroyOnFinish : MonoBehaviour
{
    private ParticleSystem p;
    // Start is called before the first frame update
    void Start()
    {
        ParticleSystem p = gameObject.GetComponent<ParticleSystem>();
        if (p != null)
        {
            float d = p.duration + p.startLifetime;
            Destroy(gameObject, d);
        }
    }

}
