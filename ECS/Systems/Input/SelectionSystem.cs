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


public class SelectionSystem : ComponentSystem
{
    public EntityQuery m_selectedOnlyQuery;
    private Entity previouslySelected;
    private Entity selectedIndicator;
    public EntityQuery m_GeneralSpawnerQuery;


    protected override void OnUpdate()
    {
        var spawn = m_GeneralSpawnerQuery.ToComponentDataArray<GeneralSpawner>(Allocator.TempJob);

        if (!EntityManager.Exists(selectedIndicator))
        {
            selectedIndicator = EntityManager.Instantiate(spawn[0].selectionObject);
        }
      
        var selected = m_selectedOnlyQuery.ToEntityArray(Allocator.TempJob);
        var selectedTrans = m_selectedOnlyQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

        if (selected.Length > 0)
        {
            if (selected[0] != previouslySelected)
            {
                var newTrans = selectedTrans[0];
                var newPos = newTrans.Value;
                newPos.y += 12.0f;
                newTrans.Value = newPos;
                if (EntityManager.Exists(selectedIndicator))
                {
                    EntityManager.SetComponentData<Translation>(selectedIndicator, newTrans);
                }                 
                else
                {
                    selectedIndicator = EntityManager.Instantiate(spawn[0].selectionObject);
                    EntityManager.SetComponentData<Translation>(selectedIndicator, newTrans);
                }

                previouslySelected = selected[0];
            }
        } else
        {
            previouslySelected = Entity.Null;
            EntityManager.DestroyEntity(selectedIndicator);
        }

        selected.Dispose();
        selectedTrans.Dispose();
        spawn.Dispose();
    }


    protected override void OnCreate()
    {
        m_selectedOnlyQuery = GetEntityQuery(typeof(Selected), typeof(Translation));
        m_GeneralSpawnerQuery = GetEntityQuery(typeof(GeneralSpawner));
        //m_buildings = GetEntityQuery(typeof(Building));
    }
}

public struct Selected : IComponentData
{
    public bool selectionRendered;
}





//public class SelectionSystem : JobComponentSystem
//{
//    EntityQueryDesc m_selectedQueryDesc;
//    EntityQueryDesc m_selectedChildQueryDesc;
//    EntityQuery m_selectedQuery;
//    EntityQuery m_selectedChildQuery;

//    EntityQuery m_selectedOnlyQuery;

//    private Entity previouslySelected;
//    private Entity selectedIndicator;

//    private Entity selectedIndicatorPrefab;

//    public static Material outlineMat;

//    public static EntityQuery m_RockGroup;

//    bool firstime = true;

//    //public struct SelectionHighlightJob : IJobForEachWithEntity_EBC<Child, Selected>
//    //{
//    //    public EntityCommandBuffer.Concurrent CommandBuffer;
//    //    public SharedComponentDataProxy<RenderMesh> RenderData;


//    //    public void Execute(Entity entity, int index, DynamicBuffer<Child> c, ref Selected s)
//    //    {
//    //        if (!s.selectionRendered)
//    //        {
//    //            for (int i = 0; i < c.Length; i++)
//    //            {
//    //                var r = EntityManager.GetSharedComponentData<RenderMesh>(c[i].Value);

//    //                RenderMesh newR = r;
//    //                newR.material = outlineMat;
//    //                EntityManager.SetSharedComponentData<RenderMesh>(c[i].Value, newR);
//    //            }




//    //            s.selectionRendered = true;
//    //        }



//    //    }
//    //}




//    protected override JobHandle OnUpdate(JobHandle inputDeps)
//    {
//        //var selectedNoChild = m_selectedQuery.ToEntityArray(Allocator.TempJob);
//        //var selectedWithChild = m_selectedChildQuery.ToEntityArray(Allocator.TempJob);
//        //var noChildComponentData = m_selectedQuery.ToComponentDataArray<Selected>(Allocator.TempJob);
//        //var withChildComponentData = m_selectedChildQuery.ToComponentDataArray<Selected>(Allocator.TempJob);

//        //for (int i = 0; i < selectedNoChild.Length; i++)
//        //{
//        //    if (!noChildComponentData[i].selectionRendered)
//        //    {
//        //        var r = EntityManager.GetSharedComponentData<RenderMesh>(selectedNoChild[i]);
//        //        RenderMesh newR = r;
//        //        newR.material = outlineMat;
//        //        EntityManager.SetSharedComponentData<RenderMesh>(selectedNoChild[0], newR);
//        //        EntityManager.SetComponentData<Selected>(selectedNoChild[i], new Selected { selectionRendered = true });
//        //    }         
//        //}

//        //for (int j = 0; j < selectedWithChild.Length; j++)
//        //{
//        //    if (!withChildComponentData[j].selectionRendered)
//        //    {
//        //        var childBuffers = GetBufferFromEntity<Child>();
//        //        var buff = childBuffers[selectedWithChild[j]];

//        //        var entityToChange = new List<Entity>();

//        //        for (int k = 0; k < buff.Length; k++)
//        //        {
//        //            if (EntityManager.HasComponent<RenderMesh>(buff[k].Value))
//        //            {
//        //                entityToChange.Add(buff[k].Value);
//        //            }
//        //        }

//        //        foreach (var e in entityToChange)
//        //        {
//        //            var r = EntityManager.GetSharedComponentData<RenderMesh>(e);
//        //            RenderMesh newR = r;
//        //            newR.material = outlineMat;
//        //            EntityManager.SetSharedComponentData<RenderMesh>(e, newR);
//        //        }

//        //        EntityManager.SetComponentData<Selected>(selectedWithChild[j], new Selected { selectionRendered = true });

//        //    }
//        //}


//        //withChildComponentData.Dispose();
//        //noChildComponentData.Dispose();
//        //selectedNoChild.Dispose();
//        //selectedWithChild.Dispose();

//        return job;
//    }


//    protected override void OnCreate()
//    {
//        //m_selectedQueryDesc = new EntityQueryDesc
//        //{
//        //    All = new ComponentType[] { typeof(Selected) },
//        //    None = new ComponentType[] { typeof(Child) },
//        //};

//        //m_selectedQuery = GetEntityQuery(m_selectedQueryDesc);

//        //m_selectedChildQueryDesc = new EntityQueryDesc
//        //{
//        //    All = new ComponentType[] { typeof(Selected), typeof(Child) },
//        //};

//        //m_selectedChildQuery = GetEntityQuery(m_selectedChildQueryDesc);

//        //outlineMat = (Material)Resources.Load("_testMat", typeof(Material));


//        m_selectedOnlyQuery = GetEntityQuery(typeof(Selected));


//        m_RockGroup = GetEntityQuery(typeof(RockTypeSpawner));

//        var spawn = m_RockGroup.ToComponentDataArray<RockTypeSpawner>(Allocator.TempJob);
//        selectedIndicatorPrefab = spawn[0].Rock_v2;
//        EntityManager.Instantiate(selectedIndicatorPrefab);
//    }
//}



