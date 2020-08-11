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

[UpdateAfter(typeof(TargettingSystem))]
public class ShootingSystem : SystemBase
{
    BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    EntityQuery m_ProjectileSpawner;
    EntityQuery m_targetQuery;


    protected override void OnUpdate()
    {
        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        var spawner = m_ProjectileSpawner.ToComponentDataArray<GeneralSpawner>(Allocator.TempJob);     

        var SpawnJob = Entities.ForEach((Entity entity, int entityInQueryIndex, ref Weapon w, ref LocalToWorld t) =>
        {
            if (w.gotTarget == 1)
            {
                if (HasComponent<Target>(w.targetEntity))
                {
                    if (w.firingTimer == 0)
                    {
                        // FIRE!
                        var instance = commandBuffer.Instantiate(entityInQueryIndex, spawner[0].projectile);
                        Projectile projectile = new Projectile
                        {
                            dst = w.targetEntity,
                            dstVec = t.Position,    // default the destination vector to its starting position
                            speed = w.projectileSpeed,
                            damage = w.damage,
                            placedInBuffer = false,
                            targetHit = false,
                            markForDestroy = false
                        };

                        commandBuffer.SetComponent(entityInQueryIndex, instance, projectile);
                        commandBuffer.SetComponent(entityInQueryIndex, instance, t);

                        w.firingTimer = w.firingRate;
                    }
                    else
                        w.firingTimer--;
                }     
            }
        }).Schedule(Dependency);

        var targets = m_targetQuery.ToComponentDataArray<Target>(Allocator.TempJob);
        var targetEntities = m_targetQuery.ToEntityArray(Allocator.TempJob);

        var UpdateLocationJob = Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref Projectile p) =>
        {
            if (targetEntities.Contains(p.dst))
            {
                p.dstVec = GetComponent<LocalToWorld>(p.dst).Position;
            }
        }).Schedule(SpawnJob);


        var MoveProjectileJob = Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref Projectile p, ref Translation t, ref Rotation r) => 
            {
                Vector3 currentPos = GetComponent<LocalToWorld>(entity).Position;

                float dist = math.distance(currentPos, p.dstVec);

                if (dist < 1)
                {
                    if (targetEntities.Contains(p.dst))
                    {
                        p.targetHit = true;
                    } else
                    {
                        p.markForDestroy = true;
                    }
                }
                else
                {
                    Vector3 newPos = Vector3.MoveTowards(currentPos, p.dstVec, p.speed);

                    Quaternion r1 = r.Value;
                    Vector3 dir = Vector3.RotateTowards(r1 * Vector3.forward, currentPos - p.dstVec, 1000f, 0f);
                    r.Value = Quaternion.LookRotation(dir);
                    t.Value = newPos;
                }

            }).Schedule(UpdateLocationJob);


        var ProcessHitsJob = Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref Projectile p) =>
            {
                if (p.targetHit)
                {
                    if (targetEntities.Contains(p.dst))
                    {
                        int i = targetEntities.IndexOf<Entity>(p.dst);

                        Target t = targets[i];
                        t.health -= p.damage;
                        targets[i] = t;
                    }
                    p.markForDestroy = true;                   
                }
            }).Schedule(MoveProjectileJob);


        ProcessHitsJob.Complete();
        m_targetQuery.CopyFromComponentDataArray(targets);

        var DestroyProjectiles = Entities
            .ForEach((Entity entity, int entityInQueryIndex, in Projectile p) =>
            {
                if (p.markForDestroy)
                {
                    commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                }
            }).Schedule(ProcessHitsJob);

        DestroyProjectiles.Complete();

        var DestroyUnits = Entities
            .ForEach((Entity entity, int entityInQueryIndex, in Target t) =>
            {
                if (t.health <= 0)
                {
                    commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                }
            }).Schedule(ProcessHitsJob);

        DestroyUnits.Complete();


        m_EntityCommandBufferSystem.AddJobHandleForProducer(ProcessHitsJob);

        spawner.Dispose();
        targets.Dispose();
        targetEntities.Dispose();

    }


    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        m_ProjectileSpawner = GetEntityQuery(typeof(GeneralSpawner));
        m_targetQuery = GetEntityQuery(typeof(Target));
    }
}
