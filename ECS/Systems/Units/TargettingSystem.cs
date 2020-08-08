using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;

public class TargettingSystem : JobComponentSystem
{
    EntityQuery m_enemyGroup;
  
    [BurstCompile]
    struct TargettingJob : IJobForEachWithEntity<FriendlyUnit, Weapon>
    {
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Entity> enemyEntities;
        [ReadOnly] public ComponentDataFromEntity<EnemyUnit> enemyData;
        [ReadOnly] public ComponentDataFromEntity<LocalToWorld> allPositions;

        public void Execute([ReadOnly] Entity friendlyEntity, [ReadOnly] int index, [ReadOnly] ref FriendlyUnit c0, ref Weapon  w)
        {
            LocalToWorld fP = allPositions[friendlyEntity];

            if (enemyData.HasComponent(w.targetEntity) && w.gotTarget == 1)
            {
                // if we currently have a target, check to see if it is still in range and return if true               
                LocalToWorld position = allPositions[w.targetEntity];
                float mag = math.distance(fP.Position, position.Position);
                //check to see if target is still within firing distance, if not, get new target
                if (mag <= w.firingDistance)
                    return;
                        
            }
            else
            {
                w.gotTarget = 0;

                for (int i = 0; i < enemyEntities.Length; i++)
                {
                    LocalToWorld enemyPosition = allPositions[enemyEntities[i]];
                    float mag = math.distance(fP.Position, enemyPosition.Position);
                    if (mag <= w.firingDistance)
                    {
                        w.gotTarget = 1;
                        w.firingTimer = 0;
                        w.targetEntity = enemyEntities[i];
                    }
                }
            }                  
        }
    }

    protected override void OnCreate()
    {
        m_enemyGroup = GetEntityQuery(typeof(EnemyUnit), typeof(Translation));
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {       
        var enemyEntities_JobData = m_enemyGroup.ToEntityArray(Allocator.TempJob);
        var enemyData = GetComponentDataFromEntity<EnemyUnit>(true);
        var job = new TargettingJob
        {
            enemyEntities = enemyEntities_JobData,
            allPositions = GetComponentDataFromEntity<LocalToWorld>(),
            enemyData = enemyData,
        };
        return job.Schedule(this, inputDeps);
    }
}

