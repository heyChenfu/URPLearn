
using Unity.Entities;
using UnityEngine;

namespace BoidsECSSimulator
{
    public class BoidObstacleAuthoring : MonoBehaviour
    {
        class BoidObstacleBaker : Baker<BoidObstacleAuthoring>
        {
            public override void Bake(BoidObstacleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new BoidObstacle());
            }
        }
    }

    public struct BoidObstacle : IComponentData
    {
    }

}
