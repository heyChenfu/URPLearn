
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

namespace BoidsECSSimulator
{
    [BurstCompile]
    public partial struct BoidSystem : ISystem
    {
        public void OnCreate(ref SystemState state) 
        {
            state.RequireForUpdate(state.GetEntityQuery(typeof(BoidSharedComponentData)));
        }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            EntityQuery boidQuery = SystemAPI.QueryBuilder().WithAll<BoidSharedComponentData>().
                WithAllRW<BoidData>().WithAllRW<LocalTransform>().WithAllRW<LocalToWorld>().Build();
            var targetQuery = SystemAPI.QueryBuilder().WithAll<BoidTarget, LocalToWorld>().Build();

            var world = state.WorldUnmanaged;
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<BoidSharedComponentData> uniqueBoidTypes, world.UpdateAllocator.ToAllocator);
            PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;
            NativeArray<float3> rayDirections = BoidDirectionHelper.GetDirectionsFloat3S();
            int targetCount = targetQuery.CalculateEntityCount();
            float dt = math.min(0.05f, SystemAPI.Time.DeltaTime);

            //每次循环处理一组相同 Boid 配置的实体
            foreach (BoidSharedComponentData boidData in uniqueBoidTypes)
            {
                boidQuery.AddSharedComponentFilter(boidData);
                var boidCount = boidQuery.CalculateEntityCount();
                if (boidCount == 0)
                {
                    boidQuery.ResetFilter();
                    continue;
                }

                var targetPositions = CollectionHelper.CreateNativeArray<float3, RewindableAllocator>(targetCount, ref world.UpdateAllocator);
                var isPositionSet = CollectionHelper.CreateNativeArray<bool, RewindableAllocator>(targetCount, ref world.UpdateAllocator);
                
                var targetPositionJob = new InitialTargetPositionJob()
                {
                    TargetPositions = targetPositions,
                    IsPositionSet = isPositionSet,
                    TargetGroupId = boidData.TargetGroupId,
                };
                JobHandle targetPositionJobHandle = targetPositionJob.ScheduleParallel(targetQuery, state.Dependency);

                var boidMainJob = new BoidMainCalJob()
                {
                    BoidSharedData = boidData,
                    //BoidComponentDataHandle = SystemAPI.GetSharedComponentTypeHandle<BoidSharedComponentData>(),
                    BoidDataHandle = SystemAPI.GetComponentTypeHandle<BoidData>(),
                    LocalTransformHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(),
                    LocalToWorldHandle = SystemAPI.GetComponentTypeHandle<LocalToWorld>(),
                };
                JobHandle boidMainJobHandle = boidMainJob.ScheduleParallel(boidQuery, targetPositionJobHandle);
                //处理目标
                var boidTargetJob = new BoidTargetJob()
                {
                    TargetPositions = targetPositions,
                    IsPositionSet = isPositionSet,
                };
                JobHandle boidTargetJobHandle = boidTargetJob.ScheduleParallel(boidQuery, boidMainJobHandle);
                //处理障碍物碰撞检测
                var boidObstacleJob = new BoidObstacleJob()
                {
                    MyCollisionWorld = collisionWorld,
                    RayDirections = rayDirections,
                };
                JobHandle boidObstacleJobHandle = boidObstacleJob.ScheduleParallel(boidQuery, boidTargetJobHandle);
                //移动boid实体
                var steerBoidJob = new BoidSteerJob()
                {
                    deltaTime = dt,
                };
                var steerBoidJobHandle = steerBoidJob.ScheduleParallel(boidQuery, boidObstacleJobHandle);

                state.Dependency = steerBoidJobHandle;

                boidQuery.AddDependency(state.Dependency);
                boidQuery.ResetFilter();
            }
            uniqueBoidTypes.Dispose();
            rayDirections.Dispose(state.Dependency);
        }

    }

    [BurstCompile]
    partial struct InitialTargetPositionJob : IJobEntity
    {
        public NativeArray<float3> TargetPositions;
        public NativeArray<bool> IsPositionSet;
        public int TargetGroupId;

        void Execute([EntityIndexInQuery] int entityIndexInQuery, ref BoidTarget targetData, in LocalToWorld localToWorld)
        {
            IsPositionSet[entityIndexInQuery] = targetData.TargetGroupId == TargetGroupId;
            TargetPositions[entityIndexInQuery] = localToWorld.Position;
        }
    }

    [BurstCompile]
    unsafe struct BoidMainCalJob : IJobChunk
    {
        public BoidSharedComponentData BoidSharedData;
        //public SharedComponentTypeHandle<BoidSharedComponentData> BoidComponentDataHandle;
        public ComponentTypeHandle<BoidData> BoidDataHandle;
        [ReadOnly] 
        public ComponentTypeHandle<LocalTransform> LocalTransformHandle;
        [ReadOnly]
        public ComponentTypeHandle<LocalToWorld> LocalToWorldHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            //BoidSharedComponentData boidSharedData = chunk.GetSharedComponent(BoidComponentDataHandle);
            BoidData* boidDataArr = chunk.GetComponentDataPtrRW(ref BoidDataHandle);
            NativeArray<LocalTransform> localTransformArr = chunk.GetNativeArray(ref LocalTransformHandle);
            NativeArray<LocalToWorld> localToWorldArr = chunk.GetNativeArray(ref LocalToWorldHandle);
            for (int i = 0; i < chunk.Count; i++)
            {
                boidDataArr[i].NumFlockmates = 0;
                boidDataArr[i].FlockHeading = float3.zero;
                boidDataArr[i].FlockCentre = float3.zero;
                boidDataArr[i].AvoidanceHeading = float3.zero;
                for (int j = 0; j < chunk.Count; j++)
                {
                    if (i == j)
                        continue;
                    float3 offset = localTransformArr[j].Position - localTransformArr[i].Position;
                    float sqrDst = offset.x * offset.x + offset.y * offset.y + offset.z * offset.z;
                    if (sqrDst < BoidSharedData.perceptionRadius * BoidSharedData.perceptionRadius)
                    {
                        boidDataArr[i].NumFlockmates += 1;
                        boidDataArr[i].FlockHeading += boidDataArr[j].Forward;
                        boidDataArr[i].FlockCentre += localTransformArr[j].Position;
                        if (sqrDst < BoidSharedData.avoidanceRadius * BoidSharedData.avoidanceRadius)
                            boidDataArr[i].AvoidanceHeading -= offset / sqrDst;
                    }
                }
                boidDataArr[i].Acceleration = UpdateBoid(BoidSharedData, boidDataArr[i], localTransformArr[i]);
            }

        }

        private float3 UpdateBoid(BoidSharedComponentData boidSharedData, BoidData boidData, LocalTransform localTransform)
        {
            float3 acceleration = float3.zero;

            if (boidData.NumFlockmates > 0)
            {
                //位置总和除以邻居数量得到邻居平均位置
                float3 centreOfFlockmates = boidData.FlockCentre / boidData.NumFlockmates;
                //当前位置指向邻居平均位置向量
                float3 offsetToFlockmatesCentre = centreOfFlockmates - localTransform.Position;

                //分别得到对齐,聚合,分离
                var alignmentForce = CommonFunction.SteerTowards(
                    boidData.FlockHeading, boidSharedData.maxSpeed, boidData.Velocity, boidSharedData.maxSteerForce) 
                    * boidSharedData.alignWeight;
                var cohesionForce = CommonFunction.SteerTowards(
                    offsetToFlockmatesCentre, boidSharedData.maxSpeed, boidData.Velocity, boidSharedData.maxSteerForce) 
                    * boidSharedData.cohesionWeight;
                var seperationForce = CommonFunction.SteerTowards(
                    boidData.AvoidanceHeading, boidSharedData.maxSpeed, boidData.Velocity, boidSharedData.maxSteerForce) 
                    * boidSharedData.seperateWeight;
                acceleration += alignmentForce;
                acceleration += cohesionForce;
                acceleration += seperationForce;
            }
            return acceleration;
        }

    }

    [BurstCompile]
    partial struct BoidTargetJob : IJobEntity 
    {
        public NativeArray<float3> TargetPositions;
        public NativeArray<bool> IsPositionSet;

        void Execute([EntityIndexInQuery] int entityIndexInQuery,
            in BoidSharedComponentData boidSharedData,
            ref BoidData boidData,
            ref LocalTransform localTransform)
        {
            for (int i = 0; i < IsPositionSet.Length; ++i)
            {
                if (!IsPositionSet[i])
                    continue;
                float3 offsetToTarget = TargetPositions[i] - localTransform.Position;
                boidData.Acceleration += CommonFunction.SteerTowards(
                    offsetToTarget, boidSharedData.maxSpeed, boidData.Velocity, boidSharedData.maxSteerForce) 
                    * boidSharedData.targetWeight;
            }

        }
    }

    [BurstCompile]
    partial struct BoidObstacleJob : IJobEntity 
    {
        [ReadOnly] public CollisionWorld MyCollisionWorld;
        [ReadOnly] public NativeArray<float3> RayDirections;
        
        void Execute([EntityIndexInQuery] int entityIndexInQuery,
            in BoidSharedComponentData boidSharedData,
            ref BoidData boidData,
            ref LocalTransform localTransform)
        {
            var filter = new CollisionFilter { 
                BelongsTo = (uint)boidSharedData.obstacleLayerMask,
                CollidesWith = (uint)boidSharedData.obstacleLayerMask,
            };
            if(MyCollisionWorld.SphereCast(localTransform.Position, boidSharedData.boundsRadius, boidData.Forward, boidSharedData.collisionAvoidDst, filter))
            {
                float3 collisionAvoidDir = GetAvoidDir(boidSharedData, boidData, localTransform);
                float3 collisionAvoidForce = CommonFunction.SteerTowards(collisionAvoidDir, boidSharedData.maxSpeed, boidData.Velocity, boidSharedData.maxSteerForce);
                boidData.Acceleration += collisionAvoidForce;
            }

        }

        float3 GetAvoidDir(in BoidSharedComponentData boidSharedData, BoidData boidData, LocalTransform localTransform)
        {
            for (int i = 0; i < RayDirections.Length; i++)
            {
                float3 dir = math.normalize(RayDirections[i]);
                var ray = new RaycastInput
                {
                    Start = localTransform.Position,
                    End = localTransform.Position + dir * boidSharedData.collisionAvoidDst,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = (uint)boidSharedData.obstacleLayerMask,
                        CollidesWith = (uint)boidSharedData.obstacleLayerMask,
                        GroupIndex = 0,
                    }
                };

                if (!MyCollisionWorld.CastRay(ray, out var hit))
                {
                    return dir; // 找到一个方向返回
                }
            }

            return boidData.Forward; // 默认前进方向
        }

    }


    [BurstCompile]
    partial struct BoidSteerJob : IJobEntity
    {
        public float deltaTime;

        void Execute([EntityIndexInQuery] int entityIndexInQuery,
            in BoidSharedComponentData boidSharedData,
            ref BoidData boidData,
            ref LocalTransform localTransform)
        {
            boidData.Velocity += boidData.Acceleration * deltaTime;
            //使用最大最小速度约束当前速度
            float speed = (float)math.sqrt(boidData.Velocity.x * boidData.Velocity.x + boidData.Velocity.y * boidData.Velocity.y + boidData.Velocity.z * boidData.Velocity.z);
            float3 dir = speed != 0 ? (boidData.Velocity / speed) : float3.zero;
            speed = math.clamp(speed, boidSharedData.minSpeed, boidSharedData.maxSpeed);
            boidData.Velocity = dir * speed;

            localTransform.Position = localTransform.Position + boidData.Velocity * deltaTime;
            localTransform.Rotation = quaternion.LookRotationSafe(dir, math.up());
            boidData.Forward = dir;
            //localTransform = new LocalTransform
            //{
            //    Value = float4x4.TRS(
            //        localToWorld.Position + boidData.Velocity * deltaTime,
            //        quaternion.LookRotationSafe(dir, math.up()),
            //        20
            //        )
            //}

        }
    }

}
