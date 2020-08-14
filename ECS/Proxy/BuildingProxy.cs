using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;

[Serializable]
public struct Building : IComponentData
{
    public float3 position;
    public quaternion rotation;
    public int buildingID;
    public e_BuildingTypes buildingType;
    public float startingHealth;

    public float2x2 templateCoords;

    // GatherResource variables
    public int tileRadius;
    public e_ResourceTypes gatherableType;
    public int gatherAmount;
}

public enum e_BuildingTypes
{
    Terran_Habitat,
    Terran_House,
    Terran_Resident_Block,
    Terran_Energy_Sphere,
    Terran_Aqua_Store,
    Terran_Plasma_Cannon,
}


public class BuildingProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    [HideInInspector] public float3 position;
    [HideInInspector] public quaternion rotation;
    [HideInInspector] public int buildingID;
    public e_BuildingTypes buildingType;
    public float startingHealth;

    public float2x2 templateCoords;

    // GatherResource variables
    public int tileRadius;
    public e_ResourceTypes gatherableType;
    public int gatherAmount;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new Building
        {
            buildingType = buildingType,
            startingHealth = startingHealth,
            tileRadius = tileRadius,
            gatherableType = gatherableType,
            gatherAmount = gatherAmount,
            templateCoords = templateCoords
        };

        dstManager.AddComponentData(entity, data);
    }
}
