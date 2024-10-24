

using Unity.Entities;
using UnityEngine;

namespace BoidsECSSimulator
{
    public class BoidTargetAuthoring : MonoBehaviour
    {
        public int TargetGroupId;

        class BoidTargetBaker : Baker<BoidTargetAuthoring>
        {
            public override void Bake(BoidTargetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new BoidTarget() {
                    TargetGroupId = authoring.TargetGroupId,
                });
            }
        }
    }

    public struct BoidTarget : IComponentData
    {
        public int TargetGroupId;
    }

}
