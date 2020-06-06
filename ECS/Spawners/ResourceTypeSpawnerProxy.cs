using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[RequiresEntityConversion]
public class ResourceTypeSpawnerProxy : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject Resource_Rock;
    public GameObject Resource_Iron;



    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
    {
        gameObjects.Add(Resource_Rock);
        gameObjects.Add(Resource_Iron);

    }

    // Lets you convert the editor data representation to the entity optimal runtime representation
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var spawnerData = new ResourceTypeSpawner
        {
            Resource_Rock = conversionSystem.GetPrimaryEntity(Resource_Rock),
            Resource_Iron = conversionSystem.GetPrimaryEntity(Resource_Iron),


        };
        dstManager.AddComponentData(entity, spawnerData);

        Debug.Log("ResourceTypeSpawner created");
    }
}

public struct ResourceTypeSpawner : IComponentData
{
    public Entity Resource_Rock;
    public Entity Resource_Iron;


}
