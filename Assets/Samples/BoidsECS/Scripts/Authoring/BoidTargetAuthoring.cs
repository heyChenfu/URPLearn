

using Unity.Entities;
using UnityEngine;

namespace BoidsECSSimulator
{
    public class BoidTargetAuthoring : MonoBehaviour
    {

        class BoidTargetBaker : Baker<BoidTargetAuthoring>
        {
            public override void Bake(BoidTargetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new BoidTarget());
            }
        }
    }

    public struct BoidTarget : IComponentData
    {
    }

}
