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

public class RoadSystem : SystemBase
{
    BeginSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    EntityQuery m_gameStateGroup;
    public EntityQuery m_tilesQuery;
    public EntityQuery m_inputGroup;
    public EntityQuery m_selectedTileQuery;
    public EntityQuery m_tileMapGroup;
    public EntityQuery m_previewRoadQuery;
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

    private static int GetLeft(int i, NativeArray<Tile> tiles)
    {
        if ((i - 1) < 0 || (i - 1) > tiles.Length)
            return 0;

        if (tiles[i - 1].displayRoad)
            return 1;
        else
            return 0;
    }

    private static int GetRight(int i, NativeArray<Tile> tiles)
    {
        if ((i + 1) < 0 || (i + 1) > tiles.Length)
            return 0;
        if (tiles[i + 1].displayRoad)
            return 1;
        else
            return 0;
    }

    private static int GetUp(int i, NativeArray<Tile> tiles, int tilesPerWidth)
    {
        if ((i + tilesPerWidth) < 0 || (i + tilesPerWidth) > tiles.Length)
            return 0;
        if (tiles[i + tilesPerWidth].displayRoad)
            return 1;
        else
            return 0;
    }

    private static int GetDown(int i, NativeArray<Tile> tiles, int tilesPerWidth)
    {
        if ((i - tilesPerWidth) < 0 || (i - tilesPerWidth) > tiles.Length)
            return 0;
        if (tiles[i - tilesPerWidth].displayRoad)
            return 1;
        else
            return 0;
    }


    protected override void OnUpdate()
    {
        var gameState = m_gameStateGroup.ToComponentDataArray<GameState>(Allocator.TempJob);

        if (gameState[0].gameState == e_GameStates.state_RoadPlacement)
        {
            var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            var tileMap = m_tilesQuery.ToComponentDataArray<Tile>(Allocator.TempJob);
            var input = m_inputGroup.ToComponentDataArray<PlayerInput>(Allocator.TempJob);
            var selected = m_selectedTileQuery.ToComponentDataArray<SelectedTile>(Allocator.TempJob);
            var spawn = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
            var previewRoadEntities = m_previewRoadQuery.ToEntityArray(Allocator.TempJob);

            int tilesPerWidth = TerrainSystem.tilesPerWidth;

            var PreviewRoadJob = Entities
                .ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<PathCompleteBuffer> buff, ref PathRequest p, in PathRequestType_Road c2) =>
                {
                    if (p.pathComplete)
                    {
                        GameState state = gameState[0];

                        if (state.gameState == e_GameStates.state_RoadPlacement)
                        {
                            bool exists = false;
                            Entity rEnt;
                            PreviewRoad rData;
                            for (int j = 0; j < previewRoadEntities.Length; j++)
                            {
                                rEnt = previewRoadEntities[j];
                                rData = GetComponent<PreviewRoad>(previewRoadEntities[j]);

                                for (int k = 0; k < buff.Length; k++)
                                    if (rData.tileIndex == buff[k])
                                    {
                                        exists = true;
                                    }
                                // if road is not in current path, remove it and remove the has road component from tile
                                if (!exists)
                                {
                                    //Debug.LogFormat("Destroyed road map index {0}", rData.tileIndex);
                                    commandBuffer.DestroyEntity(entityInQueryIndex, rEnt);
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
                                    var road = commandBuffer.Instantiate(entityInQueryIndex, spawn[0].Road);
                                    Translation newPos = new Translation { Value = tempTile.tileCoord };
                                    newPos.Value.y += 0.05f;
                                    commandBuffer.SetComponent(entityInQueryIndex, road, newPos);
                                    commandBuffer.AddComponent(entityInQueryIndex, road, new RoadDisplay { mapIndex = buff[i], placing = 1 });
                                    commandBuffer.AddComponent(entityInQueryIndex, road, new PreviewRoad { tileIndex = buff[i] });
                                    commandBuffer.AddComponent(entityInQueryIndex, road, new RoadCurrentlyBuilding { });

                                    tempTile.displayRoad = true;
                                    tileMap[buff[i]] = tempTile;
                                }
                            }
                            commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                        }
                        else
                        {
                            commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                        }
                    }
                }).Schedule(Dependency);
            m_EntityCommandBufferSystem.AddJobHandleForProducer(PreviewRoadJob);


            var FinaliseRoadJob = Entities
                .ForEach((Entity entity, int entityInQueryIndex, ref RoadCurrentlyBuilding c0, ref RoadDisplay r) =>
                {
                    GameState state = gameState[0];

                    if (input[0].MouseButtonDown1)
                    {
                        Tile tempTile = tileMap[r.mapIndex];
                        tempTile.isValid = 1;
                        tempTile.hasRoad = false;
                        tempTile.displayRoad = false;
                        tileMap[r.mapIndex] = tempTile;
                        commandBuffer.DestroyEntity(entityInQueryIndex, entity);

                        //Debug.LogFormat("Destroyed entity: {0}", entity.Index);

                        state.gameState = e_GameStates.state_Idle;
                        gameState[0] = state;
                    }
                    else if (selected[0].isValid == 1)
                    {
                        if (input[0].MouseButtonUp0)
                        {
                            commandBuffer.RemoveComponent<PreviewRoad>(entityInQueryIndex, entity);
                            commandBuffer.RemoveComponent<RoadCurrentlyBuilding>(entityInQueryIndex, entity);
                            RoadDisplay tempR = r;
                            tempR.placing = 0;
                            commandBuffer.SetComponent(entityInQueryIndex, entity, tempR);
                            Tile tempTile = tileMap[r.mapIndex];
                            tempTile.isValid = 0;
                            tempTile.hasRoad = true;
                            tileMap[r.mapIndex] = tempTile;
                        }
                    }

                }).Schedule(PreviewRoadJob);
            m_EntityCommandBufferSystem.AddJobHandleForProducer(FinaliseRoadJob);

            var RoadPlacementJob = Entities
               .ForEach((Entity entity, int entityInQueryIndex, ref RoadDisplay road, ref Translation l, ref Rotation r) =>
               {
                    int roadNumber;
                    int ind = road.mapIndex;

                    roadNumber = GetLeft(ind, tileMap) * 8 + GetDown(ind, tileMap, tilesPerWidth) * 4 + GetRight(ind, tileMap) * 2 + GetUp(ind, tileMap, tilesPerWidth);

                    if (roadNumber != road.roadNumber)
                    {

                        BufferFromEntity<Child> cb = GetBufferFromEntity<Child>();
                        if (cb.HasComponent(entity))
                        {
                            DynamicBuffer<Child> children = GetBuffer<Child>(entity);
                            for (int i = 0; i < children.Length; i++)
                            {
                                RoadMaterial_RoadTypeEnable roadType = GetComponent<RoadMaterial_RoadTypeEnable>(children[i].Value);
                                RoadMaterial_Rotation rot = GetComponent<RoadMaterial_Rotation>(children[i].Value);

                                switch (roadNumber)
                                {
                                    case 0:
                                    case 4:
                                    case 5:
                                    case 1:
                                        // straight upwards
                                        roadType.Value = 0;
                                        roadType.Value.x = 1.0f;
                                        rot.Value = 90.0f;
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, roadType);
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, rot);
                                        break;
                                    case 2:
                                    case 10:
                                    case 8:
                                        // straight left to right
                                        roadType.Value = 0;
                                        roadType.Value.x = 1.0f;
                                        rot.Value = 0.0f;
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, roadType);
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, rot);
                                        break;
                                    case 6:
                                        // corner 90 deg
                                        roadType.Value = 0;
                                        roadType.Value.y = 1.0f;
                                        rot.Value = 90.0f;
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, roadType);
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, rot);
                                        break;
                                    case 3:
                                        // corner 180 deg
                                        roadType.Value = 0;
                                        roadType.Value.y = 1.0f;
                                        rot.Value = 180.0f;
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, roadType);
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, rot);
                                        break;
                                    case 9:
                                        // corner 270 deg
                                        roadType.Value = 0;
                                        roadType.Value.y = 1.0f;
                                        rot.Value = 270.0f;
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, roadType);
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, rot);
                                        break;
                                    case 12:
                                        // corner 0 deg
                                        roadType.Value = 0;
                                        roadType.Value.y = 1.0f;
                                        rot.Value = 0.0f;
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, roadType);
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, rot);
                                        break;
                                    case 14:
                                        // T 0 deg
                                        roadType.Value = 0;
                                        roadType.Value.z = 1.0f;
                                        rot.Value = 0f;
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, roadType);
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, rot);
                                        break;
                                    case 7:
                                        // T 90 deg
                                        roadType.Value = 0;
                                        roadType.Value.z = 1.0f;
                                        rot.Value = 90.0f;
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, roadType);
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, rot);
                                        break;
                                    case 11:
                                        // T 180 deg
                                        roadType.Value = 0;
                                        roadType.Value.z = 1.0f;
                                        rot.Value = 180.0f;
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, roadType);
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, rot);
                                        break;
                                    case 13:
                                        // T 270 deg
                                        roadType.Value = 0;
                                        roadType.Value.z = 1.0f;
                                        rot.Value = 270.0f;
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, roadType);
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, rot);
                                        break;
                                    case 15:
                                        // intersection
                                        roadType.Value = 0;
                                        roadType.Value.w = 1.0f;
                                        rot.Value = 0f;
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, roadType);
                                        commandBuffer.SetComponent(entityInQueryIndex, children[i].Value, rot);
                                        break;

                                    default:
                                        break;
                                }

                            }
                        }
                    }         

               }).Schedule(FinaliseRoadJob);
            m_EntityCommandBufferSystem.AddJobHandleForProducer(RoadPlacementJob);

            RoadPlacementJob.Complete();
            m_gameStateGroup.CopyFromComponentDataArray(gameState);
            m_tileMapGroup.CopyFromComponentDataArray(tileMap);

            spawn.Dispose();
            tileMap.Dispose();
            input.Dispose();
            selected.Dispose();
            previewRoadEntities.Dispose();
        }

        gameState.Dispose();    
    }

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        m_gameStateGroup = GetEntityQuery(typeof(GameState));
        m_tilesQuery = GetEntityQuery(typeof(Tile));
        m_inputGroup = GetEntityQuery(typeof(PlayerInput));
        m_selectedTileQuery = GetEntityQuery(typeof(SelectedTile));
        m_tileMapGroup = GetEntityQuery(typeof(Tile));
        m_previewRoadQuery = GetEntityQuery(typeof(PreviewRoad));
        m_BuildingTypesQuery = GetEntityQuery(typeof(BuildingTypeSpawner));
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