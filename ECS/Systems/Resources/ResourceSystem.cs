using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine.AI;
using Unity.Jobs;

public class ResourceSystem : JobComponentSystem
{
    public EntityQuery q_ResourceStorage;
    public EntityCommandBufferSystem commandBuffer;


    protected override void OnCreate()
    {
        q_ResourceStorage = GetEntityQuery(typeof(ResourceStorage));
        commandBuffer = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }


    public struct RemoveEmptyResources : IJobForEachWithEntity<Resource>
    {
        public EntityCommandBuffer.Concurrent CommandBuffer;

        public void Execute(Entity entity, int index, ref Resource r)
        {
            if (r.resourceAmount <= 0)
            {


                CommandBuffer.DestroyEntity(index, entity);
            }
        }
    }



    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var removeEmptyResources = new RemoveEmptyResources
        {
            CommandBuffer = commandBuffer.CreateCommandBuffer().ToConcurrent(),
        }.Schedule(this, inputDeps);


        return removeEmptyResources;
    }
    

    public static void CreateResourceStorage() 
    {
        var ent = MainLoader.entityManager.CreateEntity(typeof(ResourceStorage));
        Settings s = MainLoader.settings;

        ResourceStorage r = new ResourceStorage
        {
            Money = s.Starting_Money,
            Rock = s.Starting_Rock,
            Meat = s.Starting_Meat,
            Vegetables = s.Starting_Vegetables,
            Iron = s.Starting_Iron,
            Copper = s.Starting_Copper,
            Gold = s.Starting_Gold,
            Platinum = s.Starting_Platinum,
            Tin = s.Starting_Tin,
        };

        MainLoader.entityManager.SetComponentData(ent, r);     
    }

    //public bool UpdateResource(e_ResourceTypes type, int amount)
    //{
    //    switch (type)
    //    {
    //        case e_ResourceTypes.Rock:
    //            if ((Resource_Stored_Rock + amount) >= 0)
    //            {
    //                Resource_Stored_Rock += amount;
    //                return true;
    //            }
    //            else
    //                return false;                
    //        case e_ResourceTypes.Meat:
    //            if ((Resource_Stored_Meat + amount) >= 0)
    //            {
    //                Resource_Stored_Meat += amount;
    //                return true;
    //            }
    //            else
    //                return false;
    //        case e_ResourceTypes.Vegetables:
    //            if ((Resource_Stored_Vegetables + amount) >= 0)
    //            {
    //                Resource_Stored_Vegetables += amount;
    //                return true;
    //            }
    //            else
    //                return false;
    //        case e_ResourceTypes.Iron:
    //            if ((Resource_Stored_Iron + amount) >= 0)
    //            {
    //                Resource_Stored_Iron += amount;
    //                return true;
    //            }
    //            else
    //                return false;
    //        case e_ResourceTypes.Copper:
    //            if ((Resource_Stored_Copper + amount) >= 0)
    //            {
    //                Resource_Stored_Copper += amount;
    //                return true;
    //            }
    //            else
    //                return false;
    //        case e_ResourceTypes.Gold:
    //            if ((Resource_Stored_Gold + amount) >= 0)
    //            {
    //                Resource_Stored_Gold += amount;
    //                return true;
    //            }
    //            else
    //                return false;
    //        case e_ResourceTypes.Platinum:
    //            if ((Resource_Stored_Platinum + amount) >= 0)
    //            {
    //                Resource_Stored_Platinum += amount;
    //                return true;
    //            }
    //            else
    //                return false;
    //        case e_ResourceTypes.Tin:
    //            if ((Resource_Stored_Tin + amount) >= 0)
    //            {
    //                Resource_Stored_Tin += amount;
    //                return true;
    //            }
    //            else
    //                return false;
    //        default:
    //            return false;
    //    }
    //}
}


public struct ResourceStorage : IComponentData
{
    public int Money;
    public int Rock;
    public int Meat;
    public int Vegetables;
    public int Iron;
    public int Tin;
    public int Copper;
    public int Gold;
    public int Platinum;
}

public struct Resource : IComponentData
{
    public int resourceID;
    public e_ResourceTypes resourceType;
    public int resourceAmount;
}

public enum e_ResourceTypes
{
    NoResource,
    Money,
    Rock,
    Meat,
    Vegetables,
    Iron,
    Copper,
    Gold,
    Platinum,
    Tin

}

