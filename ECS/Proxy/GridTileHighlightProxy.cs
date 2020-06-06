using UnityEngine;
using Unity.Entities;
using System;
using Unity.Mathematics;

[Serializable]
public struct GridTileHighlight : IComponentData
{
    public int tileIndex;
}

public class GridTileHighlightProxy : MonoBehaviour, IConvertGameObjectToEntity
{
    public int tileIndex;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var data = new GridTileHighlight { tileIndex = tileIndex };
        dstManager.AddComponentData(entity, data);
    }
}