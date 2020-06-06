using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;

[Serializable]
public struct FriendlyUnit : IComponentData
{
    //public Entity targetEntity;
    public float health;
}

public class FriendlyUnitProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    public float health;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new FriendlyUnit { health = health };
        dstManager.AddComponentData(entity, data);
    }
}