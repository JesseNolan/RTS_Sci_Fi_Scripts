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


public class SelectionSystem : SystemBase
{
    public EntityQuery m_selectedOnlyQuery;
    private Entity previouslySelected;
    private Entity selectedIndicator;
    public EntityQuery m_GeneralSpawnerQuery;
    public EntityQuery m_renderMesh;


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
                if (EntityManager.Exists(previouslySelected))
                    DisableSelectionReentrant(previouslySelected);
                EnableSelectionReentrant(selected[0]);
                previouslySelected = selected[0];
            }
        } else
        {
            if (EntityManager.Exists(previouslySelected))
                DisableSelectionReentrant(previouslySelected);
            previouslySelected = Entity.Null;
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
        m_renderMesh = GetEntityQuery(typeof(RenderMesh));
    }


    private void EnableFresnel(Entity entity)
    {
        if (HasComponent<SelectedFresnel>(entity))
        {
            SelectedFresnel sf;
            sf.Value = 1.0f;
            EntityManager.SetComponentData<SelectedFresnel>(entity, sf);
        }
    }


    private void DisableFresnel(Entity entity)
    {
        if (HasComponent<SelectedFresnel>(entity))
        {
            SelectedFresnel sf;
            sf.Value = 0.0f;
            EntityManager.SetComponentData<SelectedFresnel>(entity, sf);
        }
    }

    private void EnableSelectionReentrant(Entity entity)
    {
        BufferFromEntity<Child> cb = GetBufferFromEntity<Child>();
        if (cb.HasComponent(entity))
        {
            DynamicBuffer<Child> children = GetBuffer<Child>(entity);
            foreach (var c in children)
            {
                EnableFresnel(c.Value);
                EnableSelectionReentrant(c.Value);
            }
        }
    }

    private void DisableSelectionReentrant(Entity entity)
    {
        BufferFromEntity<Child> cb = GetBufferFromEntity<Child>();
        if (cb.HasComponent(entity))
        {
            DynamicBuffer<Child> children = GetBuffer<Child>(entity);
            foreach (var c in children)
            {
                DisableFresnel(c.Value);
                DisableSelectionReentrant(c.Value);
            }
        }
    }
}

public struct Selected : IComponentData
{
    public bool selectionRendered;
}


