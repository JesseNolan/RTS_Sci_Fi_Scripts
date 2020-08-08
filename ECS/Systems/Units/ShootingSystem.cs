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
public class ShootingSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    EntityQuery m_Positions;
    EntityQuery m_ProjectileSpawner;
    EntityQuery m_enemyQuery;
    EntityQuery m_projectileQuery;
    EntityQuery m_bufferQuery;


    public struct SpawnProjectileJob : IJobForEachWithEntity<FriendlyUnit, Weapon, LocalToWorld>
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<HelloSpawner> spawner;
        [NativeDisableParallelForRestriction] public BufferFromEntity<ProjectileBuffer> projectileBuffer;
        [ReadOnly] public ComponentDataFromEntity<EnemyUnit> enemyData;

        public void Execute(Entity entity, int index, [ReadOnly] ref FriendlyUnit fU, ref Weapon w, ref LocalToWorld t)
        {
            if (w.gotTarget == 1)
            {
                if (enemyData.HasComponent(w.targetEntity))
                {
                    if (w.firingTimer == 0)
                    {
                        // FIRE!
                        var instance = CommandBuffer.Instantiate(index, spawner[0].Prefab);
                        Projectile projectile = new Projectile
                        {
                            dst = w.targetEntity,
                            speed = w.projectileSpeed,
                            damage = w.damage,
                            placedInBuffer = false,
                            targetHit = false,
                        };

                        CommandBuffer.SetComponent(index, instance, projectile);
                        CommandBuffer.SetComponent(index, instance, t);

                        w.firingTimer = w.firingRate;
                    }
                    else
                        w.firingTimer--;
                }
                
            }
        }
    }

    [BurstCompile]
    public struct MoveProjectile : IJobForEachWithEntity<Projectile, Translation, Rotation>
    {
        [ReadOnly] public ComponentDataFromEntity<LocalToWorld> localToWorld;

        public void Execute(Entity entity, int index, ref Projectile p, ref Translation t, ref Rotation r)
        {
            Vector3 currentPos = localToWorld[entity].Position;
            Vector3 dstPos;
            dstPos = localToWorld[p.dst].Value.c3.xyz;

            float dist = math.distance(currentPos, dstPos);

            if (dist < 1)
            {
                p.targetHit = true;
            } else
            {
                Vector3 newPos = Vector3.MoveTowards(currentPos, dstPos, p.speed);

                Quaternion r1 = r.Value;
                Vector3 dir = Vector3.RotateTowards(r1 * Vector3.forward, currentPos - dstPos, 1000f, 0f);
                r.Value = Quaternion.LookRotation(dir);
                t.Value = newPos;
            }          
        }
    }


    public struct ProcessProjectileHits : IJobForEachWithEntity<EnemyUnit>
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        [ReadOnly] public NativeArray<Projectile> projectiles;
        [ReadOnly] public NativeArray<Entity> pEnt;

        public void Execute(Entity entity, int index, ref EnemyUnit e)
        {
            for (int i = 0; i < projectiles.Length; i++)
            {
                if ((projectiles[i].dst == entity) && (projectiles[i].targetHit))
                {
                    if (e.health > 0)
                    {
                        e.health -= projectiles[i].damage;
                        CommandBuffer.DestroyEntity(index, pEnt[i]);
                    }
                    else
                    {
                        CommandBuffer.DestroyEntity(index, pEnt[i]);
                    }
                }              
            }

            for (int i = 0; i < projectiles.Length; i++)
            {
                if ((e.health <= 0) && projectiles[i].dst == entity)
                {
                    //Debug.LogFormat("Health: {0} ", e.health);
                    CommandBuffer.DestroyEntity(index, pEnt[i]);
                }
            }

            if (e.health <= 0)
                CommandBuffer.DestroyEntity(index, entity);
        }
    }

    //public struct DeathJob : IJobForEachWithEntity<EnemyUnit>
    //{
    //    public EntityCommandBuffer.Concurrent CommandBuffer;
    //    [NativeDisableParallelForRestriction, ReadOnly] public NativeArray<Entity> projectileEntities;
    //    [NativeDisableParallelForRestriction, ReadOnly] public ComponentDataFromEntity<Projectile> projectileData;

    //    public void Execute(Entity entity, int index, ref EnemyUnit e)
    //    {
    //        if (e.health <= 0)
    //        {
    //            // iterate over all projectiles, if any remaining projectiles have the enemy 
    //            // as a target, destroy those projectiles too
    //            for (int i = 0; i < projectileEntities.Length; i++)
    //            {
    //                if (projectileData[projectileEntities[i]].dst == entity)
    //                {
    //                    CommandBuffer.DestroyEntity(index, projectileEntities[i]);
    //                }
    //            }
    //            CommandBuffer.DestroyEntity(index, entity);
    //        }               
    //    }
    //}

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var spawner = m_ProjectileSpawner.ToComponentDataArray<HelloSpawner>(Allocator.TempJob);     

        var cb1 = new EntityCommandBuffer(Allocator.TempJob);
        var enemyData = GetComponentDataFromEntity<EnemyUnit>();
        var spawnjob = new SpawnProjectileJob
        {
            CommandBuffer = cb1.AsParallelWriter(),
            spawner = spawner,
            enemyData = enemyData,
        }.Schedule(this, inputDeps);
        spawnjob.Complete();
        cb1.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
        cb1.Dispose();


        //var assignJob = new AssignProjectileToBuffer
        //{
        //    projectileEntities = projectileEntities,
        //    projectileComponentData = projectileComponentData,
        //}.Schedule(this, spawnjob);
        //assignJob.Complete();


        var localToWorld = GetComponentDataFromEntity<LocalToWorld>();
        var movejob = new MoveProjectile
        {  
            localToWorld = localToWorld,

        }.Schedule(this, spawnjob);
        movejob.Complete();
        

        var cb2 = new EntityCommandBuffer(Allocator.TempJob);
        var projectiles = m_projectileQuery.ToComponentDataArray<Projectile>(Allocator.TempJob);
        var pEnt = m_projectileQuery.ToEntityArray(Allocator.TempJob);
        var processProjHits = new ProcessProjectileHits
        {
            CommandBuffer = cb2.AsParallelWriter(),
            projectiles = projectiles,
            pEnt = pEnt,

        }.Schedule(this, movejob);
        processProjHits.Complete();
        cb2.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
        cb2.Dispose();

        pEnt.Dispose();
        projectiles.Dispose();


        //var cb3 = new EntityCommandBuffer(Allocator.TempJob);
        //var projectileBuffer2 = GetBufferFromEntity<ProjectileBuffer>();
        //var projectileEntities = m_projectileQuery.ToEntityArray(Allocator.TempJob);
        //var projectileComponentData = GetComponentDataFromEntity<Projectile>(true);
        //var deathJob = new DeathJob
        //{
        //    CommandBuffer = cb3.ToConcurrent(),
        //    projectileEntities = projectileEntities,
        //    projectileData = projectileComponentData,
        //}.Schedule(this, processProjHits);
        //deathJob.Complete();
        //cb3.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
        //cb3.Dispose();
        //projectileEntities.Dispose();


        return processProjHits;
    }


    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        m_Positions = GetEntityQuery(typeof(Translation));
        m_ProjectileSpawner = GetEntityQuery(typeof(HelloSpawner), typeof(LocalToWorld));
        m_enemyQuery = GetEntityQuery(typeof(EnemyUnit));
        m_projectileQuery = GetEntityQuery(typeof(Projectile), typeof(LocalToWorld), typeof(Translation), typeof(Rotation));
    }
}



[InternalBufferCapacity(256)]
public struct ProjectileBuffer : IBufferElementData
{
    public static implicit operator Entity(ProjectileBuffer e) { return e.Value; }
    public static implicit operator ProjectileBuffer(Entity e) { return new ProjectileBuffer { Value = e }; }

    public Entity Value;
}


