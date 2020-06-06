using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
//using Unity.Transforms2D;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine.EventSystems;

/******************************************************/
// This system takes any input from the player and
// populates a datacomponent that has a copy of any input made
/******************************************************/
public class PlayerInputSystem : ComponentSystem
{
    public EntityQuery m_inputData;

    protected override void OnCreate()
    {
        m_inputData = GetEntityQuery(typeof(PlayerInput));
    }

    protected override void OnUpdate()
    {
        //var input = m_inputData.ToComponentDataArray<PlayerInput>(Unity.Collections.Allocator.TempJob);

        var inputEntities = m_inputData.ToEntityArray(Unity.Collections.Allocator.TempJob);
        var input = m_inputData.ToComponentDataArray<PlayerInput>(Unity.Collections.Allocator.TempJob);

        for (int i = 0; i < inputEntities.Length; i++)
        {
            PlayerInput playerInput;
            playerInput.mousePosScreen = Input.mousePosition;

            // this is a check to see whether we are over any UI element. 
            // we dont want clicking UI elements to click through to the game
            if (EventSystem.current.IsPointerOverGameObject())
            {
                playerInput.MouseButtonDown0 = false;
                playerInput.MouseButtonDown1 = false;

                playerInput.rotateLeft = false;
                playerInput.rotateRight = false;

                playerInput.MouseButtonUp0 = false;
                playerInput.MouseButtonUp1 = false;

                playerInput.MouseButtonHeld0 = false;
                playerInput.MouseButtonHeld1 = false;
            }
            else
            {
                playerInput.MouseButtonDown0 = Input.GetMouseButtonDown(0) ? true : false;
                playerInput.MouseButtonDown1 = Input.GetMouseButtonDown(1) ? true : false;

                playerInput.rotateLeft = Input.GetKeyDown(KeyCode.Q) ? true : false;
                playerInput.rotateRight = Input.GetKeyDown(KeyCode.E) ? true : false;

                playerInput.MouseButtonUp0 = Input.GetMouseButtonUp(0) ? true : false;
                playerInput.MouseButtonUp1 = Input.GetMouseButtonUp(1) ? true : false;

                playerInput.MouseButtonHeld0 = Input.GetMouseButton(0) ? true : false;
                playerInput.MouseButtonHeld1 = Input.GetMouseButton(1) ? true : false;
            }


            playerInput.waypoint = Input.GetKey(KeyCode.T) ? 1 : 0;
            playerInput.shift = Input.GetKey(KeyCode.LeftShift);

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            // we hit the terrain
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerInfo.terrainLayer))
            {
                playerInput.terrainHitPos = hit.point;
            }
            else // we didnt hit the terrain, remove any selected tiles
            {
                playerInput.terrainHitPos = new Vector3(-1, -1, -1);
            }

            MainLoader.entityManager.SetComponentData(inputEntities[0], playerInput);

            inputEntities.Dispose();
            input.Dispose();

            //input[i] = playerInput;

            //if (Input.GetMouseButtonDown(0))
            //{
            //    Debug.Log(playerInput.mousePosScreen);
            //}
        }

    }
}

public struct PlayerInput : IComponentData
{
    public Vector3 mousePosScreen;
    public float3 terrainHitPos;
    public bool MouseButtonDown0;
    public bool MouseButtonDown1;
    public bool MouseButtonUp0;
    public bool MouseButtonUp1;
    public bool MouseButtonHeld0;
    public bool MouseButtonHeld1;
    public bool rotateLeft;
    public bool rotateRight;
    public bool shift;
    public int waypoint;
}


