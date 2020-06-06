using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;

[Serializable]
public struct LookRotation : IComponentData
{
    public Entity target;
    public int constrainX;
    public int constrainY;
    public int constrainZ;
    public int gotTarget;
}


public class LookRotationProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    public bool constrainX;
    public bool constrainY;
    public bool constrainZ;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new LookRotation
        {
            constrainX = constrainX ? 1 : 0,
            constrainY = constrainY ? 1 : 0,
            constrainZ = constrainZ ? 1 : 0,
        };

        dstManager.AddComponentData(entity, data);
    }
}