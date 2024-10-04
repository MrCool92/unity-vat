using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace VAT.Sample
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public GameObject prefab;
        public int count;
        public int radius;

        private class Baker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Spawner()
                {
                    prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
                    count = authoring.count,
                    radius = authoring.radius,
                    random = new Random((uint)authoring.GetInstanceID())
                });
            }
        }
    }

    public struct Spawner : IComponentData
    {
        public Entity prefab;
        public int count;
        public int radius;
        public Random random;
    }
}