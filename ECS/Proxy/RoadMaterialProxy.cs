using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;
using Unity.Rendering;

[Serializable]
[MaterialProperty("Vector4_B65872C9", MaterialPropertyFormat.Float4)]
public struct RoadMaterial_RoadTypeEnable : IComponentData
{
    public float4 Value;
}

[Serializable]
[MaterialProperty("Vector1_BFF99FF3", MaterialPropertyFormat.Float)]
public struct RoadMaterial_Rotation : IComponentData
{
    public float Value;
}


public class RoadMaterialProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    public float4 RoadTypeEnable;
    public float Rotation;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new RoadMaterial_RoadTypeEnable { Value = RoadTypeEnable };
        dstManager.AddComponentData(entity, data);
        var data2 = new RoadMaterial_Rotation { Value = Rotation };
        dstManager.AddComponentData(entity, data2);
    }
}