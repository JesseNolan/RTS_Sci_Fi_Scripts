using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Collections;
using Unity.Jobs;

[UpdateAfter(typeof(TileSelectionSystem))]
[UpdateAfter(typeof(MapUpdaterSystem))]
public class GridDisplaySystem : SystemBase
{
    BeginSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    EntityQuery m_selectedTile;
    EntityQuery m_tileMapGroup;
    EntityQuery m_highlightSpawner;
    EntityQuery m_gameStateGroup;
    EntityQuery m_placingBuilding;


    protected override void OnUpdate()
    {
        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        var selectedTile = m_selectedTile.ToComponentDataArray<SelectedTile>(Allocator.TempJob);
        var gameState = m_gameStateGroup.ToComponentDataArray<GameState>(Allocator.TempJob);
        var spawner = m_highlightSpawner.ToComponentDataArray<GridHighlightSpawner>(Allocator.TempJob);
        var tileMap = m_tileMapGroup.ToComponentDataArray<Tile>(Allocator.TempJob);

        var placingBuilding = m_placingBuilding.ToComponentDataArray<Building>(Allocator.TempJob);

        float gridHighlightDisplayDistance = Settings.Instance.gridHighlightDisplayDistance;

        var highlightJob = Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref Tile t) =>
            {
                if ((gameState[0].gameState == e_GameStates.state_BuildingPlacement) || (gameState[0].gameState == e_GameStates.state_RoadPlacement))
                {
                    if ((math.distance(t.tileCoord, selectedTile[0].tileCoord) < gridHighlightDisplayDistance) && (t.isValid == 1))
                    {

                        for (int i = 0; i < placingBuilding.Length; i++)
                        {
                            
                            // check if current tile falls within this template

                        }


                        if (t.highlighted == 0)
                        {
                            Entity instance = commandBuffer.Instantiate(entityInQueryIndex, spawner[0].Prefab);
                            GridTileHighlight g = new GridTileHighlight { tileIndex = t.tileID };
                            commandBuffer.SetComponent(entityInQueryIndex, instance, g);
                            Translation tr = new Translation { Value = new float3(t.tileCoord) };
                            commandBuffer.SetComponent(entityInQueryIndex, instance, tr);
                            t.highlighted = 1;
                        }
                    }
                    else
                    {
                        t.highlighted = 0;
                    }
                }
                else
                {
                    t.highlighted = 0;
                }
            }).Schedule(Dependency);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(highlightJob);

        var destroyHighlightJob = Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref GridTileHighlight g, ref Translation t) =>
            {
                if ((gameState[0].gameState == e_GameStates.state_BuildingPlacement) || (gameState[0].gameState == e_GameStates.state_RoadPlacement))
                {
                    if ((math.distance(t.Value, selectedTile[0].tileCoord) >= gridHighlightDisplayDistance) || (tileMap[g.tileIndex].isValid == 0))
                    {
                        commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                    }
                }
                else
                {
                    commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                }
            }).Schedule(highlightJob);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(destroyHighlightJob);

        destroyHighlightJob.Complete();

        selectedTile.Dispose();
        gameState.Dispose();
        spawner.Dispose();
        tileMap.Dispose();
        placingBuilding.Dispose();

    }


    protected override void OnCreate()
    {
        m_selectedTile = GetEntityQuery(typeof(SelectedTile));
        m_tileMapGroup = GetEntityQuery(typeof(Tile));
        m_highlightSpawner = GetEntityQuery(typeof(GridHighlightSpawner));
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        m_gameStateGroup = GetEntityQuery(typeof(GameState));
        m_placingBuilding = GetEntityQuery(typeof(PlacingBuilding), typeof(Building));
    }
}
