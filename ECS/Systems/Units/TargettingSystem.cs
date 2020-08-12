using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;

public class TargettingSystem : SystemBase
{
    EntityQuery m_enemyQuery;
    EntityQuery m_friendlyQuery;
    EntityQuery m_constrainedQuery;

    protected override void OnCreate()
    {
        m_enemyQuery = GetEntityQuery(typeof(EnemyUnit), typeof(Translation));
        m_friendlyQuery = GetEntityQuery(typeof(FriendlyUnit), typeof(Translation));
        m_constrainedQuery = GetEntityQuery(typeof(ConstrainedRotation));
    }



    protected override void OnUpdate()
    {
        var enemyEntities_JobData = m_enemyQuery.ToEntityArray(Allocator.TempJob);
        var friendlyEntities = m_friendlyQuery.ToEntityArray(Allocator.TempJob);

        // This task gets all entities that are friendly units with a weapon and finds the closest
        // enemy unit to that unit. If the enemy is within weapon range, it is targetted
        var friendlyTarget = Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref Weapon w, in FriendlyUnit c0) =>
            {
                LocalToWorld fP = GetComponent<LocalToWorld>(entity);

                if (HasComponent<Target>(w.targetEntity) && w.gotTarget == 1)
                {
                    // if we currently have a target, check to see if it is still in range and return if true               
                    LocalToWorld position = GetComponent<LocalToWorld>(w.targetEntity);
                    float mag = math.distance(fP.Position, position.Position);
                    //check to see if target is still within firing distance, if not, get new target
                    if (mag <= w.firingDistance)
                    {
                       
                    }
                    else
                    {
                        w.gotTarget = 0;
                    }
                }
                else
                {
                    w.gotTarget = 0;

                    float closest = Mathf.Infinity;
                    int closestIndex = 0;

                    for (int i = 0; i < enemyEntities_JobData.Length; i++)
                    {
                        LocalToWorld enemyPosition = GetComponent<LocalToWorld>(enemyEntities_JobData[i]);
                        float mag = math.distance(fP.Position, enemyPosition.Position);

                        // find the closest enemy
                        if (closest < 0)
                        {
                            closest = mag;
                            closestIndex = i;
                        }
                        else
                        {
                            if (closest > mag)
                            {
                                closest = mag;
                                closestIndex = i;
                            }
                        }
                    }

                    if (closest <= w.firingDistance)
                    {
                        w.gotTarget = 1;
                        w.firingTimer = 0;
                        w.targetEntity = enemyEntities_JobData[closestIndex];
                    }
                }
            }
            ).Schedule(Dependency);

        friendlyTarget.Complete();


        var enemyTarget = Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref Weapon w, in EnemyUnit c0) =>
            {
                LocalToWorld fP = GetComponent<LocalToWorld>(entity);

                if (HasComponent<Target>(w.targetEntity) && w.gotTarget == 1)
                {
                    // if we currently have a target, check to see if it is still in range and return if true               
                    LocalToWorld position = GetComponent<LocalToWorld>(w.targetEntity);
                    float mag = math.distance(fP.Position, position.Position);
                    //check to see if target is still within firing distance, if not, get new target
                    if (mag <= w.firingDistance)
                        return;
                }
                else
                {
                    w.gotTarget = 0;

                    float closest = Mathf.Infinity;
                    int closestIndex = 0;

                    for (int i = 0; i < friendlyEntities.Length; i++)
                    {
                        LocalToWorld friendlyPosition = GetComponent<LocalToWorld>(friendlyEntities[i]);
                        float mag = math.distance(fP.Position, friendlyPosition.Position);

                        // find the closest enemy
                        if (closest < 0)
                        {
                            closest = mag;
                            closestIndex = i;
                        }
                        else
                        {
                            if (closest > mag)
                            {
                                closest = mag;
                                closestIndex = i;
                            }
                        }
                    }

                    if (closest <= w.firingDistance)
                    {
                        w.gotTarget = 1;
                        w.firingTimer = 0;
                        w.targetEntity = friendlyEntities[closestIndex];
                    }
                }
            }
            ).Schedule(friendlyTarget);

        
        enemyTarget.Complete();


        enemyEntities_JobData.Dispose();
        friendlyEntities.Dispose();
    }

}

