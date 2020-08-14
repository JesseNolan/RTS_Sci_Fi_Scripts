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

[UpdateAfter(typeof(PlayerInputSystem))]
public class BuildSystem : SystemBase
{
    BeginSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    public static EntityQuery m_gameStateGroup;
    public static EntityQuery m_BuildingTypesQuery;

    public EntityQuery m_tilesQuery;
    public EntityQuery m_inputGroup;
    public EntityQuery m_selectedTileQuery;
    public EntityQuery m_tileMapGroup;
    public EntityQuery m_placingRoadQuery;
    public EntityQuery q_buildingQuery;

    private static Entity GetBuildingPrefab(Building b, BuildingTypeSpawner s)
    {
        switch (b.buildingType)
        {
            case e_BuildingTypes.Terran_Habitat:
                return s.Terran_Habitat;
            case e_BuildingTypes.Terran_House:
                return s.Terran_House;
            case e_BuildingTypes.Terran_Resident_Block:
                return s.Terran_ResidentBlock;
            case e_BuildingTypes.Terran_Energy_Sphere:
                return s.Terran_EnergySphere;
            case e_BuildingTypes.Terran_Aqua_Store:
                return s.Terran_AquaStore;
            case e_BuildingTypes.Terran_Plasma_Cannon:
                return s.Terran_PlasmaCannon;
            default:
                Debug.Log("No building type found");
                return Entity.Null;
        }
    }

    private static Entity GetBuildingConstructionPrefab(Building b, BuildingTypeSpawner s)
    {
        switch (b.buildingType)
        {
            case e_BuildingTypes.Terran_Habitat:
                return s.Terran_Habitat_Construction;
            case e_BuildingTypes.Terran_House:
                return s.Terran_House_Construction;
            case e_BuildingTypes.Terran_Resident_Block:
                return s.Terran_ResidentBlock_Construction;
            case e_BuildingTypes.Terran_Energy_Sphere:
                return s.Terran_EnergySphere_Construction;
            case e_BuildingTypes.Terran_Aqua_Store:
                return s.Terran_AquaStore_Construction;
            case e_BuildingTypes.Terran_Plasma_Cannon:
                return s.Terran_PlasmaCannon_Construction;
            default:
                return Entity.Null;
        }
    }

    // Templates are done as (x1,y1), (x2,y2) style with the bounding box being defined by diagonal corners.
    private static bool CheckTemplateValid(SelectedTile selected, NativeArray<Tile> tileMap, float2x2 template, int tilesPerWidth)
    {
        int x1 = (int)template.c0.x;
        int y1 = (int)template.c0.y;
        int x2 = (int)template.c1.x;
        int y2 = (int)template.c1.y;

        bool evenX = (x2 - x1 + 1) % 2 == 0 ? true : false;
        bool evenY = (y2 - y1 + 1) % 2 == 0 ? true : false;

        int tileAnchorX;
        int tileAnchorY;

        if (evenX)
            tileAnchorX = ((x2 - x1 + 1) / 2) - 1;
        else
            tileAnchorX = ((x2 - x1 + 1) / 2);

        if (evenY)
            tileAnchorY = ((y2 - y1 + 1) / 2) - 1;
        else
            tileAnchorY = ((y2 - y1 + 1) / 2);

        // get the start index (0,0) position removing the offset of the tile anchor
        int startIndex = selected.tileID - (tileAnchorY * tilesPerWidth) - tileAnchorX;

        for (int x = 0; x < (x2 - x1 + 1); x++)
        {
            for (int y = 0; y < (y2 - y1 + 1); y++)
            {
                int currentIndex = startIndex + (y * tilesPerWidth) + x;

                if (currentIndex > 0)
                {
                    Tile testTile = tileMap[currentIndex];
                    if (testTile.isValid == 0)
                        return false;
                }
                else
                {
                    return false;
                }

            }
        }

        return true;
    }

    private static void SetTemplate(SelectedTile selected, NativeArray<Tile> tileMap, float2x2 template, int tilesPerWidth, GameState state)
    {
        int x1 = (int)template.c0.x;
        int y1 = (int)template.c0.y;
        int x2 = (int)template.c1.x;
        int y2 = (int)template.c1.y;

        bool evenX = (x2 - x1 + 1) % 2 == 0 ? true : false;
        bool evenY = (y2 - y1 + 1) % 2 == 0 ? true : false;

        int tileAnchorX;
        int tileAnchorY;

        if (evenX)
            tileAnchorX = ((x2 - x1 + 1) / 2) - 1;
        else
            tileAnchorX = ((x2 - x1 + 1) / 2);

        if (evenY)
            tileAnchorY = ((y2 - y1 + 1) / 2) - 1;
        else
            tileAnchorY = ((y2 - y1 + 1) / 2);

        // get the start index (0,0) position removing the offset of the tile anchor
        int startIndex = selected.tileID - (tileAnchorY * tilesPerWidth) - tileAnchorX;

        int buildingID = state.buildingID_Incrementer;
        for (int x = 0; x < (x2 - x1 + 1); x++)
        {
            for (int y = 0; y < (y2 - y1 + 1); y++)
            {
                int currentIndex = startIndex + (y * tilesPerWidth) + x;
                if (currentIndex > 0)
                {
                    Tile tile = tileMap[currentIndex];
                    tile.isValid = 0;
                    tile.hasRoad = true;
                    tile.hasBuilding = true;
                    tile.buildingID = buildingID;
                    tileMap[currentIndex] = tile;
                }
            }
        }
    }


    private static float3 CalculateBuildingOffset(SelectedTile selected, float2x2 template, int tileWidth)
    {
        int x1 = (int)template.c0.x;
        int y1 = (int)template.c0.y;
        int x2 = (int)template.c1.x;
        int y2 = (int)template.c1.y;

        float3 offset;

        offset.x = (x2 - x1 + 1) % 2 == 0 ? (float)tileWidth / 2.0f : 0;
        offset.z = (y2 - y1 + 1) % 2 == 0 ? (float)tileWidth / 2.0f : 0;
        offset.y = 0;

        return offset;
    }


    protected override void OnUpdate()
    {
        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        var tileMap = m_tilesQuery.ToComponentDataArray<Tile>(Allocator.TempJob);
        var input = m_inputGroup.ToComponentDataArray<PlayerInput>(Allocator.TempJob);
        var selected = m_selectedTileQuery.ToComponentDataArray<SelectedTile>(Allocator.TempJob);
        var gameState = m_gameStateGroup.ToComponentDataArray<GameState>(Allocator.TempJob);
        var spawner = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
        var buildings = q_buildingQuery.ToComponentDataArray<Building>(Allocator.TempJob);
        var buildingEntities = q_buildingQuery.ToEntityArray(Allocator.TempJob);
        var buildingLocations = q_buildingQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);

        int tilesPerWidth = TerrainSystem.tilesPerWidth;
        int tileWidth = TerrainSystem.tileWidth;

        bool[] bArr = new bool[1];
        NativeArray<bool> newBuildingBuilt = new NativeArray<bool>(bArr, Allocator.TempJob);


        var moveBuildingJob = Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref PlacingBuilding p, ref Translation t, ref Rotation r, in Building b) =>
            {
                if (input[0].rotateLeft)
                {
                    Quaternion prev = r.Value;
                    Quaternion r1 = prev * Quaternion.Euler(0, 90, 0);
                    r.Value = r1;
                }
                else if (input[0].rotateRight)
                {
                    Quaternion prev = r.Value;
                    Quaternion r1 = prev * Quaternion.Euler(0, -90, 0);
                    r.Value = r1;
                }

                float3 offset = CalculateBuildingOffset(selected[0], b.templateCoords, tileWidth);

                t.Value = selected[0].tileCoord + offset;
            }).Schedule(Dependency);


        var finaliseBuildingJob = Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref PlacingBuilding p, ref Building building, in Translation pos, in Rotation r) =>
            {
                GameState state = gameState[0];

                if (CheckTemplateValid(selected[0], tileMap, building.templateCoords, tilesPerWidth))
                {
                    if ((input[0].MouseButtonUp0) && input[0].shift)
                    {
                        newBuildingBuilt[0] = true;

                        var constructionEntity = GetBuildingConstructionPrefab(building, spawner[0]);
                        var newEnt = commandBuffer.Instantiate(entityInQueryIndex, constructionEntity);
                        commandBuffer.SetComponent(entityInQueryIndex, newEnt, pos);

                        commandBuffer.AddComponent<ConstructingBuilding>(entityInQueryIndex, newEnt);
                        CountdownTimer c = new CountdownTimer { timerLength_secs = 10, timerValue = 10 };
                        commandBuffer.AddComponent(entityInQueryIndex, newEnt, c);

                        Building buildingToSet = building;
                        buildingToSet.position = pos.Value;
                        buildingToSet.buildingID = state.buildingID_Incrementer;
                        buildingToSet.rotation = r.Value;
                        commandBuffer.AddComponent(entityInQueryIndex, newEnt, buildingToSet);

                        // if the building has a weapon, add the component to the construction entity
                        if (HasComponent<Weapon>(entity))
                        {
                            commandBuffer.AddComponent<Weapon>(entityInQueryIndex, newEnt, GetComponent<Weapon>(entity));
                        }

                        SetTemplate(selected[0], tileMap, buildingToSet.templateCoords, tilesPerWidth, state);
                        state.buildingID_Incrementer++;
                        gameState[0] = state;
                    }
                    else if (input[0].MouseButtonUp0)
                    {
                        newBuildingBuilt[0] = true;

                        var constructionEntity = GetBuildingConstructionPrefab(building, spawner[0]);
                        var newEnt = commandBuffer.Instantiate(entityInQueryIndex, constructionEntity);
                        commandBuffer.SetComponent(entityInQueryIndex, newEnt, pos);

                        commandBuffer.AddComponent<ConstructingBuilding>(entityInQueryIndex, newEnt);
                        CountdownTimer c = new CountdownTimer { timerLength_secs = 10, timerValue = 10 };
                        commandBuffer.AddComponent(entityInQueryIndex, newEnt, c);

                        Building buildingToSet = building;
                        buildingToSet.position = pos.Value;
                        buildingToSet.buildingID = state.buildingID_Incrementer;
                        buildingToSet.rotation = r.Value;
                        commandBuffer.AddComponent(entityInQueryIndex, newEnt, buildingToSet);

                        // if the building has a weapon, add the component to the construction entity
                        if (HasComponent<Weapon>(entity))
                        {
                            commandBuffer.AddComponent<Weapon>(entityInQueryIndex, newEnt, GetComponent<Weapon>(entity));
                        }

                        SetTemplate(selected[0], tileMap, buildingToSet.templateCoords, tilesPerWidth, state);
                        state.buildingID_Incrementer++;
                        state.gameState = e_GameStates.state_Idle;
                        gameState[0] = state;

                        commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                    }
                }

                if (input[0].MouseButtonDown1)
                {
                    commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                    state.gameState = e_GameStates.state_Idle;
                    gameState[0] = state;
                }
            }).Schedule(moveBuildingJob);


        var completeBuildingJob = Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref ConstructingBuilding c, ref Building b, ref CountdownTimer timer, ref Translation pos) =>
            {
                if (timer.timerValue <= 0)
                {
                    // get the building prefab and instantiate it
                    var entPrefab = GetBuildingPrefab(b, spawner[0]);
                    var newEnt = commandBuffer.Instantiate(entityInQueryIndex, entPrefab);

                    // set the buildings position and rotation
                    commandBuffer.SetComponent(entityInQueryIndex, newEnt, new Translation { Value = b.position });
                    commandBuffer.SetComponent(entityInQueryIndex, newEnt, new Rotation { Value = b.rotation });

                    // Add the Building component to it
                    Building newBuilding = b;
                    commandBuffer.AddComponent(entityInQueryIndex, newEnt, b);

                    // Add the Friendly component to it
                    commandBuffer.AddComponent<FriendlyUnit>(entityInQueryIndex, newEnt);

                    // If it has a weapon, enable the weapon and add it to the building
                    if (HasComponent<Weapon>(entity))
                    {
                        Weapon w = GetComponent<Weapon>(entity);
                        w.enabled = true;
                        commandBuffer.AddComponent<Weapon>(entityInQueryIndex, newEnt, w);
                    }

                    // Add the target component to the new prefab and copy across its health
                    Target t;
                    t.health = b.startingHealth;
                    commandBuffer.AddComponent<Target>(entityInQueryIndex, newEnt, t);

                    // If the building has a gatherable resource add the ResourceGatherBuilding component to the building
                    if (b.gatherableType != e_ResourceTypes.NoResource)
                    {
                        ResourceGatherBuilding r;
                        r.gatherableType = b.gatherableType;
                        r.gatherAmount = b.gatherAmount;
                        r.tileRadius = b.tileRadius;
                        commandBuffer.AddComponent<ResourceGatherBuilding>(entityInQueryIndex, newEnt, r);
                        CountdownTimer countdown = new CountdownTimer { timerLength_secs = 5, timerValue = 0 };   // 5 second intervals
                        commandBuffer.AddComponent<CountdownTimer>(entityInQueryIndex, newEnt, countdown);
                    }


                    commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                }
            }).Schedule(finaliseBuildingJob);

        NativeList<Vector3> deathLocations = new NativeList<Vector3>(Allocator.TempJob);

        // Loop over all buildings with a target, if that target is 0, mark it for destruction.
        var MarkForDestroy = Entities
            .ForEach((Entity entity, int entityInQueryIndex, in Target t, in LocalToWorld l, in Building s) =>
            {
                if (t.health <= 0)
                {
                    Destroy_Building d;
                    d.triggerDestroy = true;
                    d.buildingID = s.buildingID;
                    commandBuffer.AddComponent<Destroy_Building>(entityInQueryIndex, entity, d);
                }
            }).Schedule(completeBuildingJob);

        // This job destroys buildings that have been marked (usually from the UI)
        var destroyBuildingJob = Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref Destroy_Building d) =>
            {
                if (d.triggerDestroy)
                {
                    for (int i = 0; i < buildings.Length; i++)
                    {
                        if (buildings[i].buildingID == d.buildingID)
                        {
                            deathLocations.Add(buildingLocations[i].Position);

                            commandBuffer.DestroyEntity(entityInQueryIndex, buildingEntities[i]);
                            commandBuffer.DestroyEntity(entityInQueryIndex, entity);

                            for (int j = 0; j < tileMap.Length; j++)
                            {
                                if (tileMap[j].buildingID == d.buildingID)
                                {
                                    Tile t = tileMap[j];
                                    t.buildingID = 0;
                                    t.hasBuilding = false;
                                    t.isValid = 1;
                                    tileMap[j] = t;
                                }
                            }
                            break;
                        }
                    }
                }
            }).Schedule(MarkForDestroy);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(destroyBuildingJob);
        destroyBuildingJob.Complete();

        foreach (var d in deathLocations)
        {
            GameObject.Instantiate(GO_Spawner.Instance.largeExplosion, d, Quaternion.identity);
        }

        deathLocations.Dispose();

        if (newBuildingBuilt[0])
        {
            //LocalNavMeshBuilder.Instance.updateMeshes();
        }

        m_tilesQuery.CopyFromComponentDataArray(tileMap);
        m_gameStateGroup.CopyFromComponentDataArray(gameState);   

        tileMap.Dispose();
        gameState.Dispose();
        input.Dispose();
        selected.Dispose();
        spawner.Dispose();
        newBuildingBuilt.Dispose();
        buildings.Dispose();
        buildingEntities.Dispose();
        buildingLocations.Dispose();
    }

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        m_tilesQuery = GetEntityQuery(typeof(Tile));
        m_inputGroup = GetEntityQuery(typeof(PlayerInput));
        m_selectedTileQuery = GetEntityQuery(typeof(SelectedTile));
        m_gameStateGroup = GetEntityQuery(typeof(GameState));
        m_BuildingTypesQuery = GetEntityQuery(typeof(BuildingTypeSpawner));
        m_tileMapGroup = GetEntityQuery(typeof(Tile));
        m_placingRoadQuery = GetEntityQuery(typeof(PreviewRoad));
        q_buildingQuery = GetEntityQuery(typeof(Building), typeof(LocalToWorld));
    }

    public static void Spawn_Terran_Habitat()
    {
        var s = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
        GenericSpawner(s[0].Terran_Habitat);
        s.Dispose();
    }

    public static void Spawn_Terran_House()
    {
        var s = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
        GenericSpawner(s[0].Terran_House);
        s.Dispose();
    }

    public static void Spawn_Terran_ResidentBlock()
    {
        var s = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
        var ent = GenericSpawner(s[0].Terran_ResidentBlock);
        s.Dispose();
    }

    public static void Spawn_Terran_EnergySphere()
    {
        var s = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
        var ent = GenericSpawner(s[0].Terran_EnergySphere);
        s.Dispose();
    }

    public static void Spawn_Terran_AquaStore()
    {
        var s = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
        GenericSpawner(s[0].Terran_AquaStore);
        s.Dispose();
    }

    public static void Spawn_Terran_PlasmaCannon()
    {
        var s = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
        GenericSpawner(s[0].Terran_PlasmaCannon);
        s.Dispose();
    }

    public static void Spawn_Destroy_Building()
    {
        var ent = MainLoader.entityManager.CreateEntity();
        MainLoader.entityManager.AddComponent<Destroy_Building>(ent);


    }

    private static Entity GenericSpawner(Entity prefab)
    {
        Debug.Log("Generic Spawner");
        var gameState = m_gameStateGroup.ToComponentDataArray<GameState>(Allocator.TempJob);
        var gameStateEntity = m_gameStateGroup.ToEntityArray(Allocator.TempJob);

        GameState newState = gameState[0];
        newState.gameState = e_GameStates.state_BuildingPlacement;
        MainLoader.entityManager.SetComponentData(gameStateEntity[0], newState);

        var entity = MainLoader.entityManager.Instantiate(prefab);
        var position = new float3(515, 50, 515);
        Translation pos = new Translation { Value = position };
        MainLoader.entityManager.SetComponentData(entity, pos);
        MainLoader.entityManager.AddComponent(entity, typeof(PlacingBuilding));
        MainLoader.entityManager.SetComponentData(entity, new Rotation { Value = Quaternion.Euler(0, 0, 0) });

        gameStateEntity.Dispose();
        gameState.Dispose();

        return entity;
    }

    private static void RoadSpawner(Entity prefab)
    {
        var gameState = m_gameStateGroup.ToComponentDataArray<GameState>(Allocator.TempJob);
        var gameStateEntity = m_gameStateGroup.ToEntityArray(Allocator.TempJob);

        GameState newState = gameState[0];
        newState.gameState = e_GameStates.state_RoadPlacement;
        MainLoader.entityManager.SetComponentData(gameStateEntity[0], newState);

        var entity = MainLoader.entityManager.Instantiate(prefab);
        var position = new float3(515, 50, 515);
        Translation pos = new Translation { Value = position };
        MainLoader.entityManager.SetComponentData(entity, pos);
        //MainLoader.entityManager.AddComponent(entity, typeof(PlacingRoad));
        MainLoader.entityManager.AddComponent(entity, typeof(RoadDisplay));
        MainLoader.entityManager.SetComponentData(entity, new RoadDisplay { placing = 1 });
        MainLoader.entityManager.AddComponent(entity, typeof(RoadCurrentlyBuilding));
        //MainLoader.entityManager.AddComponent(entity, typeof(Road));
        MainLoader.entityManager.SetComponentData(entity, new Rotation { Value = Quaternion.Euler(0, 0, 0) });

        gameStateEntity.Dispose();
        gameState.Dispose();
    }
}

public struct ConstructingBuilding : IComponentData
{
    public bool dontuse;
}


public struct PlacingBuilding : IComponentData
{
    public Building building;
}


public struct RoadCurrentlyBuilding : IComponentData
{
}

public struct PathRequestType_Road : IComponentData
{
}


public struct Destroy_Building : IComponentData
{
    public int buildingID;
    public bool triggerDestroy;
}

public struct ResourceGatherBuilding : IComponentData
{
    public int tileRadius;
    public e_ResourceTypes gatherableType;
    public int gatherAmount;
}