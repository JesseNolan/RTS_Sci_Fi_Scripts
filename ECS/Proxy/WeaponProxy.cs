using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;


public class WeaponProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    public float firingDistance;
    public int firingRate;
    public float projectileSpeed;
    public float damage;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new Weapon
        {
            enabled = false,
            firingDistance = firingDistance,
            firingRate = firingRate,
            projectileSpeed = projectileSpeed,
            damage = damage,
        };

        dstManager.AddComponentData(entity, data);
    }
}

public struct Weapon : IComponentData
{
    public Entity targetEntity;
    public bool enabled;
    public int gotTarget;
    public int firingTimer;
    public int firingRate;  // number of ticks between firing
    public float firingDistance;
    public float projectileSpeed;
    public float damage;
    
}
