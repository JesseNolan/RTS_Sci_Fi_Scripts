using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[RequiresEntityConversion]
public class BuildingTypeSpawnerProxy : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject Terran_Habitat;
    public GameObject Terran_House;
    public GameObject Terran_ResidentBlock;
    public GameObject Terran_EnergySphere;
    public GameObject Terran_AquaStore;
    public GameObject Terran_PlasmaCannon;
    public GameObject Road;

    public GameObject Terran_Habitat_Construction;
    public GameObject Terran_House_Construction;
    public GameObject Terran_ResidentBlock_Construction;
    public GameObject Terran_EnergySphere_Construction;
    public GameObject Terran_AquaStore_Construction;
    public GameObject Terran_PlasmaCannon_Construction;


    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
    {
        gameObjects.Add(Terran_Habitat);
        gameObjects.Add(Terran_House);
        gameObjects.Add(Terran_ResidentBlock);
        gameObjects.Add(Terran_EnergySphere);
        gameObjects.Add(Terran_AquaStore);
        gameObjects.Add(Terran_PlasmaCannon);
        gameObjects.Add(Terran_Habitat_Construction);
        gameObjects.Add(Terran_House_Construction);
        gameObjects.Add(Terran_ResidentBlock_Construction);
        gameObjects.Add(Terran_EnergySphere_Construction);
        gameObjects.Add(Terran_AquaStore_Construction);
        gameObjects.Add(Terran_PlasmaCannon_Construction);
        gameObjects.Add(Road);
    }

    // Lets you convert the editor data representation to the entity optimal runtime representation
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var spawnerData = new BuildingTypeSpawner
        {
            Terran_Habitat = conversionSystem.GetPrimaryEntity(Terran_Habitat),
            Terran_House = conversionSystem.GetPrimaryEntity(Terran_House),
            Terran_ResidentBlock = conversionSystem.GetPrimaryEntity(Terran_ResidentBlock),
            Terran_EnergySphere = conversionSystem.GetPrimaryEntity(Terran_EnergySphere),
            Terran_AquaStore = conversionSystem.GetPrimaryEntity(Terran_AquaStore),
            Terran_PlasmaCannon = conversionSystem.GetPrimaryEntity(Terran_PlasmaCannon),

            Terran_Habitat_Construction = conversionSystem.GetPrimaryEntity(Terran_Habitat_Construction),
            Terran_House_Construction = conversionSystem.GetPrimaryEntity(Terran_House_Construction),
            Terran_ResidentBlock_Construction = conversionSystem.GetPrimaryEntity(Terran_ResidentBlock_Construction),
            Terran_EnergySphere_Construction = conversionSystem.GetPrimaryEntity(Terran_EnergySphere_Construction),
            Terran_AquaStore_Construction = conversionSystem.GetPrimaryEntity(Terran_AquaStore_Construction),
            Terran_PlasmaCannon_Construction = conversionSystem.GetPrimaryEntity(Terran_PlasmaCannon_Construction),

            Road = conversionSystem.GetPrimaryEntity(Road),
        };
        dstManager.AddComponentData(entity, spawnerData);
    }
}

public struct BuildingTypeSpawner : IComponentData
{
    public Entity Terran_Habitat;
    public Entity Terran_House;
    public Entity Terran_ResidentBlock;
    public Entity Terran_EnergySphere;
    public Entity Terran_AquaStore;
    public Entity Terran_PlasmaCannon;

    public Entity Terran_Habitat_Construction;
    public Entity Terran_House_Construction;
    public Entity Terran_ResidentBlock_Construction;
    public Entity Terran_EnergySphere_Construction;
    public Entity Terran_AquaStore_Construction;
    public Entity Terran_PlasmaCannon_Construction;

    public Entity Road;
}
