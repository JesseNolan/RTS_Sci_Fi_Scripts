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
using Unity.Burst;

public class GameStateSystem : JobComponentSystem
{
    public EntityQuery q_stateChange;
    public EntityQuery q_currState;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var changeState = q_stateChange.ToComponentDataArray<ChangeGameStateRequest>(Allocator.TempJob);
        var changeStateEntity = q_stateChange.ToEntityArray(Allocator.TempJob);
        var state = q_currState.ToComponentDataArray<GameState>(Allocator.TempJob);
        var stateEntity = q_currState.ToEntityArray(Allocator.TempJob);

        if (changeState.Length == 1)
        {
            Debug.Log("Changing States");
            GameState s = state[0];
            s.gameState = changeState[0].newState;
            MainLoader.entityManager.SetComponentData(stateEntity[0], s);
            MainLoader.entityManager.DestroyEntity(changeStateEntity[0]);
        } else if (changeState.Length > 1)
        {
            Debug.LogError("multiple things trying to change game state at once");
        }

        changeState.Dispose();
        changeStateEntity.Dispose();
        state.Dispose();
        stateEntity.Dispose();

        return inputDeps;
    }

    protected override void OnCreate()
    {
        q_stateChange = GetEntityQuery(typeof(ChangeGameStateRequest));
        q_currState = GetEntityQuery(typeof(GameState));
    }
}

public struct ChangeGameStateRequest : IComponentData
{
    public e_GameStates newState;
}

public struct GameState : IComponentData
{
    public e_GameStates gameState;
    public int buildingID_Incrementer;
    public int resourceID_Incrementer;
}

public enum e_GameStates
{
    state_Idle = 0,
    state_BuildingPlacement,
    state_RoadPlacement,
    state_DestroyBuildings,
}


public enum e_SelectionContext
{
    Road = 0,
    TileableBuilding,
    Building,
    Unit,
}

