using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;

[Serializable]
public struct ConstrainedRotation : IComponentData
{
    public float3 track;
    public bool constrainX;
    public bool constrainY;
    public bool constrainZ;
}


public class ConstrainedRotationProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    public float3 track;
    public bool constrainX;
    public bool constrainY;
    public bool constrainZ;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new ConstrainedRotation
        {
            track = track,
            constrainX = constrainX,
            constrainY = constrainY,
            constrainZ = constrainZ
        };

        dstManager.AddComponentData(entity, data);
    }
}