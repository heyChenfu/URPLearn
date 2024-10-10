using System;
using Unity.Entities;

namespace BoidsECSSimulator 
{
    [Serializable]
    public class BoidsSharedComponentData : ISharedComponentData
    {
        public float alignWeight;
        public float cohesionWeight;
        public float seperateWeight;
        public float targetWeight;

    }

    public class BoidComponentData : IComponentData 
    {
        public float minSpeed;
        public float maxSpeed;
        public float boundsRadius;
        public float collisionAvoidDst;
        public int obstacleLayerMask;

    }

}


