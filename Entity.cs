using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.GameCenter;

public abstract class Entity {

    public int BBsChecked;

    public virtual Vector3 MoveEntity(Vector3 velocity, World world) {
        BoundingBox entityBB = GetEntityBoundingBox();
        Vector3 entityCenter = entityBB.center;
        Vector3 entityGlobalPos = World.GetFlooredVector3(entityCenter);
        Chunk chunk = world.GetChunk(entityGlobalPos);
        List<BoundingBox> boundingBoxes = new List<BoundingBox>();
        List<Vector3> boxCoordsToCheck = new List<Vector3>();
        BoundingBoxOffsets bbo = entityBB.GetBoundingBoxOffsets(entityCenter + velocity);
        
        //X Checks
        if (velocity.x < 0) { //Left check
            for (int y = 0; y <= (bbo.Y_Plus - bbo.Y_Neg); y++) {
                for (int z = 0; z <= (bbo.Z_Plus - bbo.Z_Neg); z++) {
                    boxCoordsToCheck.Add(new Vector3(bbo.X_Neg, bbo.Y_Neg + y, bbo.Z_Neg + z));
                }
            }
        } else if (velocity.x > 0) { //Right check
            for (int y = 0; y <= (bbo.Y_Plus - bbo.Y_Neg); y++) {
                for (int z = 0; z <= (bbo.Z_Plus - bbo.Z_Neg); z++) {
                    boxCoordsToCheck.Add(new Vector3(bbo.X_Plus, bbo.Y_Neg + y, bbo.Z_Neg + z));
                }
            }
        }
        //Y Checks
        if (velocity.y < 0) { //Down check
            for (int x = 0; x <= (bbo.X_Plus - bbo.X_Neg); x++) {
                for (int z = 0; z <= (bbo.Z_Plus - bbo.Z_Neg); z++) {
                    boxCoordsToCheck.Add(new Vector3(bbo.X_Neg + x, bbo.Y_Neg, bbo.Z_Neg + z));
                }
            }
        }

        if (velocity.y > 0) { //Up check
            for (int x = 0; x <= (bbo.X_Plus - bbo.X_Neg); x++) {
                for (int z = 0; z <= (bbo.Z_Plus - bbo.Z_Neg); z++) {
                    boxCoordsToCheck.Add(new Vector3(bbo.X_Neg + x, bbo.Y_Plus, bbo.Z_Neg + z));
                }
            }
        }

        //Z Checks
        if (velocity.z < 0) { //Backward check
            for (int y = 0; y <= (bbo.Y_Plus - bbo.Y_Neg); y++) {
                for (int x = 0; x <= (bbo.X_Plus - bbo.X_Neg); x++) {
                    boxCoordsToCheck.Add(new Vector3(bbo.X_Neg + x, bbo.Y_Neg + y, bbo.Z_Neg));
                }
            }
        } else if (velocity.z > 0) { //Forward check
            for (int y = 0; y <= (bbo.Y_Plus - bbo.Y_Neg); y++) {
                for (int x = 0; x <= (bbo.X_Plus - bbo.X_Neg); x++) {
                    boxCoordsToCheck.Add(new Vector3(bbo.X_Neg + x, bbo.Y_Neg + y, bbo.Z_Plus));
                }
            }
        }

        foreach (Vector3 iterationGlobalPos in boxCoordsToCheck) {
            if (chunk.IsVoxelInChunk(iterationGlobalPos)) {
                BoundingBox bb = chunk.GetBlockBoundingBox(iterationGlobalPos);
                if (!bb.Equals(BoundingBox.AIR_BB))
                    boundingBoxes.Add(bb);
            }
            else {
                BoundingBox bb = world.GetChunk(iterationGlobalPos).GetBlockBoundingBox(iterationGlobalPos);
                if (!bb.Equals(BoundingBox.AIR_BB))
                    boundingBoxes.Add(bb);
            }
        }

        /*if (chunk.IsVoxelInChunk(iterationGlobalPos))
        {
            BoundingBox bb = chunk.GetBlockBoundingBox(iterationGlobalPos);
            if (!bb.Equals(BoundingBox.AIR_BB))
                boundingBoxes.Add(bb);
        }
        else
        {
            BoundingBox bb = world.GetChunk(iterationGlobalPos).GetBlockBoundingBox(iterationGlobalPos);
            if (!bb.Equals(BoundingBox.AIR_BB))
                boundingBoxes.Add(bb);
        }*/
        BBsChecked = boundingBoxes.Count;
        Vector3 finalVelocity = entityBB.TryMove(velocity, boundingBoxes.ToArray());
        TranslateEntity(finalVelocity);
        //Debug.Log("iV: " + velocity + " fV: " + finalVelocity);
        return finalVelocity;
    }

    public abstract BoundingBox GetEntityBoundingBox();

    protected abstract void TranslateEntity(Vector3 velocity);

    public abstract void Interact(Entity entity);

    public abstract void FixedUpdate();
    public virtual void Update() { }
    public virtual void Start() { }

}
