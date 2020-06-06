using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;


public class AI_Citizens : JobComponentSystem
{
    public EntityCommandBufferSystem commandBuffer;

    public EntityQuery q_generalSpawner;
    public EntityQuery q_translation;
    public EntityQuery q_tiles;


    public struct Citizen_State_Job : IJobForEachWithEntity<AI_Citizen_State, Citizen, Translation>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [ReadOnly] public int terrainWidth;
        [ReadOnly] public int terrainDecimate;
        [ReadOnly] public int terrainDecimatedWidth;

        public void Execute(Entity entity, int index, ref AI_Citizen_State s, ref Citizen c, ref Translation t)
        {
            Vector3 curr;

            switch (s.currentState)
            {
                case e_AI_Citizen_States.idle:
                    s.currentState = e_AI_Citizen_States.goToWork;

                    PathRequest np = new PathRequest
                    {
                        startIndex = GetTileIndexFromWorldPos(c.home_Pos, terrainDecimate, terrainDecimatedWidth),
                        endIndex = GetTileIndexFromWorldPos(c.job_Pos, terrainDecimate, terrainDecimatedWidth),
                        pathComplete = false,
                        roadBuilding = false,
                        roadPathing = true,
                    };

                    CommandBuffer.AddBuffer<PathCompleteBuffer>(index, entity);
                    CommandBuffer.AddComponent(index, entity, np);
                    CommandBuffer.AddComponent(index, entity, typeof(PathRequestType_AIPath));

                    c.reachedDst = false;
                    c.startedPath = false;

                    break;
                case e_AI_Citizen_States.goToWork:
                   

                    //check if the unit has reached its destination
                    if (c.reachedDst)
                    {
                        c.timer++;
                        if (c.timer == c.idleTime)
                        {
                            c.timer = 0;
                            //c.dst = c.home;
                            s.currentState = e_AI_Citizen_States.goHome;

                            var home = GetTileIndexFromWorldPos(c.home_Pos, terrainDecimate, terrainDecimatedWidth);
                            var work = GetTileIndexFromWorldPos(c.job_Pos, terrainDecimate, terrainDecimatedWidth);

                            PathRequest p = new PathRequest
                            {
                                startIndex = work,
                                endIndex = home,
                                pathComplete = false,
                                roadBuilding = false,
                                roadPathing = true,
                            };
                            
                            CommandBuffer.AddBuffer<PathCompleteBuffer>(index, entity);
                            CommandBuffer.AddComponent(index, entity, p);
                            CommandBuffer.AddComponent(index, entity, typeof(PathRequestType_AIPath));

                            c.reachedDst = false;
                            c.startedPath = false;
                        }            
                    }  
                    break;
                case e_AI_Citizen_States.goHome:
                  
                    if (c.reachedDst)
                    {
                        c.timer++;
                        if (c.timer == c.idleTime)
                        {
                            c.timer = 0;
                            //c.dst = c.job;
                            s.currentState = e_AI_Citizen_States.goToWork;

                            var home = GetTileIndexFromWorldPos(c.home_Pos, terrainDecimate, terrainDecimatedWidth);
                            var work = GetTileIndexFromWorldPos(c.job_Pos, terrainDecimate, terrainDecimatedWidth);

                            PathRequest p = new PathRequest
                            {
                                startIndex = home,
                                endIndex = work,
                                pathComplete = false,
                                roadBuilding = false,
                                roadPathing = true
                            };

                            CommandBuffer.AddBuffer<PathCompleteBuffer>(index, entity);
                            CommandBuffer.AddComponent(index, entity, p);
                            CommandBuffer.AddComponent(index, entity, typeof(PathRequestType_AIPath));

                            c.reachedDst = false;
                            c.startedPath = false;
                        }
                    }
                    break;
                default:
                    //c.dst = c.home;
                    s.currentState = e_AI_Citizen_States.idle;
                    break;
            }
        }

        private int GetTileIndexFromWorldPos(float3 pos, int tileWidth, int tilesPerWidth)
        {
            int xIndex = (int)pos.x / tileWidth;
            int yIndex = (int)pos.z / tileWidth;

            //Mathf.Clamp(xIndex, 0, tilesPerWidth);
            //Mathf.Clamp(yIndex, 0, tilesPerWidth);

            int arrayIndex = yIndex * (tilesPerWidth) + xIndex;

            return arrayIndex;
        }
    }

    public struct ProcessAIPaths : IJobForEachWithEntity_EBCCC<PathCompleteBuffer, PathRequest, PathRequestType_AIPath, Citizen>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Entity> tileMap;
        [ReadOnly] public int terrainWidth;
        [ReadOnly] public int terrainDecimate;
        [ReadOnly] public int terrainDecimatedWidth;

        public void Execute(Entity entity, int index, DynamicBuffer<PathCompleteBuffer> buff, ref PathRequest p, [ReadOnly] ref PathRequestType_AIPath c2, ref Citizen citizen)
        {
            if (p.pathComplete)
            {
                if (!citizen.startedPath)
                {
                    citizen.dst = tileMap[buff[buff.Length - 1]];
                    citizen.startedPath = true;
                }
                else
                {
                    if (citizen.completedNode)
                    {
                        // if we are at the end

                        int indx = GetTileIndexFromWorldPos(citizen.dst_Pos, terrainDecimate, terrainDecimatedWidth);

                        if (indx == buff[0])
                        {
                            CommandBuffer.RemoveComponent<PathCompleteBuffer>(index, entity);
                            CommandBuffer.RemoveComponent<PathRequest>(index, entity);
                            CommandBuffer.RemoveComponent<PathRequestType_AIPath>(index, entity);

                            citizen.reachedDst = true;
                        }
                        else
                        {
                            for (int i = 0; i < buff.Length; i++)
                            {
                                if (citizen.dst == tileMap[buff[i]])
                                {
                                    citizen.dst = tileMap[buff[i - 1]];
                                }
                            }
                        }
                        citizen.completedNode = false;
                    }
                }
            }
        }

        private int GetTileIndexFromWorldPos(float3 pos, int tileWidth, int tilesPerWidth)
        {
            int xIndex = (int)pos.x / tileWidth;
            int yIndex = (int)pos.z / tileWidth;

            //Mathf.Clamp(xIndex, 0, tilesPerWidth);
            //Mathf.Clamp(yIndex, 0, tilesPerWidth);

            int arrayIndex = yIndex * (tilesPerWidth) + xIndex;

            return arrayIndex;
        }
    }


    [BurstCompile]
    public struct CalculatePositionFromEntity : IJobForEachWithEntity<Citizen>
    {
        [ReadOnly] public ComponentDataFromEntity<Tile> tiles;

        public void Execute(Entity entity, int index, ref Citizen c)
        {
            Tile t = tiles[c.dst];
            c.dst_Pos = t.tileCoord;
        }
    }


    [BurstCompile]
    public struct MoveCitizens : IJobForEachWithEntity<Citizen, Translation>
    {
        [ReadOnly] public float deltaTime;

        public void Execute(Entity entity, int index, ref Citizen c, ref Translation t)
        {
            Vector3 dst = c.dst_Pos;
            Vector3 newPos = Vector3.MoveTowards(t.Value, dst, deltaTime * 20);

            if (newPos == dst)
                c.completedNode = true;

            t.Value = newPos;
        }
    }



    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var spawner = q_generalSpawner.ToComponentDataArray<GeneralSpawner>(Allocator.TempJob);
        var translation = GetComponentDataFromEntity<Translation>(false);

        var idle_Job = new Citizen_State_Job
        {
            CommandBuffer = commandBuffer.CreateCommandBuffer().ToConcurrent(),
            terrainWidth = TerrainSystem.terrainWidth,
            terrainDecimate = TerrainSystem.tileWidth,
            terrainDecimatedWidth = TerrainSystem.tilesPerWidth,
        }.Schedule(this, inputDeps);

        var processAIPaths = new ProcessAIPaths
        {
            CommandBuffer = commandBuffer.CreateCommandBuffer().ToConcurrent(),
            tileMap = q_tiles.ToEntityArray(Allocator.TempJob),
            terrainWidth = TerrainSystem.terrainWidth,
            terrainDecimate = TerrainSystem.tileWidth,
            terrainDecimatedWidth = TerrainSystem.tilesPerWidth,
        }.Schedule(this, idle_Job);

        var calcPos_Job = new CalculatePositionFromEntity
        {
            tiles = GetComponentDataFromEntity<Tile>()
        }.Schedule(this, processAIPaths);

        var moveCitizens_Job = new MoveCitizens
        {
            deltaTime = Time.DeltaTime,
        }.Schedule(this, calcPos_Job);

        spawner.Dispose();

        return moveCitizens_Job;

    }

    protected override void OnCreate()
    {
        commandBuffer = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        q_generalSpawner = GetEntityQuery(typeof(GeneralSpawner));
        q_translation = GetEntityQuery(typeof(Translation));
        q_tiles = GetEntityQuery(typeof(Tile));
    }
}


public struct AI_Citizen_State : IComponentData
{
    public e_AI_Citizen_States currentState;
}

public struct Citizen : IComponentData
{
    public Entity dst;
    public Entity job;
    public Entity home;
    public Vector3 dst_Pos;
    public Vector3 job_Pos;
    public Vector3 home_Pos;
    public int timer;
    public int idleTime;
    public bool completedNode;
    public bool reachedDst;
    public bool startedPath;
}


public enum e_AI_Citizen_States
{
    idle,
    goToWork,
    goHome
};


public struct PathRequestType_AIPath : IComponentData
{
}