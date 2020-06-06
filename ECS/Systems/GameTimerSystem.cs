using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;


public class GameTimerSystem : JobComponentSystem
{
    private float timeCounter;


    public struct UpdateTimers : IJobForEachWithEntity<CountdownTimer>
    {

        public void Execute(Entity entity, int index, ref CountdownTimer c)
        {
            c.timerValue--;
        }
    }


    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {       
        timeCounter += Time.DeltaTime;
        if (timeCounter >= 1.0f)
        {
            timeCounter = 0;
            var updateTimers = new UpdateTimers
            { }.Schedule(this, inputDeps);

            return updateTimers;
        }
        return inputDeps;
    }
}


public struct CountdownTimer : IComponentData
{
    public int timerLength_secs;
    public int timerValue;
}