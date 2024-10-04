using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace VAT.Sample
{
    public class MaterialPropertyClipTimeAuthoring : MonoBehaviour
    {
        private class Baker : Baker<MaterialPropertyClipTimeAuthoring>
        {
            public override void Bake(MaterialPropertyClipTimeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MaterialPropertyClipTime());
            }
        }
    }

    [MaterialProperty("_ClipTime")]
    public struct MaterialPropertyClipTime : IComponentData
    {
        public float time;
    }
}