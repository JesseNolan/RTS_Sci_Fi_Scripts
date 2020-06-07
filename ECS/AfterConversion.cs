using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

[UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
public class AfterConversion : GameObjectConversionSystem
{
    bool started = false;

    protected override void OnUpdate()
    {
        Debug.Log("AfterConversion executed");
        if (!started)
        {


            TileSelectionSystem.spawnSelectedTile();



            //UISystem.SetupGameObjects();

            //TerrainSystem.SetupTerrain();

            //TerrainSystem.generateRocks();

            //TerrainSystem.LoadTileMap();

            ////TerrainSystem.GenerateResources();

            //ResourceSystem.CreateResourceStorage();

            ////ResourceSystem.SpawnResources();

            ////LocalNavMeshBuilder.Instance.updateMeshes();

            ////RoadSystem.InitialiseRoads();


            started = true;
        }
       
    }

}
