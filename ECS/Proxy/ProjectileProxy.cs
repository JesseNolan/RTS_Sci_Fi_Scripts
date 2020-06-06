using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;

[Serializable]
public struct Projectile : IComponentData
{
    [HideInInspector] public Entity dst;
    [HideInInspector] public bool placedInBuffer;
    [HideInInspector] public bool targetHit;
    public float speed;
    public float damage;
}

public class ProjectileProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    public float speed;
    public float damage;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new Projectile { speed = speed, damage = damage };
        dstManager.AddComponentData(entity, data);
    }
}