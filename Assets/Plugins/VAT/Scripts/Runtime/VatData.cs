using System;
using System.Collections.Generic;
using UnityEngine;

namespace VAT
{
    [CreateAssetMenu]
    public class VatData : ScriptableObject
    {
        public GameObject mesh;
        public Material material;
        public VatMode mode = VatMode.RGB24;
        public int fps = 30;
        public List<AnimationClip> animationClips = new List<AnimationClip>();

        /// Lookup id for use in ECS, auto-assigned when populating VatDataList
        public ushort vatId;

        public List<VatClip> vatClips = new List<VatClip>();

        [Serializable]
        public struct VatClip
        {
            public int frames;
            public float startFrame;
            public float endFrame;
            public float duration;

            public VatClip(int frames, float startFrame, float endFrame, float duration)
            {
                this.frames = frames;
                this.startFrame = startFrame;
                this.endFrame = endFrame;
                this.duration = duration;
            }
        }
    }

    public enum VatMode
    {
        RGBAHalf,
        RGB24
    }
}