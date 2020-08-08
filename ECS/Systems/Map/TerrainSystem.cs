using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

public class TerrainSystem : ComponentSystem
{
    public static int tileWidth;
    public static int terrainWidth;
    public static int terrainHeight;
    public static int tilesPerWidth;
    public static int res;
    public static Terrain terrain;

    public static void SetupTerrain()
    {
        terrain = GameObject.Find("Terrain").GetComponent<Terrain>();
        Debug.Log("getting terrain");

        tileWidth = MainLoader.settings.terrainDecimate;

        Vector3 size = terrain.terrainData.size;
        terrainWidth = (int)size.x;
        terrainHeight = (int)size.y;
        tilesPerWidth = terrainWidth / tileWidth;
        res = terrain.terrainData.heightmapResolution;

    }

    public static EntityQuery m_RockGroup;
    public static EntityQuery q_Resources;
    public static EntityQuery q_Tiles;
    public static EntityQuery q_gameState;

    protected override void OnCreate()
    {
        m_RockGroup = GetEntityQuery(typeof(RockTypeSpawner));
        q_Resources = GetEntityQuery(typeof(ResourceTypeSpawner));
        q_Tiles = GetEntityQuery(typeof(Tile));
        q_gameState = GetEntityQuery(typeof(GameState));
    }

    bool initialSpawn = false;

    protected override void OnUpdate()
    {
        if (!initialSpawn)
        {
            SetupTerrain();
            //generateRocks();
            LoadTileMap();
            GenerateResources();
            initialSpawn = true;
        }
    }


    public static void LoadTileMap()
    {
        Vector3 TSize = terrain.terrainData.size;
        int terrainWidth = (int)TSize.x;

        int terrainSize = (int)TSize.x / tileWidth;
        int tileArrayLength = terrainSize * terrainSize;

        for (int i = 0; i < tileArrayLength; i++)
        {
            Entity tileEntity = MainLoader.entityManager.CreateEntity(typeof(Tile));

            int xIndex = i % terrainSize;
            int yIndex = i / terrainSize;

            float worldPosX = xIndex * tileWidth + 0.5f * tileWidth;
            float worldPosY = yIndex * tileWidth + 0.5f * tileWidth;

            float height = terrain.terrainData.GetInterpolatedHeight(worldPosX / terrainWidth, worldPosY / terrainWidth);

            float height1 = terrain.terrainData.GetInterpolatedHeight((worldPosX - tileWidth / 2) / terrainWidth, (worldPosY - tileWidth / 2) / terrainWidth);
            float height2 = terrain.terrainData.GetInterpolatedHeight((worldPosX + tileWidth / 2) / terrainWidth, (worldPosY - tileWidth / 2) / terrainWidth);
            float height3 = terrain.terrainData.GetInterpolatedHeight((worldPosX - tileWidth / 2) / terrainWidth, (worldPosY + tileWidth / 2) / terrainWidth);
            float height4 = terrain.terrainData.GetInterpolatedHeight((worldPosX + tileWidth / 2) / terrainWidth, (worldPosY + tileWidth / 2) / terrainWidth);

           

            Tile newTile = new Tile();
            newTile.tileID = i;
            newTile.xIndex = xIndex;
            newTile.yIndex = yIndex;


            float diff1 = Mathf.Abs(height2 - height1);
            float diff2 = Mathf.Abs(height3 - height2);
            float diff3 = Mathf.Abs(height4 - height3);
            float diff4 = Mathf.Abs(height1 - height4);
            float diff5 = Mathf.Abs(height3 - height1);
            float diff6 = Mathf.Abs(height4 - height2);

            float[] diffList = { diff1, diff2, diff3, diff4, diff5 };

            float maxDiff = Mathf.Max(diffList);


            //if ((height1 == height2) && (height2 == height3) && (height3 == height4) && (height1 > 10))
            if (maxDiff < 1)
                newTile.isValid = 1;           
            else
                newTile.isValid = 0;           
            newTile.tileWidth = 1;
            newTile.tileCoord = new float3(worldPosX, height, worldPosY);
            newTile.tilePenalty = 5;
            newTile.highlighted = 1;

            MainLoader.entityManager.SetComponentData(tileEntity, newTile);
        }

        Debug.Log("Finished Generating Tile Map");
    }

    private static int GenerateResourceCluster(List<int> indx, int tilesPerWidth)
    {
        int ran = UnityEngine.Random.Range(0, indx.Count);
        int ranDir = UnityEngine.Random.Range(0, 3);
        switch (ranDir)
        {
            case 0:
                return GetLeftTile(indx[ran], tilesPerWidth);
            case 1:
                return GetRightTile(indx[ran], tilesPerWidth);
            case 2:
                return GetTopTile(indx[ran], tilesPerWidth);
            case 3:
                return GetBotTile(indx[ran], tilesPerWidth);
        }
        return GetLeftTile(indx[ran], tilesPerWidth);
    }

    private static int GetLeftTile(int indx, int tilesPerWidth)
    {
        return (indx - 1);
    }

    private static int GetRightTile(int indx, int tilesPerWidth)
    {
        return (indx + 1);
    }

    private static int GetTopTile(int indx, int tilesPerWidth)
    {
        return (indx - tilesPerWidth);
    }

    private static int GetBotTile(int indx, int tilesPerWidth)
    {
        return (indx + tilesPerWidth);
    }

    public static void GenerateResources()
    {
        var spawn = q_Resources.ToComponentDataArray<ResourceTypeSpawner>(Allocator.TempJob);
        var tiles = q_Tiles.ToComponentDataArray<Tile>(Allocator.TempJob);
        var state = q_gameState.ToComponentDataArray<GameState>(Allocator.TempJob);

        // iterate over tiles
        // decide if a tile should have the resouce based on abundance
        // cluster more of the resource together based on density
        // assign resource value based on the richness

        // Calculate Rock
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i].isValid == 1)
            {
                int ran = UnityEngine.Random.Range(0, (int)(1 / MainLoader.settings.Resource_Rock_Abundance));

                if (ran == 0)
                {
                    // start a cluster
                    List<int> indexes = new List<int>();
                    indexes.Add(i);
                    for (int k = 0; k < MainLoader.settings.Resource_Rock_Cluster_Nodes; k++)
                    {
                        var nextIndx = GenerateResourceCluster(indexes, TerrainSystem.tilesPerWidth);
                        if (!indexes.Contains(nextIndx))
                            if ((nextIndx > 0) && (nextIndx < tiles.Length))
                                indexes.Add(nextIndx);
                    }

                    for (int p = 0; p < indexes.Count; p++)
                    {
                        var currentIndex = indexes[p];
                        if (!tiles[currentIndex].hasResource)
                        {
                            var ent = MainLoader.entityManager.Instantiate(spawn[0].Resource_Rock);
                            Translation t = new Translation { Value = new float3(tiles[currentIndex].tileCoord) };
                            MainLoader.entityManager.SetComponentData<Translation>(ent, t);
                            MainLoader.entityManager.SetComponentData(ent, new Rotation
                            {
                                Value = Quaternion.Euler(
                                                new float3(
                                                0,
                                                UnityEngine.Random.Range(0, 180),
                                                0)
                                            )
                            });


                            Resource res = new Resource { resourceID = state[0].resourceID_Incrementer, resourceType = e_ResourceTypes.Rock, resourceAmount = 250 };
                            MainLoader.entityManager.AddComponentData(ent, res);

                            Tile tile = tiles[currentIndex];
                            tile.isValid = 0;
                            tile.hasResource = true;
                            tile.resourceID = state[0].resourceID_Incrementer;
                            tile.resourceType = e_ResourceTypes.Rock;
                            tiles[currentIndex] = tile;

                            GameState s = state[0];
                            s.resourceID_Incrementer++;
                            state[0] = s;
                            q_gameState.CopyFromComponentDataArray(state);
                        }                       
                    }      
                }
            }
        }

        // calculate Iron
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i].isValid == 1)
            {
                int ran = UnityEngine.Random.Range(0, (int)(1 / MainLoader.settings.Resource_Iron_Abundance));

                if (ran == 0)
                {
                    // start a cluster
                    List<int> indexes = new List<int>();
                    indexes.Add(i);
                    for (int k = 0; k < MainLoader.settings.Resource_Iron_Cluster_Nodes; k++)
                    {
                        var nextIndx = GenerateResourceCluster(indexes, TerrainSystem.tilesPerWidth);
                        if (!indexes.Contains(nextIndx))
                            indexes.Add(nextIndx);
                    }

                    for (int p = 0; p < indexes.Count; p++)
                    {
                        var currentIndex = indexes[p];
                        if (!tiles[currentIndex].hasResource)
                        {
                            var ent = MainLoader.entityManager.Instantiate(spawn[0].Resource_Iron);
                            Translation t = new Translation { Value = new float3(tiles[currentIndex].tileCoord) };
                            MainLoader.entityManager.SetComponentData<Translation>(ent, t);
                            //MainLoader.entityManager.SetComponentData(ent, new Rotation
                            //{
                            //    Value = Quaternion.Euler(
                            //                    new float3(
                            //                    0,
                            //                    UnityEngine.Random.Range(0, 180),
                            //                    0)
                            //                )
                            //});


                            Resource res = new Resource { resourceID = state[0].resourceID_Incrementer, resourceType = e_ResourceTypes.Iron, resourceAmount = 250 };
                            MainLoader.entityManager.AddComponentData(ent, res);

                            Tile tile = tiles[currentIndex];
                            tile.isValid = 0;
                            tile.hasResource = true;
                            tile.resourceID = state[0].resourceID_Incrementer;
                            tile.resourceType = e_ResourceTypes.Iron;
                            tiles[currentIndex] = tile;

                            GameState s = state[0];
                            s.resourceID_Incrementer++;
                            state[0] = s;
                            q_gameState.CopyFromComponentDataArray(state);
                        }
                    }
                }
            }
        }

        q_Tiles.CopyFromComponentDataArray(tiles); //after modifying tiles with our resource data, copy it back into the data array

        state.Dispose();
        tiles.Dispose();
        spawn.Dispose();
    }


    public static void generateRocks()
    {
        var spawn = m_RockGroup.ToComponentDataArray<RockTypeSpawner>(Allocator.TempJob);


        List<Entity> rocks = new List<Entity>();
        rocks.Add(spawn[0].Rock_v1);
        rocks.Add(spawn[0].Rock_v2);
        rocks.Add(spawn[0].Rock_v3);
        rocks.Add(spawn[0].Rock_v4);
        rocks.Add(spawn[0].Rock_v5);
        rocks.Add(spawn[0].Rock_v6);


        NoiseData nd1 = new NoiseData(46, 6, 0.5f, 1.7f, 54, new Vector2(0, 0), 1, 0);
        float[,] nm1 = Noise.GenerateNoiseMap(terrainWidth, terrainWidth, nd1);

        NoiseData nd2 = new NoiseData(76, 6, 0.5f, 1.7f, 54, new Vector2(0, 0), 1, 0);
        float[,] nm2 = Noise.GenerateNoiseMap(terrainWidth, terrainWidth, nd1);

        for (int i = 0; i < terrainWidth; i++)
        {
            for (int j = 0; j < terrainWidth; j++)
            {

                Vector3 terrainNormal = terrain.terrainData.GetInterpolatedNormal(i / (float)terrainWidth, j / (float)terrainWidth);
                float slope = calculateSlope(terrainNormal);

                if (slope < MainLoader.settings.rockSlopeThreshold)
                {

                    if (nm1[i, j] > MainLoader.settings.rockThreshold)
                    {

                        int ran = UnityEngine.Random.Range(0, MainLoader.settings.rockDensity);

                        if (ran == 0)
                        {
                            float terHeight = terrain.terrainData.GetInterpolatedHeight(i / (float)terrainWidth, j / (float)terrainWidth);
                            if ((terHeight < MainLoader.settings.rockTerrainHeightCutoff) && (terHeight > MainLoader.settings.rockTerrainMinHeight))
                            {
                                Vector3 placePos = new Vector3(i, terHeight - 1, j);
                                int ranNum = UnityEngine.Random.Range(0, rocks.Count);

                                var entity = MainLoader.entityManager.Instantiate(rocks[ranNum]);

                                MainLoader.entityManager.SetComponentData(entity, new Translation { Value = new float3(placePos) });

                                MainLoader.entityManager.SetComponentData(entity, new Rotation
                                {
                                    Value = Quaternion.Euler(
                                        new float3(
                                        UnityEngine.Random.Range(0, 180),
                                        UnityEngine.Random.Range(0, 180),
                                        UnityEngine.Random.Range(0, 180))
                                    )
                                });

                            }
                            

                            //MainLoader.entityManager.SetComponentData(entity, new NonUniformScale { Value = UnityEngine.Random.Range(0.3f, 3f)});            
                        }
                    }
                }
            }
        }

        spawn.Dispose();
    }

    public static float getTerrainHeightAtWorldCoord(Vector3 pos)
    {
        Vector3 size = terrain.terrainData.size;
        float terHeight = terrain.terrainData.GetInterpolatedHeight(pos.x / size.x, pos.z / size.z);
        return terHeight;
    }

    public static float calculateSlope(Vector3 normal)
    {
        return Vector3.Angle(normal, Vector3.up);
    }


}


public struct Tile : IComponentData
{
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

    public bool hasResource;
    public int resourceID;
    public e_ResourceTypes resourceType;

    //public bool isValid;
}