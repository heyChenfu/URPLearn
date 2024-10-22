using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BoidsECSSimulator
{
    public class BoidAuthoring : MonoBehaviour
    {
        public float minSpeed;
        public float maxSpeed;
        public float perceptionRadius;
        public float avoidanceRadius;
        public float maxSteerForce;

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
                AddSharedComponent(entity, new BoidSharedComponentData
                {
                    minSpeed = authoring.minSpeed,
                    maxSpeed = authoring.maxSpeed,
                    perceptionRadius = authoring.perceptionRadius,
                    avoidanceRadius = authoring.avoidanceRadius,
                    maxSteerForce = authoring.maxSteerForce,
                    alignWeight = authoring.alignWeight,
                    cohesionWeight = authoring.cohesionWeight,
                    seperateWeight = authoring.seperateWeight,
                    targetWeight = authoring.targetWeight,
                    avoidCollisionWeight = authoring.avoidCollisionWeight,
                    boundsRadius = authoring.boundsRadius,
                    collisionAvoidDst = authoring.collisionAvoidDst,
                    obstacleLayerMask = authoring.obstacleLayerMask,
                });
                AddComponent(entity, new BoidData());

            }
        }
    }

    [Serializable]
    public struct BoidData : IComponentData 
    {
        public float3 Forward;
        public float3 FlockHeading; //当前Boid感知到的所有邻居的方向总和
        public float3 FlockCentre; //当前Boid感知到的所有邻居的位置总和
        public float3 AvoidanceHeading; //当前Boid感知到的所有邻居的分离方向总和
        public int NumFlockmates; //当前Boid感知到的邻居数量
        public float3 Velocity;

    }

    [Serializable]
    public struct BoidSharedComponentData : ISharedComponentData
    {
        public float minSpeed;
        public float maxSpeed;
        public float perceptionRadius;//伙伴判定半径
        public float avoidanceRadius;//规避判定半径
        public float maxSteerForce;

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

