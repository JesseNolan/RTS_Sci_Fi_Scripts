using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
//using Unity.Transforms2D;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine.UI;
using Unity.Collections;

public class UISystem : ComponentSystem
{
    EntityQuery m_inputQuery;
    EntityQuery m_placingBuildingQuery;
    public EntityQuery q_ResourceStorage;
    private EntityQuery q_placingBuilding;

    private EntityQuery q_buildingSelected;
    private EntityQuery q_resourceSelected;

    static EntityQuery m_gameStateQuery;


    public static Camera mainCamera;

    public static GameObject Buttons_Buildings;
    public static Button Button_Terran_Habitat;
    public static Button Button_Terran_House;
    public static Button Button_Terran_ResidentBlock;
    public static Button Button_Terran_EnergySphere;
    public static Button Button_Terran_AquaStore;
    public static Button Button_Terran_PlasmaCannon;
    public static Button Button_Road;
    public static Button Button_Destroy;

    public static Button Button_Test;

    public static GameObject Buttons_Ships;
    public static Button Button_Terran_SmallShip;
    public static Button Button_Terran_MediumShip;
    public static Button Button_Terran_LargeShip;
    public static Button Button_Terran_SmallShip_Enemy;

    // Building canvases
    public static Canvas Canvas_Terran_Habitat;
    public static Canvas Canvas_Terran_House;
    public static Canvas Canvas_Terran_Resident_Block;
    public static Canvas Canvas_Terran_Aqua_Store;
    public static Canvas Canvas_Terran_Energy_Sphere;
    public static Canvas Canvas_Terran_Plasma_Cannon;

    // Resource canvases
    public static Canvas Canvas_Resource_Rock;
    public static Canvas Canvas_Resource_Iron;

    // Resource values
    public static Text Resource_Rock_Value;
    public static Text Resource_Iron_Value;



    public static Canvas Canvas_Buttons;

    public static Button Button_Test_Unit;


    // Resource storage value text fields
    public static Text ResourceStorage_Rock_Value;
    public static Text ResourceStorage_Iron_Value;
    public static Text ResourceStorage_Money_Value;

    // Resource cost UI
    public static Canvas Canvas_Building_Resource_Cost;
    public static RectTransform UI_Building_Resource_Cost;


    private Entity previousSelectedBuilding = Entity.Null;
    private int previousBuildingLength = 0;
    private Entity previousSelectedResource = Entity.Null;
    private int previousResourceLength = 0;


    private static e_ResourceTypes currentlyEnabledResourceType;


    protected override void OnCreate()
    {
        m_inputQuery = GetEntityQuery(typeof(PlayerInput));
        m_placingBuildingQuery = GetEntityQuery(typeof(PlacingBuilding));
        m_gameStateQuery = GetEntityQuery(typeof(GameState));
        q_buildingSelected = GetEntityQuery(typeof(Selected), typeof(Building));
        q_resourceSelected = GetEntityQuery(typeof(Selected), typeof(Resource));
        q_ResourceStorage = GetEntityQuery(typeof(ResourceStorage));
        q_placingBuilding = GetEntityQuery(typeof(PlacingBuilding), typeof(LocalToWorld));
    }


    public static void SetupGameObjects()
    {
        mainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();

        Buttons_Buildings = GameObject.Find("Buttons_Buildings");
        Button_Terran_Habitat = GameObject.Find("Button_Terran_Habitat").GetComponent<Button>();
        Button_Terran_Habitat.onClick.AddListener(BuildSystem.Spawn_Terran_Habitat);
        Button_Terran_House = GameObject.Find("Button_Terran_House").GetComponent<Button>();
        Button_Terran_House.onClick.AddListener(BuildSystem.Spawn_Terran_House);
        Button_Terran_ResidentBlock = GameObject.Find("Button_Terran_ResidentBlock").GetComponent<Button>();
        Button_Terran_ResidentBlock.onClick.AddListener(BuildSystem.Spawn_Terran_ResidentBlock);
        Button_Terran_EnergySphere = GameObject.Find("Button_Terran_EnergySphere").GetComponent<Button>();
        Button_Terran_EnergySphere.onClick.AddListener(BuildSystem.Spawn_Terran_EnergySphere);
        Button_Terran_AquaStore = GameObject.Find("Button_Terran_AquaStore").GetComponent<Button>();
        Button_Terran_AquaStore.onClick.AddListener(BuildSystem.Spawn_Terran_AquaStore);
        Button_Terran_PlasmaCannon = GameObject.Find("Button_Terran_PlasmaCannon").GetComponent<Button>();
        Button_Terran_PlasmaCannon.onClick.AddListener(BuildSystem.Spawn_Terran_PlasmaCannon);
        Button_Road = GameObject.Find("Button_Road").GetComponent<Button>();
        Button_Road.onClick.AddListener(StartRoadBuilding);
        Button_Destroy = GameObject.Find("Button_Destroy").GetComponent<Button>();
        Button_Destroy.onClick.AddListener(StartBuildingDestroy);


        Buttons_Ships = GameObject.Find("Buttons_Ships");
        Button_Terran_SmallShip = GameObject.Find("Button_Terran_SmallShip").GetComponent<Button>();
        Button_Terran_SmallShip.onClick.AddListener(FlightSystem.Spawn_Terran_SmallShip);
        Button_Terran_MediumShip = GameObject.Find("Button_Terran_MediumShip").GetComponent<Button>();
        Button_Terran_MediumShip.onClick.AddListener(FlightSystem.Spawn_Terran_MediumShip);
        Button_Terran_LargeShip = GameObject.Find("Button_Terran_LargeShip").GetComponent<Button>();
        Button_Terran_LargeShip.onClick.AddListener(FlightSystem.Spawn_Terran_LargeShip);
        Button_Terran_SmallShip_Enemy = GameObject.Find("Button_Terran_SmallShip_Enemy").GetComponent<Button>();
        Button_Terran_SmallShip_Enemy.onClick.AddListener(FlightSystem.Spawn_Terran_SmallShip_Enemy);
        Button_Test_Unit = GameObject.Find("Button_Test_Unit").GetComponent<Button>();
        Button_Test_Unit.onClick.AddListener(FlightSystem.Spawn_Test_Unit);

        // Building canvases
        Canvas_Terran_Habitat = GameObject.Find("Canvas_Terran_Habitat").GetComponent<Canvas>();
        Canvas_Terran_House = GameObject.Find("Canvas_Terran_House").GetComponent<Canvas>();
        Canvas_Terran_Resident_Block = GameObject.Find("Canvas_Terran_Resident_Block").GetComponent<Canvas>();
        Canvas_Terran_Aqua_Store = GameObject.Find("Canvas_Terran_Aqua_Store").GetComponent<Canvas>();
        Canvas_Terran_Energy_Sphere = GameObject.Find("Canvas_Terran_Energy_Sphere").GetComponent<Canvas>();
        Canvas_Terran_Plasma_Cannon = GameObject.Find("Canvas_Terran_Plasma_Cannon").GetComponent<Canvas>();

        // Resource Canvases
        Canvas_Resource_Rock = GameObject.Find("Canvas_Resource_Rock").GetComponent<Canvas>();
        Canvas_Resource_Iron = GameObject.Find("Canvas_Resource_Iron").GetComponent<Canvas>();

        // Resource value text fields
        Resource_Rock_Value = GameObject.Find("Resource_Rock_Value").GetComponent<Text>();
        Resource_Iron_Value = GameObject.Find("Resource_Iron_Value").GetComponent<Text>();

        // Resource Storage value text fields
        ResourceStorage_Rock_Value = GameObject.Find("ResourceStorage_Rock_Value").GetComponent<Text>();
        ResourceStorage_Iron_Value = GameObject.Find("ResourceStorage_Iron_Value").GetComponent<Text>();
        ResourceStorage_Money_Value = GameObject.Find("ResourceStorage_Money_Value").GetComponent<Text>();

        Canvas_Buttons = GameObject.Find("Canvas_Buttons").GetComponent<Canvas>();


        Canvas_Building_Resource_Cost = GameObject.Find("Canvas_Building_Resource_Cost").GetComponent<Canvas>();
        UI_Building_Resource_Cost = GameObject.Find("UI_Building_Resource_Cost").GetComponent<RectTransform>();

    }

    public static void StartRoadBuilding()
    {
        //var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        //entityManager.CreateEntity(typeof(UI_Command_StartRoadBuilding));
        var ent = MainLoader.entityManager.CreateEntity();
        ChangeGameStateRequest c = new ChangeGameStateRequest { newState = e_GameStates.state_RoadPlacement };
        MainLoader.entityManager.AddComponentData(ent, c);
    }

    public static void StartBuildingDestroy()
    {
        var ent = MainLoader.entityManager.CreateEntity();
        ChangeGameStateRequest c = new ChangeGameStateRequest { newState = e_GameStates.state_DestroyBuildings };
        MainLoader.entityManager.AddComponentData(ent, c);
    }

    public static void DisableAllCanvases()
    {
        Canvas_Terran_Habitat.enabled = false;
        Canvas_Terran_House.enabled = false;
        Canvas_Terran_Resident_Block.enabled = false;
        Canvas_Terran_Aqua_Store.enabled = false;
        Canvas_Terran_Energy_Sphere.enabled = false;
        Canvas_Terran_Plasma_Cannon.enabled = false;


        Canvas_Resource_Rock.enabled = false;
        Canvas_Resource_Iron.enabled = false;


        Canvas_Buttons.enabled = false;

        currentlyEnabledResourceType = e_ResourceTypes.NoResource;
    }

    public static void EnableBuildingCanvas(e_BuildingTypes type)
    {
        switch (type)
        {
            case e_BuildingTypes.Terran_Habitat:
                Canvas_Terran_Habitat.enabled = true;
                break;
            case e_BuildingTypes.Terran_House:
                Canvas_Terran_House.enabled = true;
                break;
            case e_BuildingTypes.Terran_Resident_Block:
                Canvas_Terran_Resident_Block.enabled = true;
                break;
            case e_BuildingTypes.Terran_Energy_Sphere:
                Canvas_Terran_Energy_Sphere.enabled = true;
                break;
            case e_BuildingTypes.Terran_Aqua_Store:
                Canvas_Terran_Aqua_Store.enabled = true;
                break;
            case e_BuildingTypes.Terran_Plasma_Cannon:
                Canvas_Terran_Plasma_Cannon.enabled = true;
                break;
            default:
                break;
        }   
    }

    public static void EnableResourceCanvas(e_ResourceTypes type)
    {
        currentlyEnabledResourceType = type;
        switch (type)
        {
            case e_ResourceTypes.NoResource:
                break;
            case e_ResourceTypes.Money:
                break;
            case e_ResourceTypes.Rock:
                Canvas_Resource_Rock.enabled = true;
                break;
            case e_ResourceTypes.Meat:
                break;
            case e_ResourceTypes.Vegetables:
                break;
            case e_ResourceTypes.Iron:
                Canvas_Resource_Iron.enabled = true;
                break;
            case e_ResourceTypes.Copper:
                break;
            case e_ResourceTypes.Gold:
                break;
            case e_ResourceTypes.Platinum:
                break;
            case e_ResourceTypes.Tin:
                break;
            default:
                break;
        }
    }

    public static void EnableButtonsCanvas()
    {
        Canvas_Buttons.enabled = true;
    }

    protected override void OnUpdate()
    {
        //var placingBuildings = m_placingBuildingQuery.ToComponentDataArray<PlacingBuilding>(Allocator.TempJob);
        var selectedBuilding = q_buildingSelected.ToComponentDataArray<Building>(Allocator.TempJob);
        var selectedBuildingEntity = q_buildingSelected.ToEntityArray(Allocator.TempJob);
        var selectedResources = q_resourceSelected.ToComponentDataArray<Resource>(Allocator.TempJob);
        var selectedResourcesEntity = q_resourceSelected.ToEntityArray(Allocator.TempJob);
        var resourceStorage = q_ResourceStorage.ToComponentDataArray<ResourceStorage>(Allocator.TempJob);
        var placingBuildingPos = q_placingBuilding.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);


        var state = m_gameStateQuery.ToComponentDataArray<GameState>(Allocator.TempJob);

        if ((state[0].gameState == e_GameStates.state_BuildingPlacement) || (state[0].gameState == e_GameStates.state_RoadPlacement))
        {
            Buttons_Buildings.SetActive(false);
        } else
        {
            Buttons_Buildings.SetActive(true);
        }

        /*###########################################################################*/
        // This handles activating the canvas of the currently selected object

        // nothing selected, show normal UI
        if ((selectedBuildingEntity.Length == 0) && (selectedResourcesEntity.Length == 0))
        {
            DisableAllCanvases();
            EnableButtonsCanvas();
        } else if (previousBuildingLength < selectedBuilding.Length)   // a building was selected
        {
            DisableAllCanvases();
            EnableBuildingCanvas(selectedBuilding[0].buildingType);
        } else if (previousResourceLength < selectedResources.Length)  // a resource was selected
        {
            DisableAllCanvases();
            EnableResourceCanvas(selectedResources[0].resourceType);
        } else if (selectedBuildingEntity.Length > 0)
        {
            if (selectedBuildingEntity[0] != previousSelectedBuilding)
            {
                DisableAllCanvases();
                EnableBuildingCanvas(selectedBuilding[0].buildingType);
            }
            previousSelectedBuilding = selectedBuildingEntity[0];
        } else if (selectedResourcesEntity.Length > 0)
        {
            if (selectedResourcesEntity[0] != previousSelectedResource)
            {
                DisableAllCanvases();
                EnableResourceCanvas(selectedResources[0].resourceType);
            }
            previousSelectedResource = selectedResourcesEntity[0];
        }

        previousBuildingLength = selectedBuilding.Length;
        previousResourceLength = selectedResources.Length;

        /*###########################################################################*/
        // This section handles updating the resource value in UI

        ResourceStorage_Rock_Value.text = resourceStorage[0].Rock.ToString();
        ResourceStorage_Iron_Value.text = resourceStorage[0].Iron.ToString();
        ResourceStorage_Money_Value.text = resourceStorage[0].Money.ToString();

        /*###########################################################################*/
        // This secion updates the amount of resource left for the selected resource on the map
        switch (currentlyEnabledResourceType)
        {
            case e_ResourceTypes.NoResource:
                break;
            case e_ResourceTypes.Money:
                break;
            case e_ResourceTypes.Rock:
                Resource_Rock_Value.text = selectedResources[0].resourceAmount.ToString();
                break;
            case e_ResourceTypes.Meat:
                break;
            case e_ResourceTypes.Vegetables:
                break;
            case e_ResourceTypes.Iron:
                Resource_Iron_Value.text = selectedResources[0].resourceAmount.ToString();
                break;
            case e_ResourceTypes.Copper:
                break;
            case e_ResourceTypes.Gold:
                break;
            case e_ResourceTypes.Platinum:
                break;
            case e_ResourceTypes.Tin:
                break;
            default:
                break;
        }

        /*###########################################################################*/

        if (placingBuildingPos.Length > 0)
        {
            Canvas_Building_Resource_Cost.enabled = true;
            LocalToWorld ltw = placingBuildingPos[0];
            float3 pos = ltw.Position.xyz;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(pos);
            float2 size = UI_Building_Resource_Cost.sizeDelta;
            screenPos.x += size.x;
            screenPos.y += size.y;
            UI_Building_Resource_Cost.anchoredPosition = screenPos;


        } else
        {
            Canvas_Building_Resource_Cost.enabled = false;
        }


        resourceStorage.Dispose();
        selectedResourcesEntity.Dispose();
        selectedResources.Dispose();
        selectedBuildingEntity.Dispose();
        selectedBuilding.Dispose();
        //placingBuildings.Dispose();
        state.Dispose();
        placingBuildingPos.Dispose();
    }
}


public struct UI_Command_StartRoadBuilding : IComponentData
{
}