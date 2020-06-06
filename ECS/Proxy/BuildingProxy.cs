using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;

[Serializable]
public struct Building : IComponentData
{
    public float3 position;
    public int buildingID;
    public e_BuildingTypes buildingType;
    public System.UInt32 buildingTemplate;
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

public class BuildingProxy : ComponentDataProxy<Building> { }
