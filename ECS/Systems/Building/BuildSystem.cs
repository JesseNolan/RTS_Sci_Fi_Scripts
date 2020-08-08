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


public class BuildSystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    public static EntityQuery m_gameStateGroup;
    public static EntityQuery m_BuildingTypesQuery;

    public EntityQuery m_tilesQuery;
    public EntityQuery m_inputGroup;
    public EntityQuery m_selectedTileQuery;
    public EntityQuery m_tileMapGroup;
    public EntityQuery m_placingRoadQuery;
    public EntityQuery q_buildingQuery;

    // This simply gets the building being currently placed and moves / rotates it around 
    [BurstCompile]
    public struct MoveBuilding : IJobForEachWithEntity<PlacingBuilding, Translation, Rotation>
    {
        [ReadOnly] public NativeArray<PlayerInput> input;
        [ReadOnly] public NativeArray<SelectedTile> selectedTile;

        public void Execute(Entity entity, int Index, ref PlacingBuilding p, ref Translation t, ref Rotation r)
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
            t.Value = selectedTile[0].tileCoord;
        }
    }

    public struct FinaliseBuilding : IJobForEachWithEntity<PlacingBuilding, Building, Translation>
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<PlayerInput> input;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<SelectedTile> selectedTile;
        [NativeDisableParallelForRestriction] public NativeArray<Tile> tileMap;
        public NativeArray<GameState> gameState;
        [ReadOnly] public BuildingTypeSpawner spawner;
        [ReadOnly] public int tilesPerWidth;
        public NativeArray<bool> newBuildingBuilt;

        public void Execute(Entity entity, int index, ref PlacingBuilding p, ref Building building, ref Translation pos)
        {
            GameState state = gameState[0];

            if (CheckTemplateValid(selectedTile[0], tileMap, building.buildingTemplate, tilesPerWidth))
            {
                if ((input[0].MouseButtonUp0) && input[0].shift)
                {
                    newBuildingBuilt[0] = true;

                    var constructionEntity = GetBuildingConstructionPrefab(building, spawner);
                    var newEnt = CommandBuffer.Instantiate(index, constructionEntity);
                    CommandBuffer.SetComponent(index, newEnt, pos);
                    
                    CommandBuffer.AddComponent<ConstructingBuilding>(index, newEnt);
                    CountdownTimer c = new CountdownTimer { timerLength_secs = 10 , timerValue = 10};
                    CommandBuffer.AddComponent(index, newEnt, c);

                    Building buildingToSet = building;
                    buildingToSet.position = selectedTile[0].tileCoord;
                    buildingToSet.buildingID = state.buildingID_Incrementer;
                    CommandBuffer.AddComponent(index, newEnt, buildingToSet);

                    SetTemplate(selectedTile[0], tileMap, buildingToSet.buildingTemplate, tilesPerWidth, state);                
                    state.buildingID_Incrementer++;
                    gameState[0] = state;                  
                }
                else if (input[0].MouseButtonUp0)
                {
                    newBuildingBuilt[0] = true;

                    var constructionEntity = GetBuildingConstructionPrefab(building, spawner);
                    var newEnt = CommandBuffer.Instantiate(index, constructionEntity);
                    CommandBuffer.SetComponent(index, newEnt, pos);

                    CommandBuffer.AddComponent<ConstructingBuilding>(index, newEnt);
                    CountdownTimer c = new CountdownTimer { timerLength_secs = 10, timerValue = 10 };
                    CommandBuffer.AddComponent(index, newEnt, c);

                    Building buildingToSet = building;
                    buildingToSet.position = selectedTile[0].tileCoord;
                    buildingToSet.buildingID = state.buildingID_Incrementer;
                    CommandBuffer.AddComponent(index, newEnt, buildingToSet);

                    SetTemplate(selectedTile[0], tileMap, buildingToSet.buildingTemplate, tilesPerWidth, state);
                    state.buildingID_Incrementer++;
                    state.gameState = e_GameStates.state_Idle;
                    gameState[0] = state;

                    CommandBuffer.DestroyEntity(index, entity);
                }
            }

            if (input[0].MouseButtonDown1)
            {
                CommandBuffer.DestroyEntity(index, entity);
                state.gameState = e_GameStates.state_Idle;
                gameState[0] = state;
            }
        }


        private Entity GetBuildingConstructionPrefab(Building b, BuildingTypeSpawner s)
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

        private bool CheckTemplateValid(SelectedTile selected, NativeArray<Tile> tileMap, System.UInt32 template, int tilesPerWidth)
        {
            int anchor = selected.tileID + (2 * tilesPerWidth) - 2;
            // template is 5x5 = 25
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    uint t = (template >> (i * 5 + j)) & 1U;
                    int test = anchor - (i * tilesPerWidth) + j;
                    Tile testTile = tileMap[test];
                    if (t > 0)
                        if (testTile.isValid == 0)
                            return false;
                }
            }       
            return true;
        }

        // For a building's template and a selected tile, this sets up the respective tiles the building has been built on
        private void SetTemplate(SelectedTile selected, NativeArray<Tile> tileMap, System.UInt32 template, int tilesPerWidth, GameState state)
        {
            int anchor = selected.tileID + (2 * tilesPerWidth) - 2;
            int buildingID = state.buildingID_Incrementer;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    uint t = (template >> (i * 5 + j)) & 1U;
                    int test = anchor - (i * tilesPerWidth) + j;
                    if (t > 0)
                    {
                        Tile tile = tileMap[test];
                        tile.isValid = 0;
                        tile.hasRoad = true;
                        tile.hasBuilding = true;
                        tile.buildingID = buildingID;
                        tileMap[test] = tile;                                               
                    }
                }
            }
        }
    }


    public struct CompleteBuildingConstruction : IJobForEachWithEntity<ConstructingBuilding, Building, CountdownTimer, Translation>
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        [ReadOnly] public BuildingTypeSpawner spawner;

        public void Execute(Entity entity, int index, ref ConstructingBuilding c, ref Building b, ref CountdownTimer timer, ref Translation pos)
        {
           
            if (timer.timerValue <= 0)
            {
                var entPrefab = GetBuildingPrefab(b, spawner);
                var newEnt = CommandBuffer.Instantiate(index, entPrefab);
                CommandBuffer.SetComponent(index, newEnt, pos);
                Building newBuilding = b;
                CommandBuffer.AddComponent(index, newEnt, b);

                CommandBuffer.DestroyEntity(index, entity);           
            }

        }

        private Entity GetBuildingPrefab(Building b, BuildingTypeSpawner s)
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
    }

    public struct DestroyBuildingJob : IJobForEachWithEntity<Destroy_Building>
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Building> buildings;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Entity> buildingEntities;
        [NativeDisableParallelForRestriction] public NativeArray<Tile> tileMap;

        public void Execute(Entity entity, int index, ref Destroy_Building d)
        {
            if (d.triggerDestroy)
            {
                for (int i = 0; i < buildings.Length; i++)
                {
                    if (buildings[i].buildingID == d.buildingID)
                    {
                        CommandBuffer.DestroyEntity(index, buildingEntities[i]);
                        CommandBuffer.DestroyEntity(index, entity);

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
            else
            {

            }
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        //var tiles = m_tilesQuery.ToEntityArray(Allocator.TempJob);
        //var tilesComponentData = GetComponentDataFromEntity<Tile>();
        var tileMap = m_tilesQuery.ToComponentDataArray<Tile>(Allocator.TempJob);
        var input = m_inputGroup.ToComponentDataArray<PlayerInput>(Allocator.TempJob);
        var selected = m_selectedTileQuery.ToComponentDataArray<SelectedTile>(Allocator.TempJob);
        var gameState = m_gameStateGroup.ToComponentDataArray<GameState>(Allocator.TempJob);
        var spawn = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);

        bool[] bArr = new bool[1];
        NativeArray<bool> newBuildingBuilt = new NativeArray<bool>(bArr, Allocator.TempJob);

        var moveJob = new MoveBuilding
        {
            input = input,
            selectedTile = selected,
        }.Schedule(this, inputDeps);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(moveJob);

        //var placeJob = new PlaceBuildingJob
        //{
        //    CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
        //    tiles = tiles,
        //    tilesComponentData = tilesComponentData,
        //    input = input,
        //    selectedTile = selected,
        //    stateEntity = state,
        //    gameStateComponentData = GetComponentDataFromEntity<GameState>(),
        //    deltaTime = Time.deltaTime,
        //}.Schedule(this, moveJob);


        var finaliseJob = new FinaliseBuilding
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter(),
            input = input,
            selectedTile = selected,
            tileMap = tileMap,
            gameState = gameState,
            tilesPerWidth = TerrainSystem.tilesPerWidth,
            newBuildingBuilt = newBuildingBuilt,
            spawner = spawn[0],
        }.Schedule(this, moveJob); 
        m_EntityCommandBufferSystem.AddJobHandleForProducer(finaliseJob);

        finaliseJob.Complete();

        var completeBuildingConstruction = new CompleteBuildingConstruction
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter(),
            spawner = spawn[0],
        }.Schedule(this, finaliseJob);

        completeBuildingConstruction.Complete();

        var destroyJob = new DestroyBuildingJob
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter(),
            buildings = q_buildingQuery.ToComponentDataArray<Building>(Allocator.TempJob),
            buildingEntities = q_buildingQuery.ToEntityArray(Allocator.TempJob),
            tileMap = tileMap
        }.Schedule(this, completeBuildingConstruction);

        destroyJob.Complete();

        if (newBuildingBuilt[0])
        {
            //LocalNavMeshBuilder.Instance.updateMeshes();
        }

        newBuildingBuilt.Dispose();

        m_tilesQuery.CopyFromComponentDataArray(tileMap);
        m_gameStateGroup.CopyFromComponentDataArray(gameState);

        tileMap.Dispose();
        gameState.Dispose();
        spawn.Dispose();



        return finaliseJob;
    }

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        m_tilesQuery = GetEntityQuery(typeof(Tile));
        m_inputGroup = GetEntityQuery(typeof(PlayerInput));
        m_selectedTileQuery = GetEntityQuery(typeof(SelectedTile));
        m_gameStateGroup = GetEntityQuery(typeof(GameState));
        m_BuildingTypesQuery = GetEntityQuery(typeof(BuildingTypeSpawner));
        m_tileMapGroup = GetEntityQuery(typeof(Tile));
        m_placingRoadQuery = GetEntityQuery(typeof(PreviewRoad));
        q_buildingQuery = GetEntityQuery(typeof(Building));
    }

    public static void Spawn_Terran_Habitat()
    {
        var s = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
        GenericSpawner(s[0].Terran_Habitat, s[0].Terran_Habitat_Template, e_BuildingTypes.Terran_Habitat);
        s.Dispose();
    }

    public static void Spawn_Terran_House()
    {
        var s = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
        GenericSpawner(s[0].Terran_House, s[0].Terran_House_Template, e_BuildingTypes.Terran_House);
        s.Dispose();
    }

    public static void Spawn_Terran_ResidentBlock()
    {
        var s = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
        var ent = GenericSpawner(s[0].Terran_ResidentBlock, s[0].Terran_ResidentBlock_Template, e_BuildingTypes.Terran_Resident_Block);
        ResourceGatherBuilding r = new ResourceGatherBuilding
        {
            tileRadius = 2,
            gatherableType = e_ResourceTypes.Rock,
            gatherAmount = 10,
        };
        MainLoader.entityManager.AddComponentData(ent, r);
        CountdownTimer c = new CountdownTimer { timerLength_secs = 5, timerValue = 0 };   // 5 second intervals
        MainLoader.entityManager.AddComponentData(ent, c);
        s.Dispose();
    }

    public static void Spawn_Terran_EnergySphere()
    {
        var s = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
        var ent = GenericSpawner(s[0].Terran_EnergySphere, s[0].Terran_EnergySphere_Template, e_BuildingTypes.Terran_Energy_Sphere);
        ResourceGatherBuilding r = new ResourceGatherBuilding
        {
            tileRadius = 3,
            gatherableType = e_ResourceTypes.Iron,
            gatherAmount = 10,
        };
        MainLoader.entityManager.AddComponentData(ent, r);
        CountdownTimer c = new CountdownTimer { timerLength_secs = 5, timerValue = 0 };   // 5 second intervals
        MainLoader.entityManager.AddComponentData(ent, c);
        s.Dispose();
    }

    public static void Spawn_Terran_AquaStore()
    {
        var s = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
        GenericSpawner(s[0].Terran_AquaStore, s[0].Terran_AquaStore_Template, e_BuildingTypes.Terran_Aqua_Store);
        s.Dispose();
    }

    public static void Spawn_Terran_PlasmaCannon()
    {
        var s = m_BuildingTypesQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);
        GenericSpawner(s[0].Terran_PlasmaCannon, s[0].Terran_PlasmaCannon_Template, e_BuildingTypes.Terran_Plasma_Cannon);
        s.Dispose();
    }

    public static void Spawn_Destroy_Building()
    {
        var ent = MainLoader.entityManager.CreateEntity();
        MainLoader.entityManager.AddComponent<Destroy_Building>(ent);


    }

    private static Entity GenericSpawner(Entity prefab, System.UInt32 template, e_BuildingTypes type)
    {
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
        Building b = new Building { buildingType = type, buildingTemplate = template };
        MainLoader.entityManager.AddComponentData(entity, b);

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
    public int poo;
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