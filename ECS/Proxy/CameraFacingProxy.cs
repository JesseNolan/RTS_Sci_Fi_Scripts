using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;

public class CameraFacingProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new CameraFacing { };
        dstManager.AddComponentData(entity, data);
    }
}

public struct CameraFacing : IComponentData
{
}