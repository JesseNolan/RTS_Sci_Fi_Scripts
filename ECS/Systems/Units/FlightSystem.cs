using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

public class FlightSystem : JobComponentSystem
{
    private EntityQuery m_inputGroup;
    private EntityQuery m_tileMapGroup;
    private static EntityQuery m_translations;

    private static EntityQuery m_shipTypeSpawner;
    private static EntityQuery m_generalSpawner;

    private static EntityQuery m_buildings;

    [BurstCompile]
    public struct FlightJob : IJobForEachWithEntity<SpaceShip, Translation, Rotation>
    {
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<PlayerInput> playerInput;
        [ReadOnly] public Unity.Mathematics.Random random;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Tile> tiles;  // these should be ordered in a n x m grid (this is what MapUpdaterSystem does)
        [ReadOnly] public int terrainWidth;
        [ReadOnly] public int terrainDecimate;
        [ReadOnly] public int terrainDecimatedWidth;
        [ReadOnly] public float deltaTime;

        public void Execute(Entity entity, int index, ref SpaceShip s, ref Translation t, ref Rotation r)
        {
            if (playerInput[0].waypoint == 1)
            {
                Vector3 wayPointPos = playerInput[0].terrainHitPos + new float3(0, 30, 0);
                s.dest = wayPointPos;
            }

            Vector3 currentPos = t.Value;
            Vector3 currentDest = s.dest;
            Vector3 dx = Vector3.MoveTowards(currentPos, currentDest, s.speed * deltaTime * 50);
            Quaternion rot = r.Value;
            Vector3 dr = Vector3.RotateTowards(rot * Vector3.forward, currentDest - currentPos, 0.05f, 0);
            t.Value = dx;
            if (dr != Vector3.zero)
                r.Value = Quaternion.LookRotation(dr); 

            if (currentPos == currentDest)
            {
                float x, y, z;
                var newX = currentPos.x + random.NextFloat(-200, 200);
                var newY = currentPos.y + random.NextFloat(-200, 200);
                var newZ = currentPos.z + random.NextFloat(-200, 200);
                var newDest = new float3(newX, newY, newZ);
                x = Mathf.Clamp(newDest.x, 0, terrainWidth);
                z = Mathf.Clamp(newDest.z, 0, terrainWidth);              
                newDest.z = z;
                newDest.x = x;

                var tileIndex = GetTileIndexFromWorldPos(newDest, terrainDecimate, terrainDecimatedWidth);
                float height = tiles[tileIndex].tileCoord.y;
                
                y = Mathf.Clamp(newDest.y, height + 5f, height + 50f);
                newDest.y = y;

                s.dest = newDest;
            }

        }

        private int GetTileIndexFromWorldPos(float3 pos, int tileWidth, int tilesPerWidth)
        {
            int xIndex = (int)pos.x / tileWidth;
            int yIndex = (int)pos.z / tileWidth;

            Mathf.Clamp(xIndex, 0, tilesPerWidth-1);
            Mathf.Clamp(yIndex, 0, tilesPerWidth-1);

            int arrayIndex = yIndex * (tilesPerWidth-1) + xIndex;

            return arrayIndex;
        }

    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var playerInput = m_inputGroup.ToComponentDataArray<PlayerInput>(Allocator.TempJob);
        var random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, 100000));
        var tiles = m_tileMapGroup.ToComponentDataArray<Tile>(Allocator.TempJob);

        var job = new FlightJob
        {
            playerInput = playerInput,
            random = random,
            tiles = tiles,
            terrainWidth = TerrainSystem.terrainWidth,
            terrainDecimate = TerrainSystem.tileWidth,
            terrainDecimatedWidth = TerrainSystem.tilesPerWidth,
            deltaTime = Time.DeltaTime,
        }.Schedule(this, inputDeps);

        return job;
    }


    protected override void OnCreate()
    {
        m_inputGroup = GetEntityQuery(typeof(PlayerInput));
        m_tileMapGroup = GetEntityQuery(typeof(Tile));

        m_shipTypeSpawner = GetEntityQuery(typeof(ShipTypeSpawner));
        m_generalSpawner = GetEntityQuery(typeof(GeneralSpawner));

        m_buildings = GetEntityQuery(typeof(Building));

        m_translations = GetEntityQuery(typeof(Translation), typeof(Building));
    }

    public static void Spawn_Terran_MediumShip()
    {
        var spawner = m_shipTypeSpawner.ToComponentDataArray<ShipTypeSpawner>(Allocator.TempJob);
        GenericSpawner(spawner[0].mediumShip_count, spawner[0].mediumShip);
        spawner.Dispose();
    }

    public static void Spawn_Terran_SmallShip()
    {
        var spawner = m_shipTypeSpawner.ToComponentDataArray<ShipTypeSpawner>(Allocator.TempJob);
        GenericSpawner(spawner[0].smallShip_count, spawner[0].smallShip);
        spawner.Dispose();
    }

    public static void Spawn_Terran_LargeShip()
    {
        var spawner = m_shipTypeSpawner.ToComponentDataArray<ShipTypeSpawner>(Allocator.TempJob);      
        GenericSpawner(spawner[0].largeShip_count, spawner[0].largeShip);
        spawner.Dispose();
    }

    public static void Spawn_Terran_SmallShip_Enemy()
    {
        var spawner = m_shipTypeSpawner.ToComponentDataArray<ShipTypeSpawner>(Allocator.TempJob);
        GenericSpawner(spawner[0].enemy_smallShip_count, spawner[0].enemy_smallShip);
        spawner.Dispose();
    }

    public static void Spawn_Test_Unit()
    {
        var spawner = m_generalSpawner.ToComponentDataArray<GeneralSpawner>(Allocator.TempJob);
        var buildings = m_translations.ToEntityArray(Allocator.TempJob);
        var buildingComponents = m_translations.ToComponentDataArray<Building>(Allocator.TempJob);
        var translation = m_translations.ToComponentDataArray<Translation>(Allocator.TempJob);

        if (buildingComponents.Length >= 2)
        {
            for (int i = 0; i < 1; i++)
            {
                int ran1 = UnityEngine.Random.Range(0, buildingComponents.Length);
                int ran2 = UnityEngine.Random.Range(0, buildingComponents.Length);

                var entity = MainLoader.entityManager.Instantiate(spawner[0].testUnit);
                Translation pos = translation[0];
                MainLoader.entityManager.SetComponentData(entity, pos);
                MainLoader.entityManager.SetComponentData(entity, new Rotation { Value = Quaternion.Euler(0, 0, 0) });
                Debug.Log(translation[ran1].Value);
                Debug.Log(translation[ran2].Value);
                Citizen newCit = new Citizen { home = buildings[ran1],
                                                job = buildings[ran2],
                                                dst = buildings[ran2],
                                                home_Pos = translation[ran1].Value,
                                                job_Pos = translation[ran2].Value,
                                                timer = 0,
                                                idleTime = 200,
                                                };
                MainLoader.entityManager.AddComponent(entity, typeof(AI_Citizen_State));
                MainLoader.entityManager.AddComponent(entity, typeof(Citizen));
                MainLoader.entityManager.SetComponentData(entity, newCit);
            }
        }

        buildings.Dispose();
        spawner.Dispose();
        buildingComponents.Dispose();
        translation.Dispose();
    }



    private static void GenericSpawner(int count, Entity prefab)
    {
        for (int i = 0; i < count; i++)
        {
            var entity = MainLoader.entityManager.Instantiate(prefab);
            var position = new float3(TerrainSystem.terrainWidth / 2, 50, TerrainSystem.terrainWidth / 2);
            Translation pos = new Translation { Value = position };
            MainLoader.entityManager.SetComponentData(entity, pos);
            MainLoader.entityManager.SetComponentData(entity, new Rotation { Value = Quaternion.Euler(0, 0, 0) });
            MainLoader.entityManager.AddBuffer<ProjectileBuffer>(entity);
        }
    }
}
