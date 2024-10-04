using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace VAT.Sample
{
    public class MaterialPropertyClipDurationAuthoring : MonoBehaviour
    {
        public float duration;

        private class Baker : Baker<MaterialPropertyClipDurationAuthoring>
        {
            public override void Bake(MaterialPropertyClipDurationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MaterialPropertyClipDuration()
                {
                    duration = authoring.duration
                });
            }
        }
    }

    [MaterialProperty("_Duration")]
    public struct MaterialPropertyClipDuration : IComponentData
    {
        public float duration;
    }
}