
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BoidsECSSimulator
{
    public class BoidSpawnerAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        public int SpawnNumber = 10;
        public float SpawnRange = 5;
        public float minSpeed;
        public float maxSpeed;
        public float boundsRadius;
        public float collisionAvoidDst;
        public LayerMask obstacleLayerMask;

    }

    class BoidSpawnerBaker : Baker<BoidSpawnerAuthoring>
    {
        public override void Bake(BoidSpawnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new BoidSpawnerComponentData
            {
                // By default, each authoring GameObject turns into an Entity.
                // Given a GameObject (or authoring component), GetEntity looks up the resulting Entity.
                Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                SpawnPosition = authoring.transform.position,
                SpawnNumber = authoring.SpawnNumber,
                SpawnRange = authoring.SpawnRange,
                minSpeed = authoring.minSpeed,
                maxSpeed = authoring.maxSpeed,
                boundsRadius = authoring.boundsRadius,
                collisionAvoidDst = authoring.collisionAvoidDst,
                obstacleLayerMask = authoring.obstacleLayerMask,
            });
        }
    }

    [Serializable]
    public struct BoidSpawnerComponentData : IComponentData
    {
        public Entity Prefab;
        public float3 SpawnPosition;
        public int SpawnNumber;
        public float SpawnRange;
        public float minSpeed;
        public float maxSpeed;
        public float boundsRadius;
        public float collisionAvoidDst;
        public int obstacleLayerMask;

    }

}
