using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static Unity.Physics.Math;
using UnityEngine.EventSystems;

namespace Unity.Physics.Extensions
{
    public class MouseRayCastSystem : JobComponentSystem
    {
        EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;
        EntityQuery m_gameState;

        private Entity previouslySelected;

        BuildPhysicsWorld m_BuildPhysicsWorldSystem;

        float k_MaxDistance = 10000.0f;

        [BurstCompile]
        struct Pick : IJob
        {
            [ReadOnly] public CollisionWorld CollisionWorld;
            [ReadOnly] public int NumStaticBodies;
            public RaycastInput RayInput;
            public NativeArray<Entity> selected;

            public void Execute()
            {
                float fraction = 1.0f;
                RigidBody? hitBody = null;
                if (CollisionWorld.CastRay(RayInput, out RaycastHit hit))
                {                 
                    if (hit.RigidBodyIndex < NumStaticBodies)
                    {
                        hitBody = CollisionWorld.Bodies[hit.RigidBodyIndex];
                        //Debug.Log(hitBody);                      
                        //commandBuffer.AddComponent(hitBody.Value.Entity, new Selected());
                        selected[0] = hitBody.Value.Entity;
                    }
                }                   
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var handle = JobHandle.CombineDependencies(inputDeps, m_BuildPhysicsWorldSystem.GetOutputDependency());
            var gameState = m_gameState.ToComponentDataArray<GameState>(Allocator.TempJob);

            if (Input.GetMouseButtonDown(0) && (Camera.main != null) && (gameState[0].gameState == e_GameStates.state_Idle) && (!EventSystem.current.IsPointerOverGameObject()))
            {
                Vector2 mousePosition = Input.mousePosition;
                UnityEngine.Ray unityRay = Camera.main.ScreenPointToRay(mousePosition);
                var ray = new UnityEngine.Ray(unityRay.origin, unityRay.direction * k_MaxDistance);
                
                var selectedEntities = new NativeArray<Entity>(1, Allocator.TempJob);

                handle = new Pick
                {
                    CollisionWorld = m_BuildPhysicsWorldSystem.PhysicsWorld.CollisionWorld,
                    NumStaticBodies = m_BuildPhysicsWorldSystem.PhysicsWorld.NumStaticBodies,
                    RayInput = new RaycastInput
                    {
                        Start = unityRay.origin,
                        End = unityRay.origin + unityRay.direction * k_MaxDistance,
                        Filter = CollisionFilter.Default,
                    },
                    selected = selectedEntities,
                }.Schedule(JobHandle.CombineDependencies(handle, m_BuildPhysicsWorldSystem.GetOutputDependency()));

                handle.Complete();

                // if we got a valid entity
                if (EntityManager.Exists(selectedEntities[0]))
                {
                    // if that entity doesnt already have the Selected type
                    if (!EntityManager.HasComponent(selectedEntities[0], typeof(Selected)))
                    {
                        // if the previously selected entity had Selected type, remove it
                        if (EntityManager.HasComponent(previouslySelected, typeof(Selected)))
                            EntityManager.RemoveComponent<Selected>(previouslySelected);
                        // give the entity the Selected type
                        EntityManager.AddComponent(selectedEntities[0], typeof(Selected));
                        
                        // update previous selected entity
                        previouslySelected = selectedEntities[0];
                    }
                } else  // else we didnt hit anything valid, remove previous Selection
                {
                    EntityManager.RemoveComponent<Selected>(previouslySelected);                   
                }

                
                selectedEntities.Dispose();
            }

            gameState.Dispose();
            return handle;
        }

        protected override void OnCreate()
        {
            m_EntityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            m_BuildPhysicsWorldSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BuildPhysicsWorld>();
            m_gameState = GetEntityQuery(typeof(GameState));
        }
    }
}