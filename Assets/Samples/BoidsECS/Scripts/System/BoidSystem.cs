
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.Build.Content;
using static UnityEngine.GraphicsBuffer;

namespace BoidsECSSimulator
{
    [BurstCompile]
    public partial struct BoidSystem : ISystem
    {
        public void OnCreate(ref SystemState state) 
        {
            state.RequireForUpdate(state.GetEntityQuery(typeof(BoidComponentData)));
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            EntityQuery boidQuery = SystemAPI.QueryBuilder().
                WithAll<BoidComponentData>().WithAllRW<LocalToWorld>().Build();

            var world = state.WorldUnmanaged;
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<BoidComponentData> uniqueBoidTypes, world.UpdateAllocator.ToAllocator);
            float dt = math.min(0.05f, SystemAPI.Time.DeltaTime);

            //每次循环处理一组相同 Boid 配置的实体
            foreach (BoidComponentData boidData in uniqueBoidTypes)
            {
                boidQuery.AddSharedComponentFilter(boidData);
                var boidCount = boidQuery.CalculateEntityCount();
                if (boidCount == 0)
                {
                    boidQuery.ResetFilter();
                    continue;
                }
                NativeArray<float3> flockHeadingArr = CollectionHelper.CreateNativeArray<float3, RewindableAllocator>(
                    boidCount, ref world.UpdateAllocator);
                NativeArray<float3> flockCentreArr = CollectionHelper.CreateNativeArray<float3, RewindableAllocator>(
                    boidCount, ref world.UpdateAllocator);
                NativeArray<float3> avoidanceHeadingArr = CollectionHelper.CreateNativeArray<float3, RewindableAllocator>(
                    boidCount, ref world.UpdateAllocator);
                NativeArray<int> numFlockmatesArr = CollectionHelper.CreateNativeArray<int, RewindableAllocator>(
                    boidCount, ref world.UpdateAllocator);

                boidQuery.ResetFilter();
            }
        }

    }


    [BurstCompile]
    partial struct BoidMainCalJob : IJobChunk
    {
        [NativeDisableParallelForRestriction]
        public EntityManager entityManager;

        public SharedComponentTypeHandle<BoidComponentData> BoidComponentDataHandle;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;

        public NativeArray<float3> FlockHeadingArr;
        public NativeArray<float3> FlockCentreArr;
        public NativeArray<float3> AvoidanceHeadingArr;
        public NativeArray<int> NumFlockmatesArr;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            // 获取当前 chunk 中的 BoidComponentData 索引
            int sharedComponentIndex = chunk.GetSharedComponentIndex(BoidComponentDataHandle);
            // 通过 EntityManager 获取对应的 BoidSharedComponentData
            BoidComponentData sharedData = entityManager.GetSharedComponentManaged<BoidComponentData>(sharedComponentIndex);

            NativeArray<LocalToWorld> localToWorldArr = chunk.GetNativeArray(ref LocalToWorldHandle);
            for (int i = 0; i < chunk.Count; i++)
            {
                for (int j = 0; j < chunk.Count; j++)
                {
                    if (i == j)
                        continue;

                }
            }

        }

    }

}
