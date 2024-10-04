using Unity.Burst;
using Unity.Entities;

namespace VAT.Sample
{
    public partial struct MaterialPropertyClipTimeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var currentTime in SystemAPI.Query<RefRW<MaterialPropertyClipTime>>())
            {
                currentTime.ValueRW.time += SystemAPI.Time.DeltaTime;
            }
        }
    }
}