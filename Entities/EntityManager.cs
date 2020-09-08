using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    public static List<Entity> entities = new List<Entity>();
    public static List<Entity> entitiesToFrameUpdate = new List<Entity>();

    void FixedUpdate() {
        foreach(Entity e in entities.ToList()) {
            e.FixedUpdate();
        }    
    }

    void Update() {
        foreach(Entity e in entitiesToFrameUpdate.ToList()) {
            e.Update();
        } 
    }

    //TEMPORARY FUNCTION
    public static Entity EntityIntersecting(BoundingBox pointerCheckBoundingBox) {
        foreach (Entity e in entities.ToList()) {
            if (e.GetEntityBoundingBox().IsIntersecting(pointerCheckBoundingBox))
                return e;
        }
        return null;
    }

    public static Entity EntityIntersecting(BoundingBox pointerCheckBoundingBox, Type typeFilter) {
        foreach (Entity e in entities.ToList()) {
            if (e.GetType().Equals(typeFilter) && e.GetEntityBoundingBox().IsIntersecting(pointerCheckBoundingBox))
                return e;
        }
        return null;
    }



}
