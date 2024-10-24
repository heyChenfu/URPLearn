
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

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
            var obstacleQuery = SystemAPI.QueryBuilder().WithAll<BoidObstacle, LocalToWorld>().Build();

            var world = state.WorldUnmanaged;
            state.EntityManager.GetAllUniqueSharedComponents(out NativeList<BoidSharedComponentData> uniqueBoidTypes, world.UpdateAllocator.ToAllocator);
            int targetCount = targetQuery.CalculateEntityCount();
            int obstacleCount = obstacleQuery.CalculateEntityCount();
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
                var obstaclePositions = CollectionHelper.CreateNativeArray<float3, RewindableAllocator>(obstacleCount, ref world.UpdateAllocator);

                var targetPositionJob = new InitialTargetPositionJob()
                {
                    TargetPositions = targetPositions,
                    IsPositionSet = isPositionSet,
                    TargetGroupId = boidData.TargetGroupId,
                };
                JobHandle targetPositionJobHandle = targetPositionJob.ScheduleParallel(targetQuery, state.Dependency);
                var obstaclePositionJob = new InitialObstaclePositionJob()
                {
                    TargetPositions = obstaclePositions,
                };
                JobHandle obstaclePositionJobHandle = obstaclePositionJob.ScheduleParallel(obstacleQuery, state.Dependency);
                JobHandle initHandle = JobHandle.CombineDependencies(targetPositionJobHandle, obstaclePositionJobHandle);

                var boidMainJob = new BoidMainCalJob()
                {
                    BoidSharedData = boidData,
                    //BoidComponentDataHandle = SystemAPI.GetSharedComponentTypeHandle<BoidSharedComponentData>(),
                    BoidDataHandle = SystemAPI.GetComponentTypeHandle<BoidData>(),
                    LocalTransformHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(),
                    LocalToWorldHandle = SystemAPI.GetComponentTypeHandle<LocalToWorld>(),
                };
                JobHandle boidMainJobHandle = boidMainJob.ScheduleParallel(boidQuery, initHandle);
                //处理目标

                //处理障碍物碰撞检测

                //移动boid实体
                var steerBoidJob = new SteerBoidJob()
                {
                    deltaTime = dt,
                };
                var steerBoidJobHandle = steerBoidJob.ScheduleParallel(boidQuery, boidMainJobHandle);

                state.Dependency = steerBoidJobHandle;

                boidQuery.AddDependency(state.Dependency);
                boidQuery.ResetFilter();
            }
            uniqueBoidTypes.Dispose();
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
    partial struct InitialObstaclePositionJob : IJobEntity
    {
        public NativeArray<float3> TargetPositions;
        void Execute([EntityIndexInQuery] int entityIndexInQuery, in LocalToWorld localToWorld)
        {
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
                var alignmentForce = SteerTowards(boidData.FlockHeading, boidSharedData, boidData) * boidSharedData.alignWeight;
                var cohesionForce = SteerTowards(offsetToFlockmatesCentre, boidSharedData, boidData) * boidSharedData.cohesionWeight;
                var seperationForce = SteerTowards(boidData.AvoidanceHeading, boidSharedData, boidData) * boidSharedData.seperateWeight;
                acceleration += alignmentForce;
                acceleration += cohesionForce;
                acceleration += seperationForce;
            }
            return acceleration;
        }

        private float3 SteerTowards(float3 vector, BoidSharedComponentData boidSharedData, BoidData boidData)
        {
            //目标方向和当前速度差值以得到所需的加速度向量
            float3 v = Normalize(vector) * boidSharedData.maxSpeed - boidData.Velocity;
            return ClampMagnitude(v, boidSharedData.maxSteerForce);
        }

        private float3 Normalize(float3 value)
        {
            float num = (float)math.sqrt(value.x * value.x + value.y * value.y + value.z * value.z);
            if (num > 1E-05f)
                return value / num;
            return float3.zero;
        }

        public static float3 ClampMagnitude(float3 vector, float maxLength)
        {
            float num = vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
            if (num > maxLength * maxLength)
            {
                float num2 = (float)math.sqrt(num);
                if (num2 > 0)
                {
                    float num3 = vector.x / num2;
                    float num4 = vector.y / num2;
                    float num5 = vector.z / num2;
                    return new float3(num3 * maxLength, num4 * maxLength, num5 * maxLength);
                }
            }
            return vector;
        }

    }

    [BurstCompile]
    partial struct SteerBoidJob : IJobEntity
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
