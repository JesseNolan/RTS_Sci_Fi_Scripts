using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
//using Unity.Transforms2D;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

public class RoadSystem : JobComponentSystem
{
    EntityCommandBufferSystem m_EntityCommandBufferSystem;

    EntityQuery m_gameStateGroup;
    public EntityQuery m_tilesQuery;
    public EntityQuery m_inputGroup;
    public EntityQuery m_selectedTileQuery;
    public EntityQuery m_tileMapGroup;
    public EntityQuery m_placingRoadQuery;
    public static EntityQuery m_BuildingTypesQuery;

    private static Entity[] roadList;

    private static Entity r_straight_r1;
    private static Entity r_straight_r2;
    private static Entity r_T_r1;
    private static Entity r_T_r2;
    private static Entity r_T_r3;
    private static Entity r_T_r4;
    private static Entity r_corner_r1;
    private static Entity r_corner_r2;
    private static Entity r_corner_r3;
    private static Entity r_corner_r4;
    private static Entity r_Int;

    /* TO DO
        make data type that can capture all the releveant info for doing road logic

        after all road/tile logic done, pass it to the job that "displays" it by instnatiating and destroying entities.
        ONLY destroy/instantiate entities in the last "display" job which is driven by data from the previous jobs.

        - startbuildingroadjob
        - get current path request
	        check mouse button input
	        change variable which indicates whether a raod is displayed there or not based on
	        path logic

        - display road job
	        get all the tiles (or whetever the data is on) and decide where to display road
	        and what kind of road. Keep the displaying separate from the logic above.
     */


    public struct StartRoadBuildingJob : IJobForEachWithEntity<UI_Command_StartRoadBuilding>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        //[ReadOnly] public ComponentDataFromEntity<GameState> gameStateComponentData;
        //[ReadOnly] public NativeArray<Entity> stateEntity;
        public NativeArray<GameState> gameState;

        public void Execute(Entity entity, int index, [ReadOnly] ref UI_Command_StartRoadBuilding c0)
        {
            var state = gameState[0];
            state.gameState = e_GameStates.state_RoadPlacement;
            gameState[0] = state;

            //var gameState = gameStateComponentData[stateEntity[0]];
            //gameState.gameState = e_GameStates.state_RoadPlacement;
            //CommandBuffer.SetComponent(index, stateEntity[0], gameState);


            CommandBuffer.DestroyEntity(index, entity);
        }
    }


    public struct PreviewRoadJob : IJobForEachWithEntity_EBCC<PathCompleteBuffer, PathRequest, PathRequestType_Road>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [NativeDisableParallelForRestriction] public NativeArray<Tile> tileMap;
        [ReadOnly] public BuildingTypeSpawner spawner;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Entity> roadEntities;
        [ReadOnly] public ComponentDataFromEntity<PreviewRoad> roadData;
        [ReadOnly] public NativeArray<GameState> gameState;

        public void Execute(Entity entity, int index, DynamicBuffer<PathCompleteBuffer> buff, ref PathRequest p, [ReadOnly] ref PathRequestType_Road c2)
        {
            if (p.pathComplete)
            {
                GameState state = gameState[0];

                if (state.gameState == e_GameStates.state_RoadPlacement)
                {
                    bool exists = false;
                    Entity rEnt;
                    PreviewRoad rData;
                    for (int j = 0; j < roadEntities.Length; j++)
                    {
                        rEnt = roadEntities[j];
                        rData = roadData[rEnt];

                        for (int k = 0; k < buff.Length; k++)
                            if (rData.tileIndex == buff[k])
                                exists = true;
                        // if road is not in current path, remove it and remove the has road component from tile
                        if (!exists)
                        {
                            CommandBuffer.DestroyEntity(index, rEnt);
                            Tile tempTile = tileMap[rData.tileIndex];
                            tempTile.displayRoad = false;
                            tileMap[rData.tileIndex] = tempTile;
                        }
                        exists = false;
                    }

                    for (int i = 0; i < buff.Length; i++)
                    {
                        Tile tempTile = tileMap[buff[i]];

                        if (!tempTile.displayRoad)
                        {
                            var road = CommandBuffer.Instantiate(index, spawner.Road_Straight);
                            Translation newPos = new Translation { Value = tempTile.tileCoord };
                            newPos.Value.y += 0.05f;
                            CommandBuffer.SetComponent(index, road, newPos);
                            CommandBuffer.AddComponent(index, road, new RoadDisplay { mapIndex = buff[i], placing = 1 });
                            CommandBuffer.AddComponent(index, road, new PreviewRoad { tileIndex = buff[i] });
                            CommandBuffer.AddComponent(index, road, new RoadCurrentlyBuilding());

                            tempTile.displayRoad = true;
                            tileMap[buff[i]] = tempTile;
                        }
                    }
                    CommandBuffer.DestroyEntity(index, entity);
                }
                else
                {
                    CommandBuffer.DestroyEntity(index, entity);
                }
            }
        }
    }

    public struct FinaliseRoad : IJobForEachWithEntity<RoadCurrentlyBuilding, RoadDisplay>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<PlayerInput> input;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<SelectedTile> selectedTile;
        [NativeDisableParallelForRestriction] public NativeArray<Tile> tileMap;
        [NativeDisableParallelForRestriction] public NativeArray<GameState> gameState;

        public void Execute(Entity entity, int index, [ReadOnly] ref RoadCurrentlyBuilding c0, ref RoadDisplay r)
        {
            GameState state = gameState[0];

            if (input[0].MouseButtonDown1)
            {
                Tile tempTile = tileMap[r.mapIndex];
                tempTile.isValid = 1;
                tempTile.hasRoad = false;
                tempTile.displayRoad = false;
                tileMap[r.mapIndex] = tempTile;
                CommandBuffer.DestroyEntity(index, entity);

                Debug.LogFormat("Destroyed entity: {0}", entity.Index);

                state.gameState = e_GameStates.state_Idle;
                gameState[0] = state;
            }
            else if (selectedTile[0].isValid == 1)
            {
                if (input[0].MouseButtonUp0)
                {
                    CommandBuffer.RemoveComponent<PreviewRoad>(index, entity);
                    CommandBuffer.RemoveComponent<RoadCurrentlyBuilding>(index, entity);
                    RoadDisplay tempR = r;
                    tempR.placing = 0;
                    CommandBuffer.SetComponent(index, entity, tempR);
                    Tile tempTile = tileMap[r.mapIndex];
                    tempTile.isValid = 0;
                    tempTile.hasRoad = true;
                    tileMap[r.mapIndex] = tempTile;
                }
            }
        }
    }

    public struct RoadPlacementJob : IJobForEachWithEntity<RoadDisplay, Translation, Rotation>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;
        [ReadOnly] public NativeArray<Tile> tiles;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Entity> roads;
        public int tilesPerWidth;

        public void Execute(Entity entity, int index, ref RoadDisplay road, ref Translation l, ref Rotation r)
        {
            int roadNumber;
            int ind = road.mapIndex;

            roadNumber = GetLeft(ind) * 8 + GetDown(ind) * 4 + GetRight(ind) * 2 + GetUp(ind);

            if (roadNumber != road.roadNumber)
            {
                if (road.placing == 1)
                {
                    CommandBuffer.DestroyEntity(index, entity);
                    var newEntity = CommandBuffer.Instantiate(index, roads[roadNumber]);
                    RoadDisplay newRoad = road;
                    newRoad.roadNumber = roadNumber;
                    CommandBuffer.AddComponent(index, newEntity, newRoad);
                    CommandBuffer.SetComponent(index, newEntity, l);
                    //CommandBuffer.AddComponent(index, newEntity, new PlacingRoad());
                    CommandBuffer.AddComponent(index, newEntity, new PreviewRoad { tileIndex = ind });
                    CommandBuffer.AddComponent(index, newEntity, new RoadCurrentlyBuilding());

                    //Debug.LogFormat("New road display index: {0}    new entity: {1}  version {2}", newRoad.mapIndex, newEntity.Index, newEntity.Version);

                }
                if (road.placing == 0)
                {
                    CommandBuffer.DestroyEntity(index, entity);
                    var newEntity = CommandBuffer.Instantiate(index, roads[roadNumber]);
                    RoadDisplay newRoad = road;
                    newRoad.roadNumber = roadNumber;
                    CommandBuffer.AddComponent(index, newEntity, newRoad);
                    CommandBuffer.SetComponent(index, newEntity, l);
                }
            }
        }

        private int GetLeft(int i)
        {
            if ((i - 1) < 0 || (i - 1) > tiles.Length)
                return 0;

            if (tiles[i - 1].displayRoad)
                return 1;
            else
                return 0;
        }

        private int GetRight(int i)
        {
            if ((i + 1) < 0 || (i + 1) > tiles.Length)
                return 0;
            if (tiles[i + 1].displayRoad)
                return 1;
            else
                return 0;
        }

        private int GetUp(int i)
        {
            if ((i + tilesPerWidth) < 0 || (i + tilesPerWidth) > tiles.Length)
                return 0;
            if (tiles[i + tilesPerWidth].displayRoad)
                return 1;
            else
                return 0;
        }

        private int GetDown(int i)
        {
            if ((i - tilesPerWidth) < 0 || (i - tilesPerWidth) > tiles.Length)
                return 0;
            if (tiles[i - tilesPerWidth].displayRoad)
                return 1;
            else
                return 0;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var gameStateArray = m_gameStateGroup.ToComponentDataArray<GameState>(Allocator.TempJob);

        var cb1 = new EntityCommandBuffer(Allocator.TempJob);
        var startBuildingRoadJob = new StartRoadBuildingJob
        {
            CommandBuffer = cb1.ToConcurrent(),
            gameState = gameStateArray,

        }.Schedule(this, inputDeps);
        //m_EntityCommandBufferSystem.AddJobHandleForProducer(startBuildingRoadJob);
        startBuildingRoadJob.Complete();
        cb1.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
        cb1.Dispose();

        if (gameStateArray[0].gameState == e_GameStates.state_RoadPlacement)
        {
            var tileMap = m_tilesQuery.ToComponentDataArray<Tile>(Allocator.TempJob);
            var input = m_inputGroup.ToComponentDataArray<PlayerInput>(Allocator.TempJob);
            var selected = m_selectedTileQuery.ToComponentDataArray<SelectedTile>(Allocator.TempJob);

            //var tiles = m_tilesQuery.ToEntityArray(Allocator.TempJob);
            //var tilesComponentData = GetComponentDataFromEntity<Tile>();
            //var tilesComponentDataArray = m_tilesQuery.ToComponentDataArray<Tile>(Allocator.TempJob);


            var spawn = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
            var roadEntities = m_placingRoadQuery.ToEntityArray(Allocator.TempJob);

            //var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();

            //var state = m_gameStateGroup.ToEntityArray(Allocator.TempJob);
            //var gameStateData = GetComponentDataFromEntity<GameState>();
            NativeArray<Entity> roads = new NativeArray<Entity>(roadList, Allocator.TempJob);

            
            var cb2 = new EntityCommandBuffer(Allocator.TempJob);
            var cb3 = new EntityCommandBuffer(Allocator.TempJob);
            var cb4 = new EntityCommandBuffer(Allocator.TempJob);

            

            var roadComponentData = GetComponentDataFromEntity<PreviewRoad>();

            var previewRoadJob = new PreviewRoadJob
            {
                CommandBuffer = cb2.ToConcurrent(),
                tileMap = tileMap,
                spawner = spawn[0],
                roadEntities = roadEntities,
                roadData = roadComponentData,
                gameState = gameStateArray,
            }.Schedule(this, startBuildingRoadJob);
            //m_EntityCommandBufferSystem.AddJobHandleForProducer(previewRoadJob);
            previewRoadJob.Complete();
            cb2.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
            cb2.Dispose();

            var finaliseRoadJob = new FinaliseRoad
            {
                CommandBuffer = cb3.ToConcurrent(),
                input = input,
                selectedTile = selected,
                gameState = gameStateArray,
                tileMap = tileMap,
            }.Schedule(this, previewRoadJob);
            //m_EntityCommandBufferSystem.AddJobHandleForProducer(finaliseRoadJob);
            finaliseRoadJob.Complete();
            cb3.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
            cb3.Dispose();

            var roadJob = new RoadPlacementJob
            {
                tiles = tileMap,
                CommandBuffer = cb4.ToConcurrent(),
                tilesPerWidth = TerrainSystem.tilesPerWidth,
                roads = roads,

            }.Schedule(this, finaliseRoadJob);
            //m_EntityCommandBufferSystem.AddJobHandleForProducer(roadJob);
            roadJob.Complete();
            cb4.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
            cb4.Dispose();

            //roadJob.Complete();

            m_gameStateGroup.CopyFromComponentDataArray(gameStateArray);
            m_tileMapGroup.CopyFromComponentDataArray(tileMap);


            spawn.Dispose();
            gameStateArray.Dispose();
            tileMap.Dispose();
            roads.Dispose();

            return roadJob;
        }

        m_gameStateGroup.CopyFromComponentDataArray(gameStateArray);
        gameStateArray.Dispose();

        return inputDeps;      
    }

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        m_gameStateGroup = GetEntityQuery(typeof(GameState));
        m_tilesQuery = GetEntityQuery(typeof(Tile));
        m_inputGroup = GetEntityQuery(typeof(PlayerInput));
        m_selectedTileQuery = GetEntityQuery(typeof(SelectedTile));
        m_tileMapGroup = GetEntityQuery(typeof(Tile));
        m_placingRoadQuery = GetEntityQuery(typeof(PreviewRoad));
        m_BuildingTypesQuery = GetEntityQuery(typeof(BuildingTypeSpawner));
    }


    public static void InitialiseRoads()
    {
        Debug.Log("InitialiseRoads");
        var s = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);

        r_straight_r1 = MakeNewPrefab(s[0].Road_Straight);
        r_straight_r2 = MakeNewPrefab(s[0].Road_Straight);
        MainLoader.entityManager.SetComponentData(r_straight_r2, new Rotation { Value = MainLoader.entityManager.GetComponentData<Rotation>(r_straight_r2).Value * Quaternion.Euler(0, 90, 0) });
        r_T_r1 = MakeNewPrefab(s[0].Road_T);
        r_T_r2 = MakeNewPrefab(s[0].Road_T);
        MainLoader.entityManager.SetComponentData(r_T_r2, new Rotation { Value = MainLoader.entityManager.GetComponentData<Rotation>(r_T_r2).Value * Quaternion.Euler(0, 270, 0) });
        r_T_r3 = MakeNewPrefab(s[0].Road_T);
        MainLoader.entityManager.SetComponentData(r_T_r3, new Rotation { Value = MainLoader.entityManager.GetComponentData<Rotation>(r_T_r3).Value * Quaternion.Euler(0, 180, 0) });
        r_T_r4 = MakeNewPrefab(s[0].Road_T);
        MainLoader.entityManager.SetComponentData(r_T_r4, new Rotation { Value = MainLoader.entityManager.GetComponentData<Rotation>(r_T_r4).Value * Quaternion.Euler(0, 90, 0) });
        r_corner_r1 = MakeNewPrefab(s[0].Road_Corner);
        r_corner_r2 = MakeNewPrefab(s[0].Road_Corner);
        MainLoader.entityManager.SetComponentData(r_corner_r2, new Rotation { Value = MainLoader.entityManager.GetComponentData<Rotation>(r_corner_r2).Value * Quaternion.Euler(0, 270, 0) });
        r_corner_r3 = MakeNewPrefab(s[0].Road_Corner);
        MainLoader.entityManager.SetComponentData(r_corner_r3, new Rotation { Value = MainLoader.entityManager.GetComponentData<Rotation>(r_corner_r3).Value * Quaternion.Euler(0, 180, 0) });
        r_corner_r4 = MakeNewPrefab(s[0].Road_Corner);
        MainLoader.entityManager.SetComponentData(r_corner_r4, new Rotation { Value = MainLoader.entityManager.GetComponentData<Rotation>(r_corner_r4).Value * Quaternion.Euler(0, 90, 0) });
        r_Int = MakeNewPrefab(s[0].Road_Intersection);

        roadList = new Entity[16];

        roadList[0] = r_straight_r1;
        roadList[1] = r_straight_r2;
        roadList[2] = r_straight_r1;
        roadList[3] = r_corner_r3;
        roadList[4] = r_straight_r2;
        roadList[5] = r_straight_r2;
        roadList[6] = r_corner_r2;
        roadList[7] = r_T_r2;
        roadList[8] = r_straight_r1;
        roadList[9] = r_corner_r4;
        roadList[10] = r_straight_r1;
        roadList[11] = r_T_r3;
        roadList[12] = r_corner_r1;
        roadList[13] = r_T_r4;
        roadList[14] = r_T_r1;
        roadList[15] = r_Int;

        s.Dispose();
    }


    private static Entity MakeNewPrefab(Entity oldPrefab)
    {
        var ent = MainLoader.entityManager.Instantiate(oldPrefab);
        MainLoader.entityManager.AddComponentData(ent, new Prefab());
        return ent;
    }

}



public struct Road : IComponentData
{
    public int mapIndex;
    public int roadNumber;
}

public struct RoadDisplay : IComponentData
{
    public int mapIndex;
    public int roadNumber;
    public int placing;
}

public struct PreviewRoad : IComponentData
{
    public int tileIndex;
}