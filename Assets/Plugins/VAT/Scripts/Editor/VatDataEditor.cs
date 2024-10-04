using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace VAT.Editor
{
    [CustomEditor(typeof(VatData))]
    public class VatDataEditor : UnityEditor.Editor
    {
        private static readonly List<VatData.VatClip> animationFrames = new List<VatData.VatClip>();
        private const float encodeBit = 1f / 255f;
        private static readonly Vector2 encodeMultiplier = new Vector2(1f, 255f);

        private new VatData target => (VatData)base.target;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Bind(serializedObject);

            var saveButton = new Button();
            saveButton.text = "Save";
            saveButton.clicked += HandleSaveButtonClick;
            root.Add(saveButton);

            SerializedProperty property = serializedObject.GetIterator();
            property.NextVisible(true);

            while (property.NextVisible(false))
            {
                if (property.name == "m_Script")
                    continue;

                PropertyField propertyField = new PropertyField(property);

                if (property.name == "vatId" || property.name == "vatClips")
                    propertyField.enabledSelf = false;

                propertyField.Bind(serializedObject);

                root.Add(propertyField);
            }

            return root;
        }

        private void HandleSaveButtonClick()
        {
            Save(target.mesh, target.material, target.fps, target.mode, target.animationClips);
        }

        public void Save(GameObject mesh, Material material, int fps, VatMode vatMode,
            List<AnimationClip> animationClips)
        {
            animationFrames.Clear();

            Assert.IsNotNull(mesh, "[VAT Generator] Unassigned mesh param!");

            // Instantiate mesh gameObject to make use of 
            var gameObject = Instantiate(mesh, Vector3.zero, Quaternion.identity);

            var renderer = gameObject.GetComponentInChildren<SkinnedMeshRenderer>(true);

            // Calculate total bounds
            Bounds totalBounds = new Bounds();
            int totalFrames = 0;
            var bakedMesh = new Mesh();

            Assert.IsNotNull(animationClips, "[VAT Generator] Unassigned animationClips param!");
            Assert.IsTrue(animationClips.Count > 0, "[VAT Generator] No clips provided!");

            for (int i = 0; i < animationClips.Count; ++i)
            {
                var clip = animationClips[i];

                Debug.Log($"clip: {clip.name}");

                var duration = clip.length;
                var frames = Mathf.CeilToInt(duration * fps);
                totalFrames += frames;

                for (int j = 0; j < frames; ++j)
                {
                    float normalizedTime = (float)j / frames;

                    Debug.Log($"evaluated bounds for frame {j} at time {normalizedTime * duration}");
                    clip.SampleAnimation(gameObject, normalizedTime * duration);
                    renderer.BakeMesh(bakedMesh);
                    totalBounds.Encapsulate(bakedMesh.bounds);

                    Debug.Log($"render bounds: {renderer.bounds}");
                    Debug.Log($"totalBounds: {totalBounds}");
                }
            }

            int width = 0;
            int height = 0;
            var assetPath = AssetDatabase.GetAssetPath(target);
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            var directory = Path.GetDirectoryName(assetPath);

            // Prepare clip texture
            if (vatMode == VatMode.RGB24)
            {
                var textures = GenerateTexturesRGB24x2(renderer, animationClips, totalFrames, fps, gameObject, mesh,
                    bakedMesh, totalBounds, out width, out height);
                material.shader = Shader.Find("Shader Graphs/SG_VatLit_Double");

                File.WriteAllBytes(directory + "/Tex_" + fileName + "_1.png", textures[0]);
                File.WriteAllBytes(directory + "/Tex_" + fileName + "_2.png", textures[1]);
                File.WriteAllBytes(directory + "/Tex_" + fileName + "_Normal.png", textures[2]);
            }
            else
            {
                // vatMode == VatMode.RGBAHalf
                var textures = GenerateTexturesRGBAHalf(renderer, animationClips, totalFrames, fps, gameObject, mesh,
                    bakedMesh, totalBounds, out width, out height);
                material.shader = Shader.Find("Shader Graphs/SG_VatLit_Single");

                File.WriteAllBytes(directory + "/Tex_" + fileName + ".png", textures[0]);
                File.WriteAllBytes(directory + "/Tex_" + fileName + "_Normal.png", textures[1]);
            }

            material.SetVector("_PosMin", totalBounds.min);
            material.SetVector("_PosMax", totalBounds.max);
            material.SetVector("_TexelSize", new Vector4(1f / width, 1f / height, width, height));
            EditorUtility.SetDirty(material);

            DestroyImmediate(gameObject);
            DestroyImmediate(bakedMesh);

            target.mode = vatMode;
            target.fps = fps;
            target.mesh = mesh;
            target.material = material;
            target.animationClips = new List<AnimationClip>(animationClips);
            target.vatClips = new List<VatData.VatClip>(animationFrames);
            EditorUtility.SetDirty(target);

            AssetDatabase.Refresh();
        }

        private static List<byte[]> GenerateTexturesRGB24x2(SkinnedMeshRenderer renderer,
            List<AnimationClip> animationClips, int totalFrames, int fps, GameObject gameObject, GameObject model,
            Mesh bakedMesh, Bounds totalBounds, out int width, out int height)
        {
            width = renderer.sharedMesh.vertexCount;
            height = totalFrames + (animationClips.Count - 1);

            for (int i = 0; i < animationClips.Count; ++i)
            {
                if (animationClips[i].isLooping)
                {
                    height++;
                }
            }

            var vat1 = new Texture2D(
                width: width,
                height: height,
                textureFormat: TextureFormat.RGB24,
                mipCount: 0,
                linear: false);

            var vat2 = new Texture2D(
                width: width,
                height: height,
                textureFormat: TextureFormat.RGB24,
                mipCount: 0,
                linear: false);

            var vatNormal = new Texture2D(
                width: width,
                height: height,
                textureFormat: TextureFormat.RGB24,
                mipCount: 0,
                linear: false);

            int row = 0;
            for (int i = 0; i < animationClips.Count; ++i)
            {
                var clip = animationClips[i];
                var duration = clip.length;

                var frames = (int)(duration * fps);
                var loopFrames = frames;

                int startFrame = 0;

                if (clip.isLooping)
                {
                    frames++;
                }

                // Evaluate frames
                for (int j = 0; j < frames; ++j)
                {
                    if (j == 0)
                    {
                        startFrame = row;
                    }

                    // Actual frames
                    float normalizedTime = (float)j / loopFrames;
                    clip.SampleAnimation(gameObject, normalizedTime * duration);
                    renderer.BakeMesh(bakedMesh);
                    ProcessTexturesRGB24(bakedMesh.vertices, totalBounds, vat1, vat2, row);
                    ProcessNormal(bakedMesh.normals, vatNormal, row);

                    if (j == frames - 1)
                    {
                        int endFrame = row + 1; // find out why +1 fixes loop issue
                        animationFrames.Add(new VatData.VatClip(
                            frames,
                            (float)startFrame / height + .5f / height,
                            (float)endFrame / height - .5f / height,
                            duration));
                    }

                    row++;
                }
            }

            var textures = new List<byte[]>();
            textures.Add(vat1.EncodeToPNG());
            textures.Add(vat2.EncodeToPNG());
            textures.Add(vatNormal.EncodeToPNG());
            return textures;
        }

        private static List<byte[]> GenerateTexturesRGBAHalf(SkinnedMeshRenderer renderer,
            List<AnimationClip> animationClips, int totalFrames, int fps, GameObject gameObject, GameObject model,
            Mesh bakedMesh, Bounds totalBounds, out int width, out int height)
        {
            width = renderer.sharedMesh.vertexCount;
            height = totalFrames + (animationClips.Count - 1);
            var vatTexture = new Texture2D(
                width: width,
                height: height,
                textureFormat: TextureFormat.RGBAHalf,
                mipCount: 0,
                linear: false);

            var vatNormal = new Texture2D(
                width: width,
                height: height,
                textureFormat: TextureFormat.RGBAHalf,
                mipCount: 0,
                linear: false);

            int row = 0;
            for (int i = 0; i < animationClips.Count; ++i)
            {
                var clip = animationClips[i];
                var duration = clip.length;
                var frames = (int)(duration * fps);

                int startFrame = 0;

                // Evaluate frames
                for (int j = 0; j < frames; ++j)
                {
                    float normalizedTime = (float)j / frames;

                    if (j == 0)
                    {
                        startFrame = row;
                    }

                    // Actual frames
                    clip.SampleAnimation(gameObject, normalizedTime * duration);
                    renderer.BakeMesh(bakedMesh);

                    ProcessTexturesRGBAHalf(bakedMesh.vertices, totalBounds, vatTexture, row);
                    ProcessNormal(bakedMesh.normals, vatNormal, row++);

                    if (j == frames - 1)
                    {
                        int endFrame = row;
                        animationFrames.Add(new VatData.VatClip(frames, (float)startFrame / height,
                            (float)endFrame / height, duration));
                    }
                }
            }

            var textures = new List<byte[]>();
            textures.Add(vatTexture.EncodeToPNG());
            textures.Add(vatNormal.EncodeToPNG());
            return textures;
        }

        private static void ProcessTexturesRGB24(Vector3[] vertices, Bounds totalBounds, Texture2D tex1, Texture2D tex2,
            int row)
        {
            for (int i = 0; i < vertices.Length; ++i)
            {
                var vertex = vertices[i];
                var rValue = Mathf.InverseLerp(totalBounds.min.x, totalBounds.max.x, vertex.x);
                var r = EncodeFloatRG(rValue);

                var gValue = Mathf.InverseLerp(totalBounds.min.y, totalBounds.max.y, vertex.y);
                var g = EncodeFloatRG(gValue);

                var bValue = Mathf.InverseLerp(totalBounds.min.z, totalBounds.max.z, vertex.z);
                var b = EncodeFloatRG(bValue);

                tex1.SetPixel(i, row, new Color(r.x, r.y, g.x));
                tex2.SetPixel(i, row, new Color(g.y, b.x, b.y));
            }
        }

        private static void ProcessNormal(Vector3[] normals, Texture2D normalTex, int row)
        {
            for (int i = 0; i < normals.Length; ++i)
            {
                var normal = normals[i];
                normal = normal * 0.5f + (Vector3.one * 0.5f);
                normalTex.SetPixel(i, row, new Color(normal.x, normal.y, normal.z));
            }
        }

        private static void ProcessTexturesRGBAHalf(Vector3[] vertices, Bounds totalBounds, Texture2D tex, int row)
        {
            for (int i = 0; i < vertices.Length; ++i)
            {
                var vertex = vertices[i];
                var rValue = Mathf.InverseLerp(totalBounds.min.x, totalBounds.max.x, vertex.x);
                var gValue = Mathf.InverseLerp(totalBounds.min.y, totalBounds.max.y, vertex.y);
                var bValue = Mathf.InverseLerp(totalBounds.min.z, totalBounds.max.z, vertex.z);

                tex.SetPixel(i, row, new Color(rValue, gValue, bValue));
            }
        }

        private static Vector2 EncodeFloatRG(float value)
        {
            Vector2 encoded = encodeMultiplier * value;
            encoded = new Vector2(encoded.x - Mathf.Floor(encoded.x), encoded.y - Mathf.Floor(encoded.y));
            encoded.x -= encoded.y * encodeBit;
            return encoded;
        }
    }
}