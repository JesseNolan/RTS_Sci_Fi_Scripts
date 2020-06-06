//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Entities;
//using Unity.Transforms;
//using Unity.Rendering;
//using Unity.Mathematics;
//using Unity.Jobs;
//using Unity.Collections;
//using Unity.Burst;

//public class RoadPlacementSystem : JobComponentSystem
//{
//    EndSimulationEntityCommandBufferSystem m_EntityCommandBufferSystem;

//    private static EntityQuery m_buildingTypeQuery;
//    EntityQuery m_tileQuery;

//    private static Entity[] roadList;

//    private static Entity r_straight_r1;
//    private static Entity r_straight_r2;
//    private static Entity r_T_r1;
//    private static Entity r_T_r2;
//    private static Entity r_T_r3;
//    private static Entity r_T_r4;
//    private static Entity r_corner_r1;
//    private static Entity r_corner_r2;
//    private static Entity r_corner_r3;
//    private static Entity r_corner_r4;
//    private static Entity r_Int;


//    public struct RoadPlacementJob : IJobForEachWithEntity<RoadDisplay, Translation, Rotation>
//    {
//        public EntityCommandBuffer.Concurrent CommandBuffer;
//        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Tile> tiles;
//        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Entity> roads;
//        public int tilesPerWidth;

//        public void Execute(Entity entity, int index, ref RoadDisplay road, ref Translation l, ref Rotation r)
//        {
//            int roadNumber;
//            int ind = road.mapIndex;

//            roadNumber = GetLeft(ind) * 8 + GetDown(ind) * 4 + GetRight(ind) * 2 + GetUp(ind);

//            if (roadNumber != road.roadNumber)
//            {
//                if (road.placing == 1)
//                {
//                    CommandBuffer.DestroyEntity(index, entity);
//                    var newEntity = CommandBuffer.Instantiate(index, roads[roadNumber]);
//                    RoadDisplay newRoad = road;
//                    newRoad.roadNumber = roadNumber;
//                    CommandBuffer.AddComponent(index, newEntity, newRoad);
//                    CommandBuffer.SetComponent(index, newEntity, l);
//                    //CommandBuffer.AddComponent(index, newEntity, new PlacingRoad());
//                    CommandBuffer.AddComponent(index, newEntity, new PreviewRoad { tileIndex = ind });
//                    CommandBuffer.AddComponent(index, newEntity, new RoadCurrentlyBuilding());

//                    //Debug.LogFormat("New road display index: {0}    new entity: {1}  version {2}", newRoad.mapIndex, newEntity.Index, newEntity.Version);

//                }
//                if (road.placing == 0)
//                {
//                    CommandBuffer.DestroyEntity(index, entity);
//                    var newEntity = CommandBuffer.Instantiate(index, roads[roadNumber]);
//                    RoadDisplay newRoad = road;
//                    newRoad.roadNumber = roadNumber;
//                    CommandBuffer.AddComponent(index, newEntity, newRoad);
//                    CommandBuffer.SetComponent(index, newEntity, l);
//                }
//            }
//        }

//        private int GetLeft(int i)
//        {
//            if ((i - 1) < 0 || (i - 1) > tiles.Length)
//                return 0;

//            if (tiles[i - 1].displayRoad)
//                return 1;
//            else
//                return 0;
//        }

//        private int GetRight(int i)
//        {
//            if ((i + 1) < 0 || (i + 1) > tiles.Length)
//                return 0;
//            if (tiles[i + 1].displayRoad)
//                return 1;
//            else
//                return 0;
//        }

//        private int GetUp(int i)
//        {
//            if ((i + tilesPerWidth) < 0 || (i + tilesPerWidth) > tiles.Length)
//                return 0;
//            if (tiles[i + tilesPerWidth].displayRoad)
//                return 1;
//            else
//                return 0;
//        }

//        private int GetDown(int i)
//        {
//            if ((i - tilesPerWidth) < 0 || (i - tilesPerWidth) > tiles.Length)
//                return 0;
//            if (tiles[i - tilesPerWidth].displayRoad)
//                return 1;
//            else
//                return 0;
//        }
//    }


//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        var tiles = m_tileQuery.ToComponentDataArray<Tile>(Allocator.TempJob);

//        NativeArray<Entity> roads = new NativeArray<Entity>(roadList, Allocator.TempJob);

//        var roadJob = new RoadPlacementJob
//        {
//            tiles = tiles,
//            CommandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
//            tilesPerWidth = TerrainSystem.decimatedWidth,
//            roads = roads,

//        }.Schedule(this, inputDeps);

//        m_EntityCommandBufferSystem.AddJobHandleForProducer(roadJob);

//        return roadJob;
//    }

//    public static void InitialiseRoads()
//    {
//        var s = m_buildingTypeQuery.ToComponentDataArray<BuildingTypeSpawner>(Allocator.TempJob);

       
//        r_straight_r1 = MakeNewPrefab(s[0].Road_Straight);
//        r_straight_r2 = MakeNewPrefab(s[0].Road_Straight);
//        MainLoader.entityManager.SetComponentData(r_straight_r2, new Rotation { Value = MainLoader.entityManager.GetComponentData<Rotation>(r_straight_r2).Value * Quaternion.Euler(0, 90, 0) });
//        r_T_r1 = MakeNewPrefab(s[0].Road_T);
//        r_T_r2 = MakeNewPrefab(s[0].Road_T);
//        MainLoader.entityManager.SetComponentData(r_T_r2, new Rotation { Value = MainLoader.entityManager.GetComponentData<Rotation>(r_T_r2).Value * Quaternion.Euler(0, 270, 0) });
//        r_T_r3 = MakeNewPrefab(s[0].Road_T);
//        MainLoader.entityManager.SetComponentData(r_T_r3, new Rotation { Value = MainLoader.entityManager.GetComponentData<Rotation>(r_T_r3).Value * Quaternion.Euler(0, 180, 0) });
//        r_T_r4 = MakeNewPrefab(s[0].Road_T);
//        MainLoader.entityManager.SetComponentData(r_T_r4, new Rotation { Value = MainLoader.entityManager.GetComponentData<Rotation>(r_T_r4).Value * Quaternion.Euler(0, 90, 0) });
//        r_corner_r1 = MakeNewPrefab(s[0].Road_Corner);
//        r_corner_r2 = MakeNewPrefab(s[0].Road_Corner);
//        MainLoader.entityManager.SetComponentData(r_corner_r2, new Rotation { Value = MainLoader.entityManager.GetComponentData<Rotation>(r_corner_r2).Value * Quaternion.Euler(0, 270, 0) });
//        r_corner_r3 = MakeNewPrefab(s[0].Road_Corner);
//        MainLoader.entityManager.SetComponentData(r_corner_r3, new Rotation { Value = MainLoader.entityManager.GetComponentData<Rotation>(r_corner_r3).Value * Quaternion.Euler(0, 180, 0) });
//        r_corner_r4 = MakeNewPrefab(s[0].Road_Corner);
//        MainLoader.entityManager.SetComponentData(r_corner_r4, new Rotation { Value = MainLoader.entityManager.GetComponentData<Rotation>(r_corner_r4).Value * Quaternion.Euler(0, 90, 0) });
//        r_Int = MakeNewPrefab(s[0].Road_Intersection);

//        roadList = new Entity[16];

//        roadList[0] = r_straight_r1;
//        roadList[1] = r_straight_r2;
//        roadList[2] = r_straight_r1;
//        roadList[3] = r_corner_r3;
//        roadList[4] = r_straight_r2;
//        roadList[5] = r_straight_r2;
//        roadList[6] = r_corner_r2;
//        roadList[7] = r_T_r2;
//        roadList[8] = r_straight_r1;
//        roadList[9] = r_corner_r4;
//        roadList[10] = r_straight_r1;
//        roadList[11] = r_T_r3;
//        roadList[12] = r_corner_r1;
//        roadList[13] = r_T_r4;
//        roadList[14] = r_T_r1;
//        roadList[15] = r_Int;

//        s.Dispose();
//    }


//    private static Entity MakeNewPrefab(Entity oldPrefab)
//    {
//        var ent = MainLoader.entityManager.Instantiate(oldPrefab);
//        MainLoader.entityManager.AddComponentData(ent, new Prefab());
//        return ent;
//    }

//    protected override void OnCreate()
//    {
//        m_buildingTypeQuery = GetEntityQuery(typeof(BuildingTypeSpawner));
//        m_EntityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
//        m_tileQuery = GetEntityQuery(typeof(Tile));
//    }
//}


//public struct Road : IComponentData
//{
//    public int mapIndex;
//    public int roadNumber;
//}

//public struct RoadDisplay : IComponentData
//{
//    public int mapIndex;
//    public int roadNumber;
//    public int placing;
//}