using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[RequiresEntityConversion]
public class GeneralSpawnerProxy : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject selectionObject;
    public GameObject testUnit;
    public GameObject projectile;

    public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
    {
        gameObjects.Add(selectionObject);
        gameObjects.Add(testUnit);
        gameObjects.Add(projectile);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var spawnerData = new GeneralSpawner
        {
            selectionObject = conversionSystem.GetPrimaryEntity(selectionObject),
            testUnit = conversionSystem.GetPrimaryEntity(testUnit),
            projectile = conversionSystem.GetPrimaryEntity(projectile),
        };
        dstManager.AddComponentData(entity, spawnerData);

        Debug.Log("GeneralSpawner created");
    }
}

public struct GeneralSpawner : IComponentData
{
    public Entity selectionObject;
    public Entity testUnit;
    public Entity projectile;
}
