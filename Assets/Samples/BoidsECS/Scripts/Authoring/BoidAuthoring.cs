using System;
using Unity.Entities;
using UnityEngine;

namespace BoidsECSSimulator
{
    public class BoidAuthoring : MonoBehaviour
    {

    }

    [Serializable]
    public struct BoidsSharedComponentData : ISharedComponentData
    {
        public float alignWeight;
        public float cohesionWeight;
        public float seperateWeight;
        public float targetWeight;

    }

    [Serializable]
    public struct BoidComponentData : IComponentData
    {
        public float minSpeed;
        public float maxSpeed;
        public float boundsRadius;
        public float collisionAvoidDst;
        public int obstacleLayerMask;

    }

}

