using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RandomMove : MonoBehaviour {

    NavMeshAgent m_Agent;
    // Use this for initialization

    Vector3 initialPos;

    int walkRadius = 100;

    float worldXMin;
    float worldXMax;
    float worldYMin;
    float worldYMax;


    void Start ()
    {
        m_Agent = GetComponent<NavMeshAgent>();

        initialPos = transform.position;


    }
	
	// Update is called once per frame
	void Update ()
    {

        float dist = m_Agent.remainingDistance;

        if (dist != Mathf.Infinity && m_Agent.pathStatus == NavMeshPathStatus.PathComplete && m_Agent.remainingDistance < 0.5f)
        {
            Vector3 randomDirection = initialPos + Random.insideUnitSphere * walkRadius;
            //randomDirection += transform.position;
            NavMeshHit hit;
            NavMesh.SamplePosition(randomDirection, out hit, walkRadius, 1);
            Vector3 finalPosition = hit.position;
            m_Agent.SetDestination(finalPosition);
        }




        

       

	}
}
