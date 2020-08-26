using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundingBox : MonoBehaviour{

    public float width, height, depth;
    public Vector3 center;
    //Width is x axis, Height is y axis, Depth is z axis
    public BoundingBox(Vector3 globalPosition, float width, float height, float depth) {
        this.width = width / 2;
        this.height = height / 2;
        this.depth = depth / 2;
        UpdateLocation(globalPosition);
    }

    //Returns valid move capable from given 
    public Vector3 TryMove(Vector3 velocity, BoundingBox[] others) {
        Vector3 centerPostVelocity = center + velocity;
        foreach(BoundingBox bb in others) { 
            if (IsIntersecting(centerPostVelocity, bb)) {
                velocity.x = XMove(velocity.x, bb);
                velocity.y = YMove(velocity.y, bb);
                velocity.z = ZMove(velocity.z, bb);
            }
        }
        return velocity;
    }

    float XMove (float attemptedMove, BoundingBox other) { 
        if (Mathf.Abs(center.y - other.center.y) < (height + other.height) &&
            Mathf.Abs(center.z - other.center.z) < (depth + other.depth)) {
            float dstX = Mathf.Abs(center.x - other.center.x);
            float maxViableMove = dstX - width - other.width;
            if (maxViableMove <= 0)
                return 0;
            if (Mathf.Abs(attemptedMove) > maxViableMove) 
                return maxViableMove * Mathf.Sign(attemptedMove);
        }
        return attemptedMove;
    }
    float YMove(float attemptedMove, BoundingBox other) {
        if (Mathf.Abs(center.x - other.center.x) < (width + other.width) &&
            Mathf.Abs(center.z - other.center.z) < (depth + other.depth)) {
            float dstY = Mathf.Abs(center.y - other.center.y);
            float maxViableMove = dstY - height - other.height;
            if (maxViableMove <= 0)
                return 0;
            if (Mathf.Abs(attemptedMove) > maxViableMove)
                return maxViableMove * Mathf.Sign(attemptedMove);
        }
        return attemptedMove;
    }

    float ZMove (float attemptedMove, BoundingBox other) { 
        if (Mathf.Abs(center.y - other.center.y) < (height + other.height) &&
            Mathf.Abs(center.x - other.center.x) < (width + other.width)) {
            float dstZ = Mathf.Abs(center.z - other.center.z);
            float maxViableMove = dstZ - depth - other.depth;
            if (maxViableMove <= 0)
                return 0;
            if (Mathf.Abs(attemptedMove) > maxViableMove) 
                return maxViableMove * Mathf.Sign(attemptedMove);
        }
        return attemptedMove;
    }

    public bool IsIntersecting(BoundingBox other) {
        float dstX = Mathf.Abs(center.x - other.center.x);
        float dstY = Mathf.Abs(center.y - other.center.y);
        float dstZ = Mathf.Abs(center.z - other.center.z);
        return (dstX < width + other.width) &&
               (dstY < height + other.height) &&
               (dstZ < depth + other.depth);
    }

    public bool IsIntersecting(Vector3 altCenter, BoundingBox other)
    {
        float dstX = Mathf.Abs(altCenter.x - other.center.x);
        float dstY = Mathf.Abs(altCenter.y - other.center.y);
        float dstZ = Mathf.Abs(altCenter.z - other.center.z);
        return (dstX < width + other.width) &&
               (dstY < height + other.height) &&
               (dstZ < depth + other.depth);
    }

    public void UpdateLocation(Vector3 newLocation) {
        center = newLocation;
    }

    public Vector3 GetCenter() { return center; }

}

