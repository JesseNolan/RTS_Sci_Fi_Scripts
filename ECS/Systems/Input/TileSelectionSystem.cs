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


public class TileSelectionSystem : JobComponentSystem
{
    public EntityQuery m_inputGroup;
    public EntityQuery m_tileQuery;

    [BurstCompile]
    public struct TileSelectJob : IJobForEach<SelectedTile>
    {
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Tile> tiles;
        [ReadOnly] public PlayerInput input;
        [ReadOnly] public int terrainDecimate;
        [ReadOnly] public int decimatedWidth;

        public void Execute(ref SelectedTile s)
        {
            int hitIndex = GetTileIndexFromWorldPos(input.terrainHitPos, terrainDecimate, decimatedWidth);

            Tile t = tiles[hitIndex];
            s.previousIndex = s.selectedIndex;
            s.selectedIndex = hitIndex;
            s.highlighted = t.highlighted;
            s.isValid = t.isValid;
            s.hasRoad = t.hasRoad;
            s.tileCoord = t.tileCoord;
            s.tileID = t.tileID;
            s.tilePenalty = t.tilePenalty;
            s.tileWidth = t.tileWidth;
            s.xIndex = t.xIndex;
            s.yIndex = t.yIndex;
            s.buildingID = t.buildingID;
            s.hasBuilding = t.hasBuilding;


        }

        private int GetTileIndexFromWorldPos(float3 pos, int tileWidth, int tilesPerWidth)
        {
            int xIndex = (int)pos.x / tileWidth;
            int yIndex = (int)pos.z / tileWidth;

            int arrayIndex = yIndex * tilesPerWidth + xIndex;

            return arrayIndex;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var input = m_inputGroup.ToComponentDataArray<PlayerInput>(Allocator.TempJob);
        var tiles = m_tileQuery.ToComponentDataArray<Tile>(Allocator.TempJob);

        var tileJob = new TileSelectJob
        {
            tiles = tiles,
            input = input[0],
            terrainDecimate = TerrainSystem.tileWidth,
            decimatedWidth = TerrainSystem.tilesPerWidth,
        }.Schedule(this, inputDeps);

        input.Dispose();

        return tileJob;
    }

    protected override void OnCreate()
    {
        m_inputGroup = GetEntityQuery(typeof(PlayerInput));
        m_tileQuery = GetEntityQuery(typeof(Tile));
    }

    public static void spawnSelectedTile()
    {
        MainLoader.entityManager.CreateEntity(typeof(SelectedTile));
    }
}


public struct SelectedTile : IComponentData
{
    public int selectedIndex;
    public int previousIndex;

    //public Entity entity;
    public int tileID;

    public int xIndex;
    public int yIndex;
    public int tileWidth;
    public float3 tileCoord;
    public int tilePenalty;

    // bool isnt blittable yet so cant use :(
    public int isValid;
    public int highlighted;
    public bool hasRoad;
    public bool displayRoad;
    public bool hasBuilding;
    public int buildingID;
}