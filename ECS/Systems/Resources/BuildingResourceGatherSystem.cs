using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;


public class BuildingResourceGatherSystem : JobComponentSystem
{
    private EntityQuery q_tileMap;
    private EntityQuery q_Resources;
    private EntityQuery q_resourceGatherBuilding;
    private EntityQuery q_resourceStorage;

    protected override void OnCreate()
    {
        q_tileMap = GetEntityQuery(typeof(Tile));
        q_Resources = GetEntityQuery(typeof(Resource));
        q_resourceGatherBuilding = GetEntityQuery(typeof(ResourceGatherBuilding), typeof(CountdownTimer), typeof(Building));
        q_resourceStorage = GetEntityQuery(typeof(ResourceStorage));
    }


    public struct BuildingGatherJob : IJob
    {
        [ReadOnly] public NativeArray<Tile> tileMap;
        [ReadOnly] public int tilesPerWidth;
        [ReadOnly] public int tileWidth;
        public NativeArray<Resource> r;
        [ReadOnly] public NativeArray<ResourceGatherBuilding> b;
        public NativeArray<CountdownTimer> c;
        [ReadOnly] public NativeArray<Building> building;
        public NativeArray<ResourceStorage> resourceStorage;

        public void Execute()
        {
            List<Tile> tiles = new List<Tile>();
            List<int> uniqueResources = new List<int>();

            for (int i = 0; i < b.Length; i++)
            {
                if (c[i].timerValue < 0)
                {
                    CountdownTimer c1 = c[i];
                    c1.timerValue = c[i].timerLength_secs;
                    c[i] = c1;

                    // get tiles around building
                    GetTilesFromTemplateWithRadius(tileMap, building[i], b[i].tileRadius, tileWidth, tilesPerWidth, ref tiles);

                    // if tiles have a resource continue otherwise exit
                    for (int k = 0; k < tiles.Count; k++)
                    {
                        if (tiles[k].resourceType == b[i].gatherableType)
                        {
                            if (!uniqueResources.Contains(tiles[k].resourceID))
                                uniqueResources.Add(tiles[k].resourceID);
                        }          
                    }

                    if (uniqueResources.Count <= 0)
                        break;


                    for (int j = 0; j < r.Length; j++)
                    {
                        if (uniqueResources.Contains(r[j].resourceID))
                        {
                            Resource newR = r[j];
                            if (newR.resourceAmount <= 0)
                            {
                                continue;
                            } else if (newR.resourceAmount < b[i].gatherAmount)
                            {
                                int amountGathered = newR.resourceAmount;
                                ResourceStorage rs = resourceStorage[0];
                                switch (newR.resourceType)
                                {
                                    case e_ResourceTypes.NoResource:
                                        break;
                                    case e_ResourceTypes.Rock:
                                        rs.Rock += amountGathered;
                                        break;
                                    case e_ResourceTypes.Meat:
                                        break;
                                    case e_ResourceTypes.Vegetables:
                                        break;
                                    case e_ResourceTypes.Iron:
                                        rs.Iron += amountGathered;
                                        break;
                                    case e_ResourceTypes.Copper:
                                        break;
                                    case e_ResourceTypes.Gold:
                                        break;
                                    case e_ResourceTypes.Platinum:
                                        break;
                                    case e_ResourceTypes.Tin:
                                        break;
                                    default:
                                        break;
                                }         
                                resourceStorage[0] = rs;
                                newR.resourceAmount = 0;
                                r[j] = newR;
                            } else
                            {
                                ResourceStorage rs = resourceStorage[0];
                                switch (newR.resourceType)
                                {
                                    case e_ResourceTypes.NoResource:
                                        break;
                                    case e_ResourceTypes.Rock:
                                        rs.Rock += b[i].gatherAmount;
                                        break;
                                    case e_ResourceTypes.Meat:
                                        break;
                                    case e_ResourceTypes.Vegetables:
                                        break;
                                    case e_ResourceTypes.Iron:
                                        rs.Iron += b[i].gatherAmount;
                                        break;
                                    case e_ResourceTypes.Copper:
                                        break;
                                    case e_ResourceTypes.Gold:
                                        break;
                                    case e_ResourceTypes.Platinum:
                                        break;
                                    case e_ResourceTypes.Tin:
                                        break;
                                    default:
                                        break;
                                }
                                resourceStorage[0] = rs;
                                newR.resourceAmount -= b[i].gatherAmount;
                                r[j] = newR;
                            }              
                        }
                    }

                    tiles.Clear();
                    uniqueResources.Clear();
                }
            }
        }

        private bool GetTilesFromTemplateWithRadius(NativeArray<Tile> tileMap, Building building, int radius, int tileWidth, int tilesPerWidth, ref List<Tile> tiles)
        {
            List<int> x = new List<int>();
            List<int> y = new List<int>();

            // get building position
            var pos = building.position;

            // get the tile for that position
            var indx = GetTileIndexFromWorldPos(pos, tileWidth, tilesPerWidth);
            // iterate over tiles in a 5x5 around that position
            int anchor1 = indx + (2 * tilesPerWidth) - 2;
            // template is 5x5 = 25
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    int test = anchor1 - (i * tilesPerWidth) + j;
                    if (test < 0)
                        continue;
                    Tile testTile = tileMap[test];
                    if (testTile.buildingID == building.buildingID)
                    {
                        x.Add(test % tilesPerWidth);
                        y.Add(test / tilesPerWidth);
                    }
                }
            }


            //for (int i = 0; i < tileMap.Length; i++)
            //{
            //    if (tileMap[i].buildingID == building.buildingID)
            //    {
            //        int xIndx = i % tilesPerWidth;
            //        int yIndx = i / tilesPerWidth;
            //        x.Add(xIndx);
            //        y.Add(yIndx);
            //    }
            //}

            int[] xArr = x.ToArray();
            int[] yArr = y.ToArray();

            int x1 = Mathf.Min(xArr) - radius;
            int y1 = Mathf.Min(yArr) - radius;
            int x2 = Mathf.Max(xArr) + radius;
            int y2 = Mathf.Max(yArr) + radius;

            if (x1 < 0)
                x1 = 0;
            if (y1 < 0)
                y1 = 0;
            if (x2 > tilesPerWidth)
                x2 = tilesPerWidth;
            if (y2 > tilesPerWidth)
                y2 = tilesPerWidth;

            int width = x2 - x1;
            int height = y2 - y1;

            int anchor = y1 * tilesPerWidth + x1;

            for (int i = 0; i <= width; i++)
            {
                for (int j = 0; j <= height; j++)
                {
                    int test = anchor + (j * tilesPerWidth) + i;
                    Tile testTile = tileMap[test];
                    tiles.Add(testTile);
                }
            }

            return true;
        }

        private int GetTileIndexFromWorldPos(float3 pos, int tileWidth, int tilesPerWidth)
        {
            int xIndex = (int)(pos.x / tileWidth);
            int yIndex = (int)(pos.z / tileWidth);
    
            int arrayIndex = yIndex * tilesPerWidth + xIndex;

            return arrayIndex;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var tileMap = q_tileMap.ToComponentDataArray<Tile>(Allocator.TempJob);
        var Resources = q_Resources.ToComponentDataArray<Resource>(Allocator.TempJob);
        var resourceGatherBuilding = q_resourceGatherBuilding.ToComponentDataArray<ResourceGatherBuilding>(Allocator.TempJob);
        var countDown = q_resourceGatherBuilding.ToComponentDataArray<CountdownTimer>(Allocator.TempJob);
        var buildings = q_resourceGatherBuilding.ToComponentDataArray<Building>(Allocator.TempJob);
        var resourceStorage = q_resourceStorage.ToComponentDataArray<ResourceStorage>(Allocator.TempJob);

        var buildingGatherJob = new BuildingGatherJob
        {
            tileMap = tileMap,
            tilesPerWidth = TerrainSystem.tilesPerWidth,
            tileWidth = TerrainSystem.tileWidth,
            r = Resources,
            b = resourceGatherBuilding,
            c = countDown,
            building = buildings,
            resourceStorage = resourceStorage,
        }.Schedule();

        buildingGatherJob.Complete();


        q_Resources.CopyFromComponentDataArray(Resources);
        q_resourceGatherBuilding.CopyFromComponentDataArray(countDown);
        q_resourceStorage.CopyFromComponentDataArray(resourceStorage);


        tileMap.Dispose();
        Resources.Dispose();
        resourceGatherBuilding.Dispose();
        countDown.Dispose();
        buildings.Dispose();
        resourceStorage.Dispose();

        return buildingGatherJob;    
    }
}
