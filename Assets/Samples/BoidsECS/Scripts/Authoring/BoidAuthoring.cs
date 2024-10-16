using System;
using Unity.Entities;
using UnityEngine;

namespace BoidsECSSimulator
{
    public class BoidAuthoring : MonoBehaviour
    {
        public float minSpeed;
        public float maxSpeed;
        public float alignWeight;
        public float cohesionWeight;
        public float seperateWeight;
        public float targetWeight;
        public float avoidCollisionWeight;
        public float boundsRadius;
        public float collisionAvoidDst;
        public LayerMask obstacleLayerMask;

        class BoidBaker : Baker<BoidAuthoring>
        {
            public override void Bake(BoidAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable | TransformUsageFlags.WorldSpace);
                AddSharedComponent(entity, new BoidComponentData
                {
                    minSpeed = authoring.minSpeed,
                    maxSpeed = authoring.maxSpeed,
                    alignWeight = authoring.alignWeight,
                    cohesionWeight = authoring.cohesionWeight,
                    seperateWeight = authoring.seperateWeight,
                    targetWeight = authoring.targetWeight,
                    avoidCollisionWeight = authoring.avoidCollisionWeight,
                    boundsRadius = authoring.boundsRadius,
                    collisionAvoidDst = authoring.collisionAvoidDst,
                    obstacleLayerMask = authoring.obstacleLayerMask,
                });
            }
        }
    }

    [Serializable]
    public struct BoidComponentData : ISharedComponentData
    {
        public float minSpeed;
        public float maxSpeed;
        public float alignWeight;
        public float cohesionWeight;
        public float seperateWeight;
        public float targetWeight;
        public float avoidCollisionWeight;
        public float boundsRadius;
        public float collisionAvoidDst;
        public int obstacleLayerMask;

    }

}

