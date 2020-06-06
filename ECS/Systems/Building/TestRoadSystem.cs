//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Entities;
//using Unity.Transforms;
////using Unity.Transforms2D;
//using Unity.Rendering;
//using Unity.Mathematics;
//using Unity.Jobs;
//using Unity.Collections;
//using Unity.Burst;


//public class TestRoadSystem : JobComponentSystem
//{
//    EntityCommandBufferSystem m_EntityCommandBufferSystem;

//    public EntityQuery m_gameStateGroup;
//    public EntityQuery m_RoadPathRequestQuery;
//    public EntityQuery m_tilesQuery;
//    public EntityQuery m_inputGroup;
//    public EntityQuery m_selectedTileQuery;
//    public EntityQuery m_tileMapGroup;
//    public EntityQuery m_placingRoadQuery;
//    public EntityQuery m_BuildingTypesQuery;

//    // Turn on road building mode
//    public struct StartRoadBuildingJob : IJobForEachWithEntity<UI_Command_StartRoadBuilding>
//    {
//        public EntityCommandBuffer.Concurrent CommandBuffer;  
//        public NativeArray<GameState> gameState;

//        public void Execute(Entity entity, int index, [ReadOnly] ref UI_Command_StartRoadBuilding c0)
//        {
//            var state = gameState[0];
//            state.gameState = e_GameStates.state_RoadPlacement;
//            gameState[0] = state;
//            CommandBuffer.DestroyEntity(index, entity);
//        }
//    }

//    // calculate where roads are going to be
//    public struct PreviewRoadJob : IJobForEachWithEntity<Tile>
//    {
//        public EntityCommandBuffer.Concurrent CommandBuffer;
//        [NativeDisableParallelForRestriction] public NativeArray<Tile> tileMap;
//        [NativeDisableParallelForRestriction, DeallocateOnJobCompletion] public NativeArray<Entity> requests;
//        //[ReadOnly] public NativeArray<DynamicBuffer<PathCompleteBuffer>> paths;
//        [NativeDisableParallelForRestriction, DeallocateOnJobCompletion] public NativeArray<PathRequest> pathRequests;
//        [ReadOnly] public NativeArray<GameState> gameState;
//        [ReadOnly] public BufferFromEntity<PathCompleteBuffer> paths;

//        public void Execute(Entity entity, int index, ref Tile t)
//        {
//            if (gameState[0].gameState == e_GameStates.state_RoadPlacement)
//            {
//                if (pathRequests.Length > 0)
//                {
//                    bool foundLatestPath = false;

//                    for (int j = pathRequests.Length - 1; j >= 0; j--)
//                    {
//                        var currentRequest = pathRequests[j];
//                        var currentPath = paths[requests[j]];

//                        // if we have already found the most recent completed path, destroy the rest
//                        if (foundLatestPath)
//                        {
//                            CommandBuffer.DestroyEntity(index, requests[j]);
//                        }

//                        if (currentRequest.pathComplete)
//                        {
//                            foundLatestPath = true;
//                            bool check = false;
//                            for (int i = 0; i < currentPath.Length; i++)
//                            {
//                                if (t.tileID == currentPath[i].Value)
//                                {
//                                    t.displayRoad = true;
//                                    check = true;
//                                }
//                            }

//                            if (!check)
//                            {
//                                t.displayRoad = false;
//                            }

//                            CommandBuffer.DestroyEntity(index, requests[j]);
//                        }
//                    }
//                }
//            }          
//        }
//    }

//    // display roads
//    public struct DisplayRoadJob : IJobForEachWithEntity<Tile>
//    {
//        [ReadOnly] public EntityCommandBuffer.Concurrent commandBuffer;
//        [ReadOnly] public NativeArray<BuildingTypeSpawner> spawner;

//        public void Execute(Entity entity, int index, ref Tile t)
//        {
//            if (t.displayRoad)
//            {
//                var road = commandBuffer.Instantiate(index, spawner[0].Road_Straight);
//                Translation newPos = new Translation { Value = t.tileCoord };
//                commandBuffer.SetComponent(index, road, newPos);
//                commandBuffer.AddComponent(index, road, new RoadDisplay { mapIndex = t.tileID, placing = 0 });
//            }          
//        }
//    }

//    public struct RemoveOldRoads : IJobForEachWithEntity<RoadDisplay>
//    {
//        public EntityCommandBuffer.Concurrent CommandBuffer;
//        [ReadOnly] public NativeArray<Tile> tileMap;

//        public void Execute(Entity entity, int index, ref RoadDisplay r)
//        {
//            if (!tileMap[r.mapIndex].displayRoad)
//            {
//                CommandBuffer.DestroyEntity(index, entity);
//            }
//        }
//    }


//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
//        var gameStateArray = m_gameStateGroup.ToComponentDataArray<GameState>(Allocator.TempJob);
//        var tileMap = m_tilesQuery.ToComponentDataArray<Tile>(Allocator.TempJob);

//        var requests = m_RoadPathRequestQuery.ToEntityArray(Allocator.TempJob);
//        var pathRequests = m_RoadPathRequestQuery.ToComponentDataArray<PathRequest>(Allocator.TempJob);
//        var pathRequestBuffer = GetBufferFromEntity<PathCompleteBuffer>();

//        var spawn = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);

//        var startBuildingRoadJob = new StartRoadBuildingJob
//        {
//            CommandBuffer = commandBuffer,
//            gameState = gameStateArray,

//        }.Schedule(this, inputDeps);
//        m_EntityCommandBufferSystem.AddJobHandleForProducer(startBuildingRoadJob);


//        var previewRoadJob = new PreviewRoadJob
//        {
//            CommandBuffer = commandBuffer,
//            gameState = gameStateArray,
//            tileMap = tileMap,
//            requests = requests,
//            paths = pathRequestBuffer,
//            pathRequests = pathRequests,

//        }.Schedule(this, startBuildingRoadJob);
//        m_EntityCommandBufferSystem.AddJobHandleForProducer(previewRoadJob);

//        var displayRoadJob = new DisplayRoadJob
//        {
//            commandBuffer = commandBuffer,
//            spawner = spawn,

//        }.Schedule(this, previewRoadJob);
//        m_EntityCommandBufferSystem.AddJobHandleForProducer(displayRoadJob);

//        var removeOldRaods = new RemoveOldRoads
//        {
//            CommandBuffer = commandBuffer,
//            tileMap = tileMap,
//        }.Schedule(this, displayRoadJob);

//        removeOldRaods.Complete();

//        m_tileMapGroup.CopyFromComponentDataArray(tileMap);
//        m_gameStateGroup.CopyFromComponentDataArray(gameStateArray);

//        spawn.Dispose();
//        tileMap.Dispose();
//        gameStateArray.Dispose();

//        return removeOldRaods;
//    }

//    protected override void OnCreate()
//    {
//        m_RoadPathRequestQuery = GetEntityQuery(typeof(PathRequest), typeof(PathRequestType_Road), typeof(PathCompleteBuffer));
//        m_EntityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EntityCommandBufferSystem>();
//        m_gameStateGroup = GetEntityQuery(typeof(GameState));
//        m_tilesQuery = GetEntityQuery(typeof(Tile));
//        m_inputGroup = GetEntityQuery(typeof(PlayerInput));
//        m_selectedTileQuery = GetEntityQuery(typeof(SelectedTile));
//        m_tileMapGroup = GetEntityQuery(typeof(Tile));
//        m_placingRoadQuery = GetEntityQuery(typeof(PreviewRoad));
//        m_BuildingTypesQuery = GetEntityQuery(typeof(BuildingTypeSpawner));
//    }



//}


//public struct Road : IComponentData
//{
//    public int mapIndex;
//    public int roadNumber;
//}

//public struct RoadDisplay : IComponentData
//{
//    public int mapIndex;
//    public int roadNumber;
//    public int placing;
//}

//public struct PreviewRoad : IComponentData
//{
//    public int tileIndex;
//}