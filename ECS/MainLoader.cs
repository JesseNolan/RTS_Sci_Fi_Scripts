using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
//using Unity.Transforms2D;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using CTS;
using UnityEditor;
using NavJob.Components;


public class MainLoader
{

    public static EntityArchetype tileArchetype;
    public static EntityArchetype PlayerControllerArchetype;

    public static Settings settings;

    public static EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialise()
    {
        //entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        //tileArchetype = entityManager.CreateArchetype(typeof(Tile), typeof(Transform), typeof(Position));
        PlayerControllerArchetype = entityManager.CreateArchetype(typeof(Transform), typeof(PlayerInput));

    }


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void RunGame()
    {
        Debug.Log("RUNGAME");

        

        settings = Settings.Instance;

        GameState gameStateInit = new GameState {
                                                    gameState = e_GameStates.state_Idle,
                                                    buildingID_Incrementer = 1,
                                                };

        var ent = entityManager.CreateEntity(typeof(GameState));
        entityManager.SetComponentData(ent, gameStateInit);

        Entity playerControllerEntity = entityManager.CreateEntity(PlayerControllerArchetype);

    }
}
