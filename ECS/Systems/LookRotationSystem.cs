using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

public class LookRotationSystem : JobComponentSystem
{
    EntityQuery m_allPositions;

    public struct LookRotationJob : IJobForEachWithEntity<LookRotation, Rotation>
    {
        [ReadOnly] public ComponentDataFromEntity<Translation> allPositions;

        public void Execute(Entity entity, int index, ref LookRotation l, ref Rotation r)
        {
            if (l.gotTarget == 1)
            {
                Quaternion r2 = r.Value;
                Vector3 originalRot = r2.eulerAngles;

                Vector3 targetPos = allPositions[l.target].Value;
                Vector3 currentPos = allPositions[entity].Value;

                Quaternion rot = Quaternion.LookRotation(targetPos - currentPos, Vector3.up);
                rot *= Quaternion.AngleAxis(-90f, Vector3.right);

                Vector3 newRot = rot.eulerAngles;

                if (l.constrainX == 1)
                    newRot.x = originalRot.x;
                if (l.constrainY == 1)
                    newRot.y = originalRot.y;
                if (l.constrainZ == 1)
                    newRot.z = originalRot.z;

                r.Value = Quaternion.Euler(newRot);
            }          
        }
    }



    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var pos = GetComponentDataFromEntity<Translation>();

        var job = new LookRotationJob
        {
            allPositions = pos,
        }.Schedule(this, inputDeps);

        return job;
    }


    protected override void OnCreate()
    {
        m_allPositions = GetEntityQuery(typeof(Translation));
    }
}
