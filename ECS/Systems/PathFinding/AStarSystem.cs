using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;

public class AStarSystem : JobComponentSystem
{
    public EntityQuery m_tilesQuery;
    public EntityQuery spawnerQuery;
    private EntityCommandBufferSystem m_EntityCommandBufferSystem;
   
    public struct AStarJob : IJobForEachWithEntity_EBC<PathCompleteBuffer, PathRequest>
    {
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Tile> tiles;
        [ReadOnly] public int maxIter;
        [ReadOnly] public int maxpathLength;
        [ReadOnly] public int tilesPerWidth;
        //[ReadOnly] public BufferFromEntity<PathCompleteBuffer> pathCompleteBuffer;
        public EntityCommandBuffer.ParallelWriter commandBuffer;

        public void Execute(Entity entity, int index, DynamicBuffer<PathCompleteBuffer> buff, ref PathRequest p)
        {
            if (!p.pathComplete)
            {
                if (p.roadBuilding)
                {
                    if ((tiles[p.startIndex].isValid == 0) || (tiles[p.endIndex].isValid == 0))
                    {
                        Debug.Log("AStar Job: start or end tile not valid");
                        commandBuffer.DestroyEntity(index, entity);
                        return;
                    }
                } else if (p.roadPathing)
                {
                    if (!tiles[p.startIndex].hasRoad || !tiles[p.endIndex].hasRoad)
                    {
                        Debug.LogFormat("AStar Job: start or end tile not valid,  start: {0}   end: {1}", p.startIndex, p.endIndex);
                    
                        commandBuffer.DestroyEntity(index, entity);
                        return;
                    }
                }

                if (p.startIndex == p.endIndex)
                {
                    buff.Add(p.startIndex);
                    p.pathComplete = true;
                    return;
                }
           
                Debug.Log("Processing AStar request");

                List<PathCost> openCosts = new List<PathCost>();
                List<PathCost> closedCosts = new List<PathCost>();

                int currentTileIndex = 0;

                PathCost currentTile = new PathCost
                {
                    tileIndex = p.startIndex,
                    fCost = 0,
                    gCost = 0,
                    hCost = 0
                };

                openCosts.Add(currentTile);

                int i = 0;
                while ((openCosts.Count > 0) && (i < maxIter))
                {
                    currentTileIndex = GetLowestCostTile(openCosts, p);
                    currentTile = openCosts[currentTileIndex];
                    openCosts.RemoveAtSwapBack(currentTileIndex);
                    CalculateTileCosts(openCosts, closedCosts, p, currentTile);
                    closedCosts.Add(currentTile);

                    if (currentTile.tileIndex == p.endIndex)
                    {
                        // found the path
                        break;
                    }
                    i++;
                }

                //var buff = commandBuffer.SetBuffer<PathCompleteBuffer>(index, entity);

                //buff.Clear();

                int checkIndex = p.endIndex;
                int parentIndex;

                while (checkIndex != p.startIndex)
                {
                    buff.Add(checkIndex);
                    parentIndex = GetParentIndex(closedCosts, checkIndex);
                    if (parentIndex < 0)
                    {
                        Debug.Log("AStar path broken");
                        p.pathComplete = true;
                        return;
                    }
                    checkIndex = parentIndex;
                }
                buff.Add(p.startIndex);
                
                //Debug.LogFormat("AStar Buff length: {0}   start: {1}   end: {2}", buff.Length, p.startIndex, p.endIndex);

                p.pathComplete = true;

                return;
            }
        }

        private int GetParentIndex(List<PathCost> open, int index)
        {
            for (int i = 0; i < open.Count; i++)
            {
                if (open[i].tileIndex == index)
                {
                    return open[i].parentTileIndex;
                }
            }
            return -1;
        }

        private void CalculateTileCosts(List<PathCost> open, List<PathCost> closed, PathRequest p, PathCost currentOpen)
        {
            int currentTileIndex = currentOpen.tileIndex;

            //int[] indexes = new int[8];
            //indexes[0] = currentTileIndex - tilesPerWidth - 1;
            //indexes[1] = currentTileIndex - tilesPerWidth;
            //indexes[2] = currentTileIndex - tilesPerWidth + 1;
            //indexes[3] = currentTileIndex - 1;
            //indexes[4] = currentTileIndex + 1;
            //indexes[5] = currentTileIndex + tilesPerWidth - 1;
            //indexes[6] = currentTileIndex + tilesPerWidth;
            //indexes[7] = currentTileIndex + tilesPerWidth + 1;

            int[] indexes = new int[4];
            indexes[0] = currentTileIndex - tilesPerWidth;
            indexes[1] = currentTileIndex - 1;
            indexes[2] = currentTileIndex + 1;
            indexes[3] = currentTileIndex + tilesPerWidth;

            for (int i = 0; i < indexes.Length; i++)
            {
                if ((indexes[i] >= 0) && (indexes[i] < (tilesPerWidth * tilesPerWidth)) && (((tiles[indexes[i]].isValid == 1) && p.roadBuilding) || (tiles[indexes[i]].hasRoad && p.roadPathing)))
                {
                    int dstDist = GetDistanceBetweenTilesWithoutDiagonal(p.endIndex, indexes[i]);

                    int newGCost = currentOpen.gCost + 1;

                    int newFCost = dstDist + newGCost;

                    // check if the world tile index exists in our open list
                    int openIndex = DoesIndexExist(open, indexes[i]);
                    int closedIndex = DoesIndexExist(closed, indexes[i]);


                    if (closedIndex >= 0)
                    {
                        continue;
                    }


                    if (openIndex < 0)
                    {
                        PathCost r = new PathCost
                        {
                            tileIndex = indexes[i],
                            gCost = newGCost,
                            parentTileIndex = currentOpen.tileIndex,
                            hCost = dstDist,
                            fCost = dstDist + newGCost
                        };
                        open.Add(r);
                    }
                    else if (newGCost >= open[openIndex].gCost)
                    {
                        continue;
                    }
                    else
                    {
                        PathCost r = open[openIndex];
                        r.gCost = newGCost;
                        r.parentTileIndex = currentOpen.tileIndex;
                        r.hCost = dstDist;
                        r.fCost = newFCost;
                        open[openIndex] = r;
                    }


                }
            }

        }

        private int DoesIndexExist(List<PathCost> open, int index)
        {
            for (int i = 0; i < open.Count; i++)
            {
                if (open[i].tileIndex == index)
                    return i;
            }
            return -1;
        }

        private int GetDistanceBetweenTilesWithoutDiagonal(int t1, int t2)
        {
            return (Mathf.Abs(tiles[t1].xIndex - tiles[t2].xIndex) + Mathf.Abs(tiles[t1].yIndex - tiles[t2].yIndex));
        }

        private int GetLowestCostTile(List<PathCost> open, PathRequest p)
        {
            int fCostLowest = 1000000;
            int lowestCostIndex = 0;

            for (int i = 0; i < open.Count; i++)
            {
                if (open[i].fCost < fCostLowest)
                {
                    lowestCostIndex = i;
                    fCostLowest = open[i].fCost;
                }
                else if (open[i].fCost == fCostLowest)
                {
                    Tile srcTile = tiles[p.startIndex];
                    Tile dstTile = tiles[p.endIndex];
                    Tile currentTile = tiles[open[i].tileIndex];
                    Tile lowestCostTile = tiles[open[lowestCostIndex].tileIndex];

                    if (Mathf.Abs(dstTile.xIndex - srcTile.xIndex) > Mathf.Abs(dstTile.yIndex - srcTile.yIndex))
                    {
                        if (Mathf.Abs(currentTile.xIndex - dstTile.xIndex) < Mathf.Abs(lowestCostTile.xIndex - dstTile.xIndex))
                        {
                            lowestCostIndex = i;
                            fCostLowest = open[i].fCost;
                        }
                    }
                    else
                    {
                        if (Mathf.Abs(currentTile.yIndex - dstTile.yIndex) < Mathf.Abs(lowestCostTile.yIndex - dstTile.yIndex))
                        {
                            lowestCostIndex = i;
                            fCostLowest = open[i].fCost;
                        }
                    }
                }
            }
            return lowestCostIndex;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var tiles = m_tilesQuery.ToComponentDataArray<Tile>(Allocator.TempJob);

        var aStarJob = new AStarJob
        {
            tiles = tiles,
            maxIter = 1000,
            tilesPerWidth = TerrainSystem.tilesPerWidth,
            //pathCompleteBuffer = GetBufferFromEntity<PathCompleteBuffer>(false),
            commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter(),

        }.Schedule(this, inputDeps);

        aStarJob.Complete();

        return aStarJob;
    }

    protected override void OnCreate()
    {
        m_tilesQuery = GetEntityQuery(typeof(Tile));
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
    }



    private struct PathCost
    {
        public int tileIndex;   // relative to world indexes
        public int parentTileIndex; // relative to this struct
        public int fCost;
        public int gCost;
        public int hCost;
    }

}


public struct PathRequest : IComponentData
{
    public int startIndex;
    public int endIndex;
    public bool pathComplete;
    public bool roadBuilding;
    public bool roadPathing;
}

[InternalBufferCapacity(256)]
public struct PathCompleteBuffer : IBufferElementData
{
    public static implicit operator int(PathCompleteBuffer e) { return e.Value; }
    public static implicit operator PathCompleteBuffer(int e) { return new PathCompleteBuffer { Value = e }; }

    public int Value;
}

