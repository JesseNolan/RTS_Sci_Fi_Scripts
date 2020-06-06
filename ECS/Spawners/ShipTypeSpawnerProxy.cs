using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[RequiresEntityConversion]
public class ShipTypeSpawnerProxy : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject smallShip;
    public GameObject mediumShip;
    public GameObject largeShip;
    public GameObject enemy_smallShip;

    public int smallShip_count;
    public int mediumShip_count;
    public int largeShip_count;
    public int enemy_smallShip_count;

    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
    {
        gameObjects.Add(smallShip);
        gameObjects.Add(mediumShip);
        gameObjects.Add(largeShip);
        gameObjects.Add(enemy_smallShip);
    }

    // Lets you convert the editor data representation to the entity optimal runtime representation

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var spawnerData = new ShipTypeSpawner
        {
            smallShip = conversionSystem.GetPrimaryEntity(smallShip),
            mediumShip = conversionSystem.GetPrimaryEntity(mediumShip),
            largeShip = conversionSystem.GetPrimaryEntity(largeShip),
            enemy_smallShip = conversionSystem.GetPrimaryEntity(enemy_smallShip),

            smallShip_count = smallShip_count,
            mediumShip_count = mediumShip_count,
            largeShip_count = largeShip_count,
            enemy_smallShip_count = enemy_smallShip_count,
        };
        dstManager.AddComponentData(entity, spawnerData);

        Debug.Log("ShipTypeSpawner created");
    }
}

public struct ShipTypeSpawner : IComponentData
{
    public Entity smallShip;
    public Entity mediumShip;
    public Entity largeShip;
    public Entity enemy_smallShip;

    public int smallShip_count;
    public int mediumShip_count;
    public int largeShip_count;
    public int enemy_smallShip_count;
}
