using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;


public class SpaceShipProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    public float speed;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new SpaceShip
        {
            speed = speed,            
        };

        dstManager.AddComponentData(entity, data);
    }
}


public struct SpaceShip : IComponentData
{
    public float speed;
    public float3 dest;
}
