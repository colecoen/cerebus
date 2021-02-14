using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController3 : PlayerRaycastManager
{
    public float maxSlopeAngle = 60;
    public CollisionInfo collisions;


    [System.Serializable]
    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public void Reset()
        {
            above = below = false;
            left = right = false;
        }
    }
}
