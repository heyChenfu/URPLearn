
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace BoidsECSSimulator
{
    public class BoidObstacleAuthoring : MonoBehaviour
    {
        class BoidObstacleBaker : Baker<BoidObstacleAuthoring>
        {
            public override void Bake(BoidObstacleAuthoring authoring)
            {
                UnityEngine.MeshCollider meshCollider = GetComponent<UnityEngine.MeshCollider>();
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new BoidObstacle());
                if (meshCollider != null)
                {
                    var collider = Unity.Physics.MeshCollider.Create(meshDataArray[0], meshCollider.transform.localToWorldMatrix);
                    var physicsCollider = new PhysicsCollider();
                    physicsCollider.Value = collider;
                    AddComponent(entity, physicsCollider);
                }
            }
        }
    }

    public struct BoidObstacle : IComponentData
    {
    }

}
