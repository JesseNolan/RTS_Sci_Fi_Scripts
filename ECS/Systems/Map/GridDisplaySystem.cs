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
public class GridDisplaySystem : JobComponentSystem
{
    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

    EntityQuery m_selectedTile;
    EntityQuery m_tileMapGroup;
    EntityQuery m_highlightSpawner;
    EntityQuery m_gameStateGroup;

    public struct GridHighlightSpawn : IJobForEachWithEntity<Tile>
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        [ReadOnly] public GameState gameState;
        [ReadOnly] public float3 selectedCoord;
        [ReadOnly] public float gridHighlightDisplayDistance;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<GridHighlightSpawner> tilePreviewSpawner;

        public void Execute(Entity entity, int Index, ref Tile t)
        {
            if ((gameState.gameState == e_GameStates.state_BuildingPlacement) || (gameState.gameState == e_GameStates.state_RoadPlacement))
            {
                if ((math.distance(t.tileCoord, selectedCoord) < gridHighlightDisplayDistance) && (t.isValid == 1))
                {
                    if (t.highlighted == 0)
                    {
                        Entity instance = CommandBuffer.Instantiate(Index, tilePreviewSpawner[0].Prefab);
                        GridTileHighlight g = new GridTileHighlight { tileIndex = t.tileID };
                        CommandBuffer.SetComponent(Index, instance, g);
                        Translation tr = new Translation { Value = new float3(t.tileCoord) };
                        CommandBuffer.SetComponent(Index, instance, tr);
                        t.highlighted = 1;
                    }
                }
                else
                {
                    t.highlighted = 0;
                }
            } else
            {
                t.highlighted = 0;
            }
        }
    }

    public struct GridHighlightDestroy : IJobForEachWithEntity<GridTileHighlight, Translation>
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        [ReadOnly] public GameState gameState;
        [ReadOnly] public float3 selectedCoord;
        [ReadOnly] public float gridHighlightDisplayDistance;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Tile> tileMap;

        public void Execute(Entity entity, int Index, ref GridTileHighlight g, ref Translation t)
        {
            if ((gameState.gameState == e_GameStates.state_BuildingPlacement) || (gameState.gameState == e_GameStates.state_RoadPlacement))
            {
                if ((math.distance(t.Value, selectedCoord) >= gridHighlightDisplayDistance) || (tileMap[g.tileIndex].isValid == 0))
                {
                    CommandBuffer.DestroyEntity(Index, entity);
                }
            } else
            {
                CommandBuffer.DestroyEntity(Index, entity);
            }
           
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var selectedTile = m_selectedTile.ToComponentDataArray<SelectedTile>(Allocator.TempJob);
        var gameState = m_gameStateGroup.ToComponentDataArray<GameState>(Allocator.TempJob);
        var spawner = m_highlightSpawner.ToComponentDataArray<GridHighlightSpawner>(Allocator.TempJob);
        var tileMap = m_tileMapGroup.ToComponentDataArray<Tile>(Allocator.TempJob);
        float3 coord = selectedTile[0].tileCoord;

        var gridSpawnJob = new GridHighlightSpawn
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter(),
            gameState = gameState[0],
            selectedCoord = coord,
            gridHighlightDisplayDistance = Settings.Instance.gridHighlightDisplayDistance,
            tilePreviewSpawner = spawner,

        }.Schedule(this, inputDeps);

        var gridDestroyJob = new GridHighlightDestroy
        {
            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter(),
            gameState = gameState[0],
            selectedCoord = coord,
            gridHighlightDisplayDistance = Settings.Instance.gridHighlightDisplayDistance,
            tileMap = tileMap,
        }.Schedule(this, gridSpawnJob);

        m_EntityCommandBufferSystem.AddJobHandleForProducer(gridSpawnJob);
        m_EntityCommandBufferSystem.AddJobHandleForProducer(gridDestroyJob);

        selectedTile.Dispose();
        gameState.Dispose();

        return gridDestroyJob;
    }


    protected override void OnCreate()
    {
        m_selectedTile = GetEntityQuery(typeof(SelectedTile));
        m_tileMapGroup = GetEntityQuery(typeof(Tile));
        m_highlightSpawner = GetEntityQuery(typeof(GridHighlightSpawner));
        m_EntityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
     
        m_gameStateGroup = GetEntityQuery(typeof(GameState));
    }
}
