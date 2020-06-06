using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;

[Serializable]
public struct EnemyUnit : IComponentData
{
    //public Entity targetEntity;
    public float health;
}

public class EnemyUnitProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    public float health;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new EnemyUnit { health = health };
        dstManager.AddComponentData(entity, data);
    }
}


