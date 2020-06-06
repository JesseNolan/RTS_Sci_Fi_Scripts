using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[RequiresEntityConversion]
public class RockTypeSpawnerProxy : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject Rock_v1;
    public GameObject Rock_v2;
    public GameObject Rock_v3;
    public GameObject Rock_v4;
    public GameObject Rock_v5;
    public GameObject Rock_v6;


    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
    {
        gameObjects.Add(Rock_v1);
        gameObjects.Add(Rock_v2);
        gameObjects.Add(Rock_v3);
        gameObjects.Add(Rock_v4);
        gameObjects.Add(Rock_v5);
        gameObjects.Add(Rock_v6);

    }

    // Lets you convert the editor data representation to the entity optimal runtime representation
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var spawnerData = new RockTypeSpawner
        {
            Rock_v1 = conversionSystem.GetPrimaryEntity(Rock_v1),
            Rock_v2 = conversionSystem.GetPrimaryEntity(Rock_v2),
            Rock_v3 = conversionSystem.GetPrimaryEntity(Rock_v3),
            Rock_v4 = conversionSystem.GetPrimaryEntity(Rock_v4),
            Rock_v5 = conversionSystem.GetPrimaryEntity(Rock_v5),
            Rock_v6 = conversionSystem.GetPrimaryEntity(Rock_v6),

        };
        dstManager.AddComponentData(entity, spawnerData);

        Debug.Log("RockTypeSpawner created");
    }
}

public struct RockTypeSpawner : IComponentData
{
    public Entity Rock_v1;
    public Entity Rock_v2;
    public Entity Rock_v3;
    public Entity Rock_v4;
    public Entity Rock_v5;
    public Entity Rock_v6;

}
