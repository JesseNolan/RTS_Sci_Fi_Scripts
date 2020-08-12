using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;
using Unity.Rendering;

[Serializable]
[MaterialProperty("Vector1_2A7675C0", MaterialPropertyFormat.Float)]
public struct SelectedFresnel : IComponentData
{
    public float Value;
}

public class MaterialProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Value;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new SelectedFresnel { Value = Value };
        dstManager.AddComponentData(entity, data);
    }
}