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


public class LookAtCamera : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;
    EntityQuery m_rotationQuery;

    public struct CameraFaceJob : IJobForEachWithEntity<CameraFacing, LocalToWorld, Parent>
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        [ReadOnly] public float3 camera;
        [ReadOnly] public ComponentDataFromEntity<Rotation> allRotations;

        public void Execute(Entity entity, int Index, [ReadOnly] ref CameraFacing c, [ReadOnly] ref LocalToWorld t, ref Parent p)
        {
            Vector3 currentPos = t.Position;
            Vector3 dstPos = camera;

            Quaternion parentRotation = allRotations[p.Value].Value;

            //Quaternion r1 = r.Value;
            //Vector3 dir = Vector3.RotateTowards(r1 * Vector3.forward, currentPos - dstPos, 1000f, 0f);

            Quaternion r2 = Quaternion.Inverse(parentRotation) * Quaternion.LookRotation(Vector3.up, dstPos - currentPos);

            Rotation newRotation = new Rotation { Value = r2 };

            CommandBuffer.SetComponent(Index, entity, newRotation);
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var rotation = GetComponentDataFromEntity<Rotation>();

        var lookJob = new CameraFaceJob
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter(),
            camera = Camera.main.transform.position,
            allRotations = rotation,
        }.Schedule(this, inputDeps);

        return lookJob;
    }

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        m_rotationQuery = GetEntityQuery(typeof(Rotation));
    }
}
