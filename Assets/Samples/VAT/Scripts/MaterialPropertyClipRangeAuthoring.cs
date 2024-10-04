using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace VAT.Sample
{
    public class MaterialPropertyClipRangeAuthoring : MonoBehaviour
    {
        public float2 clipRange;
        
        private class Baker : Baker<MaterialPropertyClipRangeAuthoring>
        {
            public override void Bake(MaterialPropertyClipRangeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MaterialPropertyClipRange()
                {
                    clipRange = authoring.clipRange
                });
            }
        }
    }

    [MaterialProperty("_ClipRange")]
    public struct MaterialPropertyClipRange : IComponentData
    {
        public float2 clipRange;
    }
}