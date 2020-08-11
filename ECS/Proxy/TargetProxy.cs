using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;

public struct Target : IComponentData
{
    public float health;
}

public class TargetProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    public float health;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new Target { health = health };
        dstManager.AddComponentData(entity, data);
    }
}