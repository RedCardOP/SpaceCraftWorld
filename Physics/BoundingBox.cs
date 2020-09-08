using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundingBox{

    private const float EPSILON = 0.0001f;

    public float width, height, depth;
    public Vector3 center;
    //Width is x axis, Height is y axis, Depth is z axis
    public BoundingBox(Vector3 globalPosition, Vector3 dimensions) {
        this.width = dimensions.x / 2;
        this.height = dimensions.y / 2;
        this.depth = dimensions.z / 2;
        UpdateLocation(new Vector3(globalPosition.x, globalPosition.y, globalPosition.z));
    }
    public BoundingBox(Vector3 globalPosition, float width, float height, float depth) {
        this.width = width / 2;
        this.height = height / 2;
        this.depth = depth / 2;
        UpdateLocation(new Vector3(globalPosition.x, globalPosition.y, globalPosition.z));
    }

    //Returns floored coordinates for the edges of each dimension in the given bounding box
    public BoundingBoxOffsets GetBoundingBoxOffsets(Vector3 globalPos) {
        BoundingBoxOffsets bbo = new BoundingBoxOffsets();
        bbo.X_Neg = Mathf.FloorToInt(globalPos.x - width);
        bbo.X_Plus = Mathf.FloorToInt(globalPos.x + width - EPSILON);
        bbo.Y_Neg = Mathf.FloorToInt(globalPos.y - height);
        bbo.Y_Plus = Mathf.FloorToInt(globalPos.y + height - EPSILON);
        bbo.Z_Neg = Mathf.FloorToInt(globalPos.z - depth);
        bbo.Z_Plus = Mathf.FloorToInt(globalPos.z + depth - EPSILON);
        return bbo;
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

    //Changes the center based on the width/height/depth. To be used when giving corner coords of voxels
    public void offsetCenter() {
        center.x += width;
        center.y += height;
        center.z += depth;
    }

    public void offsetCenterYOnly()
    {
        center.y += height;
    }

    public Vector3 GetCenter() { return center; }

    public override string ToString() {
        return "BB " + center + " W:" + width + " H:" + height + " D:" + depth;
    }

    /*public BoundingBox Clone() {
        BoundingBox bb = new BoundingBox();
    }*/

    public override bool Equals(object other)
    {
        BoundingBox bb = (BoundingBox)other;
        return center.Equals(bb.center) && width == bb.width && height == bb.height && depth == bb.depth;
    }

    public static readonly BoundingBox AIR_BB = new BoundingBox(new Vector3(), 0, 0, 0);

}

public class BoundingBoxOffsets {

    public int X_Neg, X_Plus, Y_Neg, Y_Plus, Z_Neg, Z_Plus;

    public BoundingBoxOffsets() { }

    public BoundingBoxOffsets(int x_Neg, int x_Plus, int y_Neg, int y_Plus, int z_Neg, int z_Plus)
    {
        X_Neg = x_Neg;
        X_Plus = x_Plus;
        Y_Neg = y_Neg;
        Y_Plus = y_Plus;
        Z_Neg = z_Neg;
        Z_Plus = z_Plus;
    }
}

