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


public class RotationSystem : SystemBase
{


    protected override void OnUpdate()
    {
        Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref ConstrainedRotation c, ref Rotation r, in LocalToWorld l) =>
            {
                Vector3 targetDirection = c.track - l.Position;

                Quaternion rot = r.Value;

                Debug.DrawRay(l.Position, l.Forward, Color.red);
                Debug.DrawRay(l.Position, targetDirection, Color.green);

                Quaternion oldRotation = r.Value;
                Quaternion newRotation = Quaternion.LookRotation(targetDirection);
                Vector3 oldEuler = oldRotation.eulerAngles;
                Vector3 newEuler = newRotation.eulerAngles;

                // apply the rotation constaints in euler form
                if (c.constrainX)
                {
                    newEuler.x = oldEuler.x;
                }
                if (c.constrainY)
                {
                    newEuler.y = oldEuler.y;
                }
                if (c.constrainZ)
                {
                    newEuler.z = oldEuler.z;
                }

                // change the object rotation
                r.Value = Quaternion.Euler(newEuler);

            }).Schedule();


        Entities
           .ForEach((Entity entity, int entityInQueryIndex, ref ConstrainedRotation c) =>
           {
               // get the root entity
               Entity nextEnt = entity;
               while (HasComponent<Parent>(nextEnt))
               {
                   nextEnt = GetComponent<Parent>(nextEnt).Value;
               }
               
               // if the root entity has a weapon and that weapon has a target, get the targets position and assign it 
               // for the ConstrainedRotation to track
               if (HasComponent<Weapon>(nextEnt))
               {
                   Weapon w = GetComponent<Weapon>(nextEnt);
                   if (w.gotTarget == 1)
                   {
                       if (HasComponent<LocalToWorld>(w.targetEntity))
                       {
                           c.track = GetComponent<LocalToWorld>(w.targetEntity).Position;
                       }
                   }
               }


           }).Schedule();


    }
}
