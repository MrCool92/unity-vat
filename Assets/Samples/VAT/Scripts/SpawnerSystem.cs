using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace VAT.Sample
{
    public partial struct SpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (localTransform, spawner) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Spawner>>())
            {
                if (spawner.ValueRO.count == 0)
                    continue;

                var random = spawner.ValueRO.random;

                for (int i = 0; i < spawner.ValueRO.count; ++i)
                {
                    var instance = state.EntityManager.Instantiate(spawner.ValueRO.prefab);

                    var offset = new float3(random.NextFloat(-spawner.ValueRO.radius, spawner.ValueRO.radius), 0,
                        random.NextFloat(-spawner.ValueRO.radius, spawner.ValueRO.radius));
                    state.EntityManager.SetComponentData(instance,
                        LocalTransform.FromPosition(localTransform.ValueRO.Position + offset));

                    var time = new MaterialPropertyClipTime();
                    time.time = random.NextFloat(0f, 1f);

                    state.EntityManager.SetComponentData(instance, time);
                }

                spawner.ValueRW.count = 0;
                spawner.ValueRW.random = random;
            }
        }
    }
}