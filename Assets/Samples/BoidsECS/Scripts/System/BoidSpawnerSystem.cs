
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BoidsECSSimulator
{
    [BurstCompile]
    public partial struct BoidSpawnerSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }

        public void OnDestroy(ref SystemState state) { }

        public void OnUpdate(ref SystemState state)
        {
            Random random = Random.CreateFromIndex((uint)state.GlobalSystemVersion);
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            // Queries for all Spawner components. Uses RefRW because this system wants
            // to read from and write to the component. If the system only needed read-only
            // access, it would use RefRO instead.
            EntityQuery spawnerEntitiesQuery = SystemAPI.QueryBuilder().WithAll<BoidSpawnerComponentData>().Build();
            NativeArray<BoidSpawnerComponentData> spawnerEntitiesArr = 
                spawnerEntitiesQuery.ToComponentDataArray<BoidSpawnerComponentData>(Allocator.Temp);
            foreach (BoidSpawnerComponentData spawner in spawnerEntitiesArr)
            {
                for (int i = 0; i < spawner.SpawnNumber; ++i)
                {
                    float3 randomOffset = new float3(
                        random.NextFloat(-spawner.SpawnRange, spawner.SpawnRange),
                        random.NextFloat(-spawner.SpawnRange, spawner.SpawnRange),
                        random.NextFloat(-spawner.SpawnRange, spawner.SpawnRange)
                    );
                    // Spawns a new entity and positions it at the spawner.
                    Entity newEntity = state.EntityManager.Instantiate(spawner.Prefab);
                    // LocalPosition.FromPosition returns a Transform initialized with the given position.
                    // Set position, rotation, and scale
                    float3 position = spawner.SpawnPosition + randomOffset;
                    quaternion rotation = quaternion.identity;
                    float scale = spawner.SpawnScale;
                    state.EntityManager.AddComponent<LocalTransform>(newEntity);
                    state.EntityManager.SetComponentData(newEntity, LocalTransform.FromPositionRotationScale(position, rotation, scale));

                }
            }
            state.EntityManager.RemoveComponent<BoidSpawnerComponentData>(spawnerEntitiesQuery);

        }

    }

}
