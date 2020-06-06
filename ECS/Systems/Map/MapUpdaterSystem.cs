using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;


public class MapUpdaterSystem : JobComponentSystem
{
    EntityQuery m_tiles;
    private int previousOrder;

    [BurstCompile]
    public struct MapUpdaterJob : IJob
    {
        [ReadOnly] public int prevOrder;
        [ReadOnly] public int currentOrder;
        [DeallocateOnJobCompletion] public NativeArray<Tile> tileMap;
        
        public void Execute()
        {          
            if (prevOrder != currentOrder)
            {
                NativeArray<Tile> newTiles = new NativeArray<Tile>(tileMap.Length, Allocator.Temp);

                for (int i = 0; i < tileMap.Length; i++)
                {
                    var id = tileMap[i].tileID;
                    newTiles[id] = tileMap[i];
                }
                newTiles.CopyTo(tileMap);
                newTiles.Dispose();
            }          
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        int order = MainLoader.entityManager.GetComponentOrderVersion<Tile>();
        var tiles = m_tiles.ToComponentDataArray<Tile>(Allocator.TempJob);

        var job = new MapUpdaterJob
        {
            prevOrder = previousOrder,
            currentOrder = order,
            tileMap = tiles,
        }.Schedule(inputDeps);

        previousOrder = order;

        return job;

    }


    protected override void OnCreate()
    {
        m_tiles = GetEntityQuery(typeof(Tile));
    }
}
