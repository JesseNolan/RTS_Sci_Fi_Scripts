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
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    EntityQuery m_gameStateGroup;
    public EntityQuery m_tilesQuery;
    public EntityQuery m_inputGroup;
    public EntityQuery m_selectedTileQuery;
    public EntityQuery m_tileMapGroup;
    public static EntityQuery m_BuildingTypesQuery;
    public EntityQuery m_pathBuffer;


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
        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();          
        var input = m_inputGroup.ToComponentDataArray<PlayerInput>(Allocator.TempJob);
        var selected = m_selectedTileQuery.ToComponentDataArray<SelectedTile>(Allocator.TempJob);
        var spawn = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
        int tilesPerWidth = TerrainSystem.tilesPerWidth;
        var pathRequests = m_pathBuffer.ToComponentDataArray<PathRequest>(Allocator.TempJob);
        var pathEntities = m_pathBuffer.ToEntityArray(Allocator.TempJob);

        NativeList<int> roadToDestroy = new NativeList<int>(Allocator.TempJob);
        NativeList<int> pathIndexes = new NativeList<int>(Allocator.TempJob);

        if (pathRequests.Length > 0 && gameState[0].gameState == e_GameStates.state_RoadPlacement && pathRequests[0].pathComplete)
        {
                
            PathRequest p = pathRequests[0];
            DynamicBuffer<PathCompleteBuffer>  buff = GetBuffer<PathCompleteBuffer>(pathEntities[0]);

            for (int i = 0; i < buff.Length; i++)
            {
                pathIndexes.Add(buff[i]);
            }    
        }

        var RoadSpawnJob = Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref Tile t) =>
            {
                GameState state = gameState[0];

                if (pathIndexes.Length > 0)
                {
                    bool inBuff = false;
                    for (int b = 0; b < pathIndexes.Length; b++)
                    {
                        if (pathIndexes[b] == t.tileID)
                        {
                            inBuff = true;
                        }
                    }

                    if (inBuff)
                    {
                        //Debug.Log("Road display enabled");
                        t.displayRoad = true;
                    }
                    else if (!t.hasRoad)
                    {
                        t.displayRoad = false;
                    }

                    for (int i = 0; i < pathEntities.Length; i++)
                    {
                        commandBuffer.DestroyEntity(entityInQueryIndex, pathEntities[i]);
                    }
                }

                if (state.gameState != e_GameStates.state_RoadPlacement && !t.hasRoad)
                {
                    t.displayRoad = false;
                }

                if (t.displayRoad && !t.roadEntityPresent)
                {
                    // create a display road
                    t.roadEntityPresent = true;

                    var road = commandBuffer.Instantiate(entityInQueryIndex, spawn[0].Road);
                    Translation newPos = new Translation { Value = t.tileCoord };
                    newPos.Value.y += 0.05f;
                    commandBuffer.SetComponent(entityInQueryIndex, road, newPos);
                    commandBuffer.AddComponent(entityInQueryIndex, road, new RoadDisplay { mapIndex = t.tileID, placing = 0 });
                }

                if (!t.displayRoad && t.roadEntityPresent)
                {
                    // mark the road for destruction
                    roadToDestroy.Add(t.tileID);
                    t.roadEntityPresent = false;
                }

                if (input[0].MouseButtonUp0)
                {
                    if (t.displayRoad && !t.hasRoad && t.isValid == 1)
                    {
                        t.hasRoad = true;
                        t.isValid = 0;
                    }
                } else if (input[0].MouseButtonDown1)
                {
                    if (!t.hasRoad)
                    {
                        t.displayRoad = false;
                    }

                    state.gameState = e_GameStates.state_Idle;
                    gameState[0] = state;
                }

            }).Schedule(Dependency);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(RoadSpawnJob);

        var tileMap = m_tileMapGroup.ToComponentDataArray<Tile>(Allocator.TempJob);

        var RoadPlacementJob = Entities
            .ForEach((Entity entity, int entityInQueryIndex, in RoadDisplay road) =>
            {
                if (roadToDestroy.Contains(road.mapIndex))
                {
                    commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                }
                else
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
                                if (HasComponent<RoadMaterial_RoadTypeEnable>(children[i].Value) && HasComponent<RoadMaterial_Rotation>(children[i].Value))
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
                    }
                }

            }).Schedule(RoadSpawnJob);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(RoadPlacementJob);

        RoadPlacementJob.Complete();


        m_gameStateGroup.CopyFromComponentDataArray(gameState);

        tileMap.Dispose();
        roadToDestroy.Dispose();
        pathIndexes.Dispose();
        spawn.Dispose();
        input.Dispose();
        selected.Dispose();
        pathRequests.Dispose();
        pathEntities.Dispose();
        

        gameState.Dispose();    
    }

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        m_gameStateGroup = GetEntityQuery(typeof(GameState));
        m_tilesQuery = GetEntityQuery(typeof(Tile));
        m_inputGroup = GetEntityQuery(typeof(PlayerInput));
        m_selectedTileQuery = GetEntityQuery(typeof(SelectedTile));
        m_tileMapGroup = GetEntityQuery(typeof(Tile));
        m_BuildingTypesQuery = GetEntityQuery(typeof(BuildingTypeSpawner));
        m_pathBuffer = GetEntityQuery(typeof(PathRequest), typeof(PathRequestType_Road));
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