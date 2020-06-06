using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public struct GridHighlightSpawner : IComponentData
{
    public Entity Prefab;
}

public class GridHightlightSpawnerProxy : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject Prefab;

    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
    {
        gameObjects.Add(Prefab);
    }

    // Lets you convert the editor data representation to the entity optimal runtime representation

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var spawnerData = new GridHighlightSpawner
        {
            // The referenced prefab will be converted due to DeclareReferencedPrefabs.
            // So here we simply map the game object to an entity reference to that prefab.
            Prefab = conversionSystem.GetPrimaryEntity(Prefab),
        };
        dstManager.AddComponentData(entity, spawnerData);
    }
}