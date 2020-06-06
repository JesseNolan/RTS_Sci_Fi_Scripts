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

[UpdateBefore (typeof(BuildSystem))]
public class ClickDragSystem : ComponentSystem
{
    public EntityQuery m_selectedTileQuery;
    public EntityQuery m_inputGroup;
    public EntityQuery m_pathRequestQuery;
    public EntityQuery m_gameStateQuery;
    public EntityQuery q_tiles;
    public EntityQuery q_destroyBuildings;

    int currentIndex;
    int startIndex = -1;
    int prevIndex = -1;

    List<int> markedToDestroy = new List<int>();

    protected override void OnUpdate()
    {
        var selected = m_selectedTileQuery.ToComponentDataArray<SelectedTile>(Allocator.TempJob);
        var input = m_inputGroup.ToComponentDataArray<PlayerInput>(Allocator.TempJob);
        var gameState = m_gameStateQuery.ToComponentDataArray<GameState>(Allocator.TempJob);
        var gameStateEntity = m_gameStateQuery.ToEntityArray(Allocator.TempJob);
        var tiles = q_tiles.ToComponentDataArray<Tile>(Allocator.TempJob);


        if (gameState[0].gameState == e_GameStates.state_RoadPlacement)
        {
            currentIndex = selected[0].selectedIndex;

            if (input[0].MouseButtonHeld0)
            {
                if (startIndex < 0)
                {
                    startIndex = selected[0].selectedIndex;
                    prevIndex = startIndex;
                }
               
                if (currentIndex != prevIndex)
                {
                    if (gameState[0].gameState == e_GameStates.state_RoadPlacement)
                    {
                        PathRequest p = new PathRequest
                        {
                            startIndex = startIndex,
                            endIndex = currentIndex,
                            pathComplete = false,
                            roadBuilding = true,
                            roadPathing = false
                        };
                        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                        var entity = entityManager.CreateEntity();
                        entityManager.AddBuffer<PathCompleteBuffer>(entity);
                        entityManager.AddComponentData(entity, p);
                        entityManager.AddComponent(entity, typeof(PathRequestType_Road));
                    }

                }
                prevIndex = currentIndex;
            } else
            {
                startIndex = -1;

                if (currentIndex != prevIndex)
                {
                    PathRequest p = new PathRequest
                    {
                        startIndex = selected[0].selectedIndex,
                        endIndex = selected[0].selectedIndex,
                        pathComplete = false,
                    };
                    var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                    var entity = entityManager.CreateEntity();
                    entityManager.AddBuffer<PathCompleteBuffer>(entity);
                    entityManager.AddComponentData(entity, p);
                    entityManager.AddComponent(entity, typeof(PathRequestType_Road));
                }              
                prevIndex = currentIndex;
            }
        }
        else if (gameState[0].gameState == e_GameStates.state_DestroyBuildings)
        {
            currentIndex = selected[0].selectedIndex;

            if (input[0].MouseButtonHeld0 || input[0].MouseButtonDown0) // preview the building destroy
            {
                if (startIndex < 0)
                {
                    startIndex = selected[0].selectedIndex;
                    prevIndex = startIndex;
                }

                //if (currentIndex != prevIndex)
                //{
                    if (gameState[0].gameState == e_GameStates.state_DestroyBuildings)
                    {
                        // get tiles in a square
                        // for each tile, check if a building is there
                        if (GetTileIndexesFromClickDrag(startIndex, currentIndex, TerrainSystem.tilesPerWidth, out List<int> tileIndexes))
                        {
                           
                            foreach (var t in tileIndexes)
                            {
                                Tile currTile = tiles[t];

                                if (currTile.hasBuilding && !markedToDestroy.Contains(currTile.buildingID))
                                {
                                    markedToDestroy.Add(currTile.buildingID);
                                    var ent = MainLoader.entityManager.CreateEntity();
                                    Destroy_Building db = new Destroy_Building { buildingID = currTile.buildingID };
                                    MainLoader.entityManager.AddComponentData(ent, db);
                                }
                            }

                            var buildingsPreviouslyMarked = q_destroyBuildings.ToComponentDataArray<Destroy_Building>(Allocator.TempJob);
                            var buildingsPreviouslyMarkedEntity = q_destroyBuildings.ToEntityArray(Allocator.TempJob);

                            foreach (var b in buildingsPreviouslyMarked)
                            {
                                if (!markedToDestroy.Contains(b.buildingID))
                                {
                                    MainLoader.entityManager.DestroyEntity(buildingsPreviouslyMarkedEntity);
                                }
                            }

                            buildingsPreviouslyMarked.Dispose();
                            buildingsPreviouslyMarkedEntity.Dispose();
                            markedToDestroy.Clear();
                        }

                        
                    }
                //}
                prevIndex = currentIndex;
            }
            else if (input[0].MouseButtonUp0)   // action the building destroy
            {
                startIndex = -1;

                var buildingsMarked = q_destroyBuildings.ToComponentDataArray<Destroy_Building>(Allocator.TempJob);
                var buildingsMarkedEntity = q_destroyBuildings.ToEntityArray(Allocator.TempJob);

                for (int i = 0; i < buildingsMarked.Length; i++)
                {
                    Destroy_Building d = buildingsMarked[i];
                    d.triggerDestroy = true;
                    MainLoader.entityManager.SetComponentData(buildingsMarkedEntity[i], d);
                }
                prevIndex = currentIndex;

                GameState s = gameState[0];
                s.gameState = e_GameStates.state_Idle;
                MainLoader.entityManager.SetComponentData(gameStateEntity[0], s);

                buildingsMarked.Dispose();
                buildingsMarkedEntity.Dispose();


            } else if (input[0].MouseButtonDown1)   //cancel the building destroy
            {
                startIndex = -1;
                var buildingMarkedEntities = q_destroyBuildings.ToEntityArray(Allocator.TempJob);
                MainLoader.entityManager.DestroyEntity(buildingMarkedEntities);
                buildingMarkedEntities.Dispose();

                prevIndex = currentIndex;

                GameState s = gameState[0];
                s.gameState = e_GameStates.state_Idle;
                MainLoader.entityManager.SetComponentData(gameStateEntity[0], s);
            } else
            {
                startIndex = -1;
                prevIndex = currentIndex;
            }

        }

        selected.Dispose();
        input.Dispose();
        gameState.Dispose();
        gameStateEntity.Dispose();
        tiles.Dispose();

    }

    private bool GetTileIndexesFromClickDrag(int start, int end, int tilesPerWidth, out List<int> indexes)
    {
        indexes = new List<int>();

        int startX = start % tilesPerWidth;
        int startY = start / tilesPerWidth;

        int endX = end % tilesPerWidth;
        int endY = end / tilesPerWidth;

        if (startX > endX)
        {
            var t = startX;
            startX = endX;
            endX = t;
        }

        if (startY > endY)
        {
            var t = startY;
            startY = endY;
            endY = t;
        }


        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                int arrayIndex = y * tilesPerWidth + x;
                indexes.Add(arrayIndex);
            }
        }
        return ((indexes.Count > 0) ? true: false);
    }


    protected override void OnCreate()
    {
        m_selectedTileQuery = GetEntityQuery(typeof(SelectedTile));
        m_inputGroup = GetEntityQuery(typeof(PlayerInput));
        m_pathRequestQuery = GetEntityQuery(typeof(PathRequest));
        m_gameStateQuery = GetEntityQuery(typeof(GameState));
        q_tiles = GetEntityQuery(typeof(Tile));
        q_destroyBuildings = GetEntityQuery(typeof(Destroy_Building));
    }
}

