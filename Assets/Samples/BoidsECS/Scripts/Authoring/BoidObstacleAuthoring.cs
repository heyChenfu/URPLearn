
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BoidsECSSimulator
{
    public class BoidObstacleAuthoring : MonoBehaviour
    {
        class BoidObstacleBaker : Baker<BoidObstacleAuthoring>
        {
            public override void Bake(BoidObstacleAuthoring authoring)
            {
                Debug.Log("Baking BoidObstacleBaker:" + authoring.gameObject.name);
                MeshFilter meshFilter = GetComponent<MeshFilter>();
                //UnityEngine.MeshCollider meshCollider = GetComponent<UnityEngine.MeshCollider>();
                Entity entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new BoidObstacle());
                if (meshFilter != null)
                {
                    //PhysicsCollider会被自动转换被添加?
                    // Mesh.MeshDataArray meshDataArray = Mesh.AcquireReadOnlyMeshData(meshFilter.sharedMesh);
                    // Mesh.MeshData meshData = meshDataArray[0];
                    // int indexCount = 0;
                    // for (int i = 0; i < meshData.subMeshCount; ++i)
                    // {
                    //     indexCount += meshData.GetSubMesh(i).indexCount;
                    // }
                    // //var vertices = new NativeArray<float3>(meshData.vertexCount, Allocator.Temp);
                    // var triangles = new NativeArray<int3>(indexCount / 3, Allocator.Temp);
                    // NativeArray<float3> vertices = meshData.GetVertexData<float3>();
                    // if (meshData.indexFormat == IndexFormat.UInt16)
                    // {
                    //     var indexData16 = meshData.GetIndexData<ushort>();
                    //     var index = 0;
                    //     for (var sm = 0; sm < meshData.subMeshCount; ++sm)
                    //     {
                    //         var subMesh = meshData.GetSubMesh(sm);
                    //         for (int i = subMesh.indexStart, count = 0; count < subMesh.indexCount; i += 3, count += 3)
                    //         {
                    //             triangles[index++] = (int3) new uint3( indexData16[i], indexData16[i + 1], indexData16[i + 2] );
                    //         }
                    //     }
                    // }
                    // else
                    // {
                    //     var indexData32 = meshData.GetIndexData<uint>();
                    //     var index = 0;
                    //     for (var sm = 0; sm < meshData.subMeshCount; ++sm)
                    //     {
                    //         var subMesh = meshData.GetSubMesh(sm);
                    //         for (int i = subMesh.indexStart, count = 0; count < subMesh.indexCount; i += 3, count += 3)
                    //         {
                    //             triangles[index++] = (int3) new uint3( indexData32[i], indexData32[i + 1], indexData32[i + 2] );
                    //         }
                    //     }
                    // }
                    //
                    // var filter = new CollisionFilter
                    // {
                    //     BelongsTo = (uint)(1 << authoring.gameObject.layer),
                    //     CollidesWith = default,
                    //     GroupIndex = 0,
                    // };
                    // var collider = Unity.Physics.MeshCollider.Create(vertices, triangles, filter);
                    // var physicsCollider = new PhysicsCollider();
                    // physicsCollider.Value = collider;
                    // AddComponent(entity, physicsCollider);
                    // triangles.Dispose();
                    // vertices.Dispose();
                    
                }
            }
        }
    }

    public struct BoidObstacle : IComponentData
    {
    }

}
