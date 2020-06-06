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
    public GameObject Road_Straight;
    public GameObject Road_Corner;
    public GameObject Road_T;
    public GameObject Road_Intersection;

    public GameObject Terran_Habitat_Construction;
    public GameObject Terran_House_Construction;
    public GameObject Terran_ResidentBlock_Construction;
    public GameObject Terran_EnergySphere_Construction;
    public GameObject Terran_AquaStore_Construction;
    public GameObject Terran_PlasmaCannon_Construction;

    UInt32 Terran_Habitat_Template = 33554431;
    UInt32 Terran_House_Template = 473536;
    UInt32 Terran_ResidentBlock_Template = 473536;
    UInt32 Terran_EnergySphere_Template = 4096;
    UInt32 Terran_AquaStore_Template = 473536;
    UInt32 Terran_PlasmaCannon_Template = 6336;
    UInt32 Road_Straight_Template = 4096;
    UInt32 Road_Corner_Template = 4096;
    UInt32 Road_T_Template = 4096;
    UInt32 Road_Intersection_Template = 4096;

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
        gameObjects.Add(Road_Straight);
        gameObjects.Add(Road_Corner);
        gameObjects.Add(Road_T);
        gameObjects.Add(Road_Intersection);
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

            Road_Straight = conversionSystem.GetPrimaryEntity(Road_Straight),
            Road_Corner = conversionSystem.GetPrimaryEntity(Road_Corner),
            Road_T = conversionSystem.GetPrimaryEntity(Road_T),
            Road_Intersection = conversionSystem.GetPrimaryEntity(Road_Intersection),

            Terran_Habitat_Template = Terran_Habitat_Template,
            Terran_House_Template = Terran_House_Template,
            Terran_ResidentBlock_Template = Terran_ResidentBlock_Template,
            Terran_EnergySphere_Template = Terran_EnergySphere_Template,
            Terran_AquaStore_Template = Terran_AquaStore_Template,
            Terran_PlasmaCannon_Template = Terran_PlasmaCannon_Template,
            Road_Straight_Template = Road_Straight_Template,
            Road_Corner_Template = Road_Corner_Template,
            Road_T_Template = Road_T_Template,
            Road_Intersection_Template = Road_Intersection_Template,

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

    public Entity Road_Straight;
    public Entity Road_Corner;
    public Entity Road_T;
    public Entity Road_Intersection;

    public UInt32 Terran_Habitat_Template;
    public UInt32 Terran_House_Template;
    public UInt32 Terran_ResidentBlock_Template;
    public UInt32 Terran_EnergySphere_Template;
    public UInt32 Terran_AquaStore_Template;
    public UInt32 Terran_PlasmaCannon_Template;
    public UInt32 Road_Straight_Template;
    public UInt32 Road_Corner_Template;
    public UInt32 Road_T_Template;
    public UInt32 Road_Intersection_Template;
}
